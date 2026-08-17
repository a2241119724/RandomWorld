"""训练入口：训练效用函数 baseline 或 MLP。

用法（在 model/ 目录下）：
    python src/train.py --model baseline
    python src/train.py --model mlp
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import yaml

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.models.baseline_utility import UtilityBaseline  # noqa: E402
from src.actions import NUM_ACTIONS  # noqa: E402


def load_data(cfg: dict):
    processed = ROOT / cfg["paths"]["processed_dir"]
    X = np.load(processed / "X.npy")
    y = np.load(processed / "y.npy")
    return X, y


def split(X: np.ndarray, y: np.ndarray, val_ratio: float, test_ratio: float, seed: int):
    rng = np.random.default_rng(seed)
    n = len(y)
    idx = rng.permutation(n)
    n_test = int(n * test_ratio)
    n_val = int(n * val_ratio)
    test_idx = idx[:n_test]
    val_idx = idx[n_test:n_test + n_val]
    train_idx = idx[n_test + n_val:]
    return (X[train_idx], y[train_idx]), (X[val_idx], y[val_idx]), (X[test_idx], y[test_idx])


def accuracy(y_true: np.ndarray, y_pred: np.ndarray) -> float:
    return float((y_true == y_pred).mean())


def compute_class_weights(y: np.ndarray, mode: str) -> np.ndarray | None:
    """按训练集类别频率计算交叉熵权重，缓解长尾标签被大类主导。

    mode 取值：
      - ``none``       → 返回 None（不加权）
      - ``sqrt_inv``   → 1/sqrt(freq)，温和（稀有类权重被拉高但不极端，推荐）
      - ``inverse``    → 1/freq，激进（接近完全平衡，可能过度触发稀有行为）

    权重归一化到加权平均 = 1，保持 loss 量级不随加权放大，无需重调 learning rate。
    只在训练集上计算，避免从验证/测试集泄漏类别分布信息。
    """
    if mode in (None, "none", ""):
        return None
    counts = np.bincount(y)
    freqs = counts / counts.sum()
    if mode == "sqrt_inv":
        w = 1.0 / np.sqrt(freqs + 1e-6)
    elif mode == "inverse":
        w = 1.0 / (freqs + 1e-6)
    else:
        raise ValueError(f"未知 class_weight 模式: {mode}（可选 none/sqrt_inv/inverse）")
    w = w / (w * freqs).sum()  # 加权平均 = 1
    return w


def train_baseline(X, y, cfg, seed):
    (X_tr, y_tr), (X_va, y_va), (X_te, y_te) = split(
        X, y, cfg["split"]["val_ratio"], cfg["split"]["test_ratio"], seed)

    model = UtilityBaseline(
        C=cfg["baseline"]["C"], max_iter=cfg["baseline"]["max_iter"], seed=seed)
    model.fit(X_tr, y_tr)

    results = {
        "train_acc": accuracy(y_tr, model.predict(X_tr)),
        "val_acc": accuracy(y_va, model.predict(X_va)),
        "test_acc": accuracy(y_te, model.predict(X_te)),
    }

    # 保存完整模型（供 evaluate 复用）与权重（供 export 复用）
    import joblib
    export_dir = ROOT / cfg["paths"]["export_dir"]
    export_dir.mkdir(parents=True, exist_ok=True)
    joblib.dump(model, export_dir / "baseline.joblib")
    np.savez(
        export_dir / "baseline.npz",
        coef=model.weights, intercept=model.bias,
    )
    return model, results


def _train_torch(model, X, y, cfg, seed, ckpt_name, ckpt_meta):
    """共享的 PyTorch 训练循环：split / DataLoader / Adam / 交叉熵 / 早停 / 测试评估 / 保存。

    ckpt_meta 为随 checkpoint 一起保存的模型结构参数（重建模型用），含 input_dim/num_actions。
    """
    import torch
    import torch.nn as nn
    from torch.utils.data import DataLoader
    from src.dataset import WorkerDecisionDataset
    from rich.console import Console
    from rich.progress import BarColumn, Progress, TextColumn, TimeElapsedColumn

    torch.manual_seed(seed)

    # 有 CUDA 版 torch 时自动用 GPU，否则回退 CPU
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = model.to(device)
    console = Console()
    console.print(f"    设备: {device}")

    (X_tr, y_tr), (X_va, y_va), (X_te, y_te) = split(
        X, y, cfg["split"]["val_ratio"], cfg["split"]["test_ratio"], seed)

    train_loader = DataLoader(WorkerDecisionDataset(X_tr, y_tr),
                              batch_size=cfg["training"]["batch_size"], shuffle=True)
    val_loader = DataLoader(WorkerDecisionDataset(X_va, y_va),
                            batch_size=cfg["training"]["batch_size"], shuffle=False)

    optimizer = torch.optim.Adam(
        model.parameters(),
        lr=cfg["training"]["learning_rate"],
        weight_decay=cfg["training"]["weight_decay"],
    )
    # 类别权重：缓解长尾标签被大类主导（默认 sqrt_inv，见 compute_class_weights）
    class_weights = compute_class_weights(
        y_tr, cfg["training"].get("class_weight", "none"))
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

    # 每个 epoch 一条原地刷新的活进度条（batch 级），跑完打印一行指标摘要
    early_stop_epoch = None
    for epoch in range(1, epochs + 1):
        model.train()
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
                logits = model(xb)
                loss = criterion(logits, yb)
                loss.backward()
                optimizer.step()
                total_loss += loss.item() * len(yb)
                progress.update(task, advance=1)
        avg_loss = total_loss / len(y_tr)

        model.eval()
        val_loss = 0.0
        correct = 0
        total = 0
        with torch.no_grad():
            for xb, yb in val_loader:
                xb, yb = xb.to(device), yb.to(device)
                logits = model(xb)
                val_loss += criterion(logits, yb).item() * len(yb)
                pred = logits.argmax(dim=1)
                correct += (pred == yb).sum().item()
                total += len(yb)
        val_loss /= total
        val_acc = correct / total

        # 早停监控验证 loss（比 acc 更稳，且加类别权重后方向性不漂移）
        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_val_acc = val_acc
            best_state = {k: v.clone() for k, v in model.state_dict().items()}
            bad_epochs = 0
        else:
            bad_epochs += 1

        # 每个 epoch 一行指标摘要（保留历史，便于观察 loss/val_loss/val_acc 变化）
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

    model.load_state_dict(best_state)
    model.eval()

    # 测试集评估
    test_loader = DataLoader(WorkerDecisionDataset(X_te, y_te),
                             batch_size=cfg["training"]["batch_size"], shuffle=False)
    correct = 0
    total = 0
    with torch.no_grad():
        for xb, yb in test_loader:
            xb, yb = xb.to(device), yb.to(device)
            pred = model(xb).argmax(dim=1)
            correct += (pred == yb).sum().item()
            total += len(yb)
    test_acc = correct / total

    # 保存
    export_dir = ROOT / cfg["paths"]["export_dir"]
    export_dir.mkdir(parents=True, exist_ok=True)
    torch.save({
        "state_dict": {k: v.cpu() for k, v in best_state.items()},
        **ckpt_meta,
    }, export_dir / ckpt_name)

    results = {"val_loss": best_val_loss, "val_acc": best_val_acc, "test_acc": test_acc}
    return model, results


def train_mlp(X, y, cfg, seed):
    from src.models.mlp import WorkerMLP

    input_dim = X.shape[1]
    model = WorkerMLP(
        input_dim=input_dim,
        num_actions=NUM_ACTIONS,
        hidden_dims=cfg["mlp"]["hidden_dims"],
        dropout=cfg["mlp"]["dropout"],
        activation=cfg["mlp"]["activation"],
    )
    ckpt_meta = {
        "input_dim": input_dim,
        "num_actions": NUM_ACTIONS,
        "hidden_dims": cfg["mlp"]["hidden_dims"],
        "activation": cfg["mlp"]["activation"],
    }
    return _train_torch(model, X, y, cfg, seed, "mlp.pt", ckpt_meta)


def train_attention(X, y, cfg, seed):
    from src.models.attention import WorkerAttention

    input_dim = X.shape[1]
    a = cfg["attention"]
    model = WorkerAttention(
        input_dim=input_dim,
        num_actions=NUM_ACTIONS,
        d_model=a["d_model"],
        n_heads=a["n_heads"],
        n_layers=a["n_layers"],
        dim_feedforward=a["dim_feedforward"],
        dropout=a["dropout"],
        head_dims=a["head_dims"],
    )
    ckpt_meta = {
        "input_dim": input_dim,
        "num_actions": NUM_ACTIONS,
        "d_model": a["d_model"],
        "n_heads": a["n_heads"],
        "n_layers": a["n_layers"],
        "dim_feedforward": a["dim_feedforward"],
        "head_dims": a["head_dims"],
    }
    return _train_torch(model, X, y, cfg, seed, "attention.pt", ckpt_meta)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", choices=["baseline", "mlp", "attention"], required=True)
    parser.add_argument("--config", default="config/model_config.yaml")
    args = parser.parse_args()

    cfg = yaml.safe_load((ROOT / args.config).read_text(encoding="utf-8"))
    seed = cfg["data"]["seed"]

    print(f"[train] 加载数据 ...")
    X, y = load_data(cfg)
    print(f"[train] X={X.shape}, y={y.shape}, 行为数={NUM_ACTIONS}")

    if args.model == "baseline":
        print("[train] 训练效用函数 baseline（逻辑回归）...")
        model, results = train_baseline(X, y, cfg, seed)
    elif args.model == "mlp":
        print("[train] 训练 MLP ...")
        model, results = train_mlp(X, y, cfg, seed)
    else:
        print("[train] 训练注意力模型（FT-Transformer）...")
        model, results = train_attention(X, y, cfg, seed)

    print(f"[train] 完成 {args.model}: {results}")


if __name__ == "__main__":
    main()
