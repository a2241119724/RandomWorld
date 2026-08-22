"""共享 PyTorch 训练循环与指标工具（从 train.py 迁出）。

train_torch 只负责训练与返回结果，不负责落盘——持久化由模型各自的 ``save``
方法完成，职责更清晰。数据切分由调用方（train.py）经 dataio 完成。
"""
from __future__ import annotations

import numpy as np
import torch
import torch.nn as nn
from torch.utils.data import DataLoader

from .actions import ACTION_INDEX, NUM_ACTIONS
from .dataset import WorkerDecisionDataset


def accuracy(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    return float((y_true == y_pred).mean())


def compute_class_weights(y: np.ndarray, mode: str, n_classes: int | None = None) -> np.ndarray | None:
    """按训练集类别频率计算交叉熵权重，缓解长尾标签被大类主导。

    mode 取值：
      - ``none``       → 返回 None（不加权）
      - ``sqrt_inv``   → 1/sqrt(freq)，温和（稀有类权重被拉高但不极端，推荐）
      - ``inverse``    → 1/freq，激进（接近完全平衡，可能过度触发稀有行为）

    权重归一化到加权平均 = 1，保持 loss 量级不随加权放大。只在训练集上计算。
    必须补齐到 ``n_classes`` 维：训练集可能缺某些输出类（LLM 常识不打
    store/withdraw 等游戏机制行为），``np.bincount`` 默认截断到观测最大类索引，
    CrossEntropyLoss 要求权重张量与输出类数一致；缺样本类给中性权重 1.0
    （无样本就不会被采样，权重数值不影响 loss，只保证张量形状合法）。
    """
    if mode in (None, "none", ""):
        return None
    if n_classes is None:
        n_classes = int(y.max()) + 1  # 保持旧行为（调用方未显式传时按观测类数）
    counts = np.bincount(y, minlength=n_classes).astype(np.float64)
    freqs = counts / counts.sum()
    if mode == "sqrt_inv":
        w = 1.0 / np.sqrt(freqs + 1e-6)
    elif mode == "inverse":
        w = 1.0 / (freqs + 1e-6)
    else:
        raise ValueError(f"未知 class_weight 模式: {mode}（可选 none/sqrt_inv/inverse）")
    w[counts == 0] = 1.0  # 训练集缺失的类：中性权重
    w = w / (w * freqs).sum()  # 加权平均 = 1
    return w


def oversample_by_proportions(
    X: np.ndarray, y: np.ndarray, target: dict[str, float]
) -> tuple[np.ndarray, np.ndarray]:
    """按目标比例物理拷贝训练集（用户指定的方案：拷贝而非重采样）。

    例：训练集 Gather:Sleep=1:1、目标 8:1 → 把 Gather 样本拷贝 8 倍。
    通用算法：T = max_c(counts[c]/target[c])，类 c 最终条数 = T×target[c]
    （≥ 原始条数，因为 T ≥ counts[c]/target[c]），各样本重复 ceil(倍数) 份。
    拷贝后训练集变大为 sum(T×target)，各类占比精确 == target。

    局限：target 里写了但训练集 count 很小的类（如 boundary 下 self_build 仅 3 条
    却要占 15%）只能靠反复拷贝这 3 条，模型会过拟合到这几个重复样本——这是物理
    拷贝的本质，报告时需标注。target 未列出的已观测类原样保留（不参与目标比例）。
    """
    counts = np.bincount(y, minlength=NUM_ACTIONS).astype(np.float64)
    T = 0.0
    for name, prop in target.items():
        c = ACTION_INDEX[name]
        if counts[c] > 0:
            T = max(T, counts[c] / prop)
    T = int(np.ceil(T))

    listed = {ACTION_INDEX[n] for n in target}
    idx_parts: list[np.ndarray] = []
    for name, prop in target.items():
        c = ACTION_INDEX[name]
        if counts[c] == 0:
            continue  # 无样本可拷（不会出现在拷贝集）
        k = T * prop / counts[c]
        sel = np.where(y == c)[0]
        rep = max(1, int(np.ceil(k)))
        idx_parts.append(np.tile(sel, rep))
    for c in range(NUM_ACTIONS):  # 未列出的已观测类：原样保留
        if c not in listed and counts[c] > 0:
            idx_parts.append(np.where(y == c)[0])
    idx = np.concatenate(idx_parts)
    return X[idx], y[idx]


def oversample_balanced(
    X: np.ndarray, y: np.ndarray
) -> tuple[np.ndarray, np.ndarray]:
    """1:1 均衡物理拷贝训练集：每个已观测类拷贝到与最频繁类同数量。

    例：训练集 Gather:Sleep:Idle = 600:100:40 → 各拷贝到 600 条 → 1:1:1。
    未观测类（LLM 常识不打 store/withdraw 等）跳过，与 oversample_by_proportions
    一致。同样是物理拷贝——极低频类（如 3 条）会被复制上百份，模型易过拟合到
    这几个重复样本，报告时需标注。用于消除大类 bias 对照实验（sample_mode=balanced）。
    """
    counts = np.bincount(y, minlength=NUM_ACTIONS)
    max_c = int(counts.max())
    idx_parts: list[np.ndarray] = []
    for c in range(NUM_ACTIONS):
        if counts[c] == 0:
            continue
        rep = int(np.ceil(max_c / counts[c]))
        idx_parts.append(np.tile(np.where(y == c)[0], rep))
    idx = np.concatenate(idx_parts)
    return X[idx], y[idx]


def sample_training_data(
    X_tr: np.ndarray, y_tr: np.ndarray, cfg: dict
) -> tuple[np.ndarray, np.ndarray, str, str]:
    """按 ``training.sample_mode`` 决定训练集是否/如何物理拷贝（torch 与 gbdt 共用）。

    mode:
      - ``none``     → 不拷贝，返回原训练集（class_weight 由调用方恢复生效）
      - ``real``     → 按 ``training.sample_proportions`` 拉游戏内现实比例（默认）
      - ``balanced`` → 1:1 均衡拷贝（每个已观测类拷贝到最频繁类条数）

    mode 缺省时兼容旧配置：有 sample_proportions 视为 ``real``，否则 ``none``。
    返回 ``(Xo, yo, mode, desc)``。
    """
    t = cfg["training"]
    mode = t.get("sample_mode")
    sp = t.get("sample_proportions")
    if mode is None:
        mode = "real" if sp else "none"
    if mode == "none":
        return X_tr, y_tr, mode, "不拷贝"
    if mode == "real":
        if not sp:
            raise ValueError("training.sample_mode=real 需要配置 training.sample_proportions")
        Xo, yo = oversample_by_proportions(X_tr, y_tr, sp)
        return Xo, yo, mode, f"按目标比例物理拷贝（{len(Xo)} 条，{len(sp)} 类）"
    if mode == "balanced":
        Xo, yo = oversample_balanced(X_tr, y_tr)
        return Xo, yo, mode, f"1:1 均衡拷贝（{len(Xo)} 条）"
    raise ValueError(f"未知 training.sample_mode: {mode!r}（可选 none/real/balanced）")


def train_torch(
    net: nn.Module,
    X_tr, y_tr, X_va, y_va, X_te, y_te,
    cfg, seed: int,
) -> tuple[dict, dict]:
    """共享训练循环：Adam / 交叉熵 / 早停（验证 loss）/ 测试评估。

    返回 ``(metrics, best_state)``：
    - metrics: {"val_loss", "val_acc", "test_acc"}
    - best_state: 早停最优的 state_dict（CPU），供调用方落盘

    ckpt 所需的模型结构 meta 由调用方（TorchDecisionModel.save）写入。
    """
    from rich.console import Console
    from rich.progress import BarColumn, Progress, TextColumn, TimeElapsedColumn

    torch.manual_seed(seed)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    net = net.to(device)
    console = Console()
    console.print(f"    设备: {device}")

    val_loader = DataLoader(WorkerDecisionDataset(X_va, y_va),
                            batch_size=cfg["training"]["batch_size"], shuffle=False)

    # 训练集物理拷贝模式（none/real/balanced）：real 按现实比例、balanced 1:1 均衡
    X_tr, y_tr, sample_mode, sample_desc = sample_training_data(X_tr, y_tr, cfg)
    train_loader = DataLoader(WorkerDecisionDataset(X_tr, y_tr),
                              batch_size=cfg["training"]["batch_size"], shuffle=True)
    if sample_mode == "none":
        class_weights = compute_class_weights(
            y_tr, cfg["training"].get("class_weight", "none"), n_classes=NUM_ACTIONS)
    else:
        class_weights = None  # 拷贝已硬控类分布，避免与 loss 权重双重加权
        console.print(f"    训练采样: {sample_desc}")

    optimizer = torch.optim.Adam(
        net.parameters(),
        lr=cfg["training"]["learning_rate"],
        weight_decay=cfg["training"]["weight_decay"],
    )
    criterion = nn.CrossEntropyLoss(
        weight=torch.as_tensor(class_weights, dtype=torch.float32, device=device)
        if class_weights is not None else None,
    )

    epochs = cfg["training"]["epochs"]
    patience = cfg["training"]["patience"]
    best_val_loss = float("inf")
    best_val_acc = 0.0
    best_state = None
    bad_epochs = 0
    early_stop_epoch = None

    for epoch in range(1, epochs + 1):
        net.train()
        total_loss = 0.0
        with Progress(
            TextColumn("[progress.description]{task.description}"),
            BarColumn(),
            TextColumn("[progress.percentage]{task.percentage:>3.0f}%"),
            TimeElapsedColumn(),
            console=console,
            transient=True,
        ) as progress:
            task = progress.add_task(f"epoch {epoch}/{epochs}", total=len(train_loader))
            for xb, yb in train_loader:
                xb, yb = xb.to(device), yb.to(device)
                optimizer.zero_grad()
                logits = net(xb)
                loss = criterion(logits, yb)
                loss.backward()
                optimizer.step()
                total_loss += loss.item() * len(yb)
                progress.update(task, advance=1)
        avg_loss = total_loss / len(y_tr)

        net.eval()
        val_loss = 0.0
        correct = 0
        total = 0
        with torch.no_grad():
            for xb, yb in val_loader:
                xb, yb = xb.to(device), yb.to(device)
                logits = net(xb)
                val_loss += criterion(logits, yb).item() * len(yb)
                pred = logits.argmax(dim=1)
                correct += (pred == yb).sum().item()
                total += len(yb)
        val_loss /= total
        val_acc = correct / total

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_val_acc = val_acc
            best_state = {k: v.clone() for k, v in net.state_dict().items()}
            bad_epochs = 0
        else:
            bad_epochs += 1

        console.print(
            f"    epoch [bold]{epoch:3d}[/bold]/{epochs} "
            f"loss=[white]{avg_loss:.4f}[/white]  "
            f"val_loss=[cyan]{val_loss:.4f}[/cyan]  "
            f"val_acc=[yellow]{val_acc:.4f}[/yellow]  "
            f"best_loss=[green]{best_val_loss:.4f}[/green]"
        )

        if bad_epochs >= patience:
            early_stop_epoch = epoch
            break

    if early_stop_epoch is not None:
        console.print(
            f"    [red]早停于 epoch {early_stop_epoch}[/red]"
            f"（val_loss 连续 {patience} 轮不降）"
        )

    net.load_state_dict(best_state)
    net.eval()

    # 独立现实测试集评估
    test_loader = DataLoader(WorkerDecisionDataset(X_te, y_te),
                             batch_size=cfg["training"]["batch_size"], shuffle=False)
    correct = 0
    total = 0
    with torch.no_grad():
        for xb, yb in test_loader:
            xb, yb = xb.to(device), yb.to(device)
            pred = net(xb).argmax(dim=1)
            correct += (pred == yb).sum().item()
            total += len(yb)
    test_acc = correct / total

    results = {"val_loss": best_val_loss, "val_acc": best_val_acc, "test_acc": test_acc}
    return results, best_state
