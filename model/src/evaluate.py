"""评估与对比：在测试集上对比效用函数 baseline 与 MLP。

用法（在 model/ 目录下）：
    python src/evaluate.py
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
import yaml

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_LABELS_ZH  # noqa: E402
from src.train import load_data, split  # noqa: E402


def top_k_accuracy(y_true: np.ndarray, proba: np.ndarray, k: int) -> float:
    top = np.argsort(proba, axis=1)[:, -k:]
    hits = np.array([y_true[i] in top[i] for i in range(len(y_true))])
    return float(hits.mean())


def kl_divergence(y_true: np.ndarray, proba: np.ndarray) -> float:
    """真实标签分布 vs 模型预测平均分布的 KL 散度（越小越一致）。"""
    n_classes = proba.shape[1]
    true_dist = np.bincount(y_true, minlength=n_classes).astype(float)
    true_dist = true_dist / true_dist.sum()
    pred_dist = proba.mean(axis=0)
    # 加平滑避免 log(0)
    eps = 1e-9
    pred_dist = np.clip(pred_dist, eps, 1.0)
    return float(np.sum(true_dist * np.log(true_dist / pred_dist)))


def per_class_report(y_true: np.ndarray, pred: np.ndarray, n_classes: int) -> dict:
    """per-class precision/recall/F1 + macro-F1。

    长尾标签下 accuracy 会被大类主导，稀有类（post_bounty/accept_bounty/store/...）
    是否真正学到，只能靠 per-class 召回暴露。返回 dict 供打印。
    """
    cm = np.zeros((n_classes, n_classes), dtype=np.int64)
    for t, p in zip(y_true, pred):
        cm[t, p] += 1

    recall = {}
    precision = {}
    f1 = {}
    for i in range(n_classes):
        tp = cm[i, i]
        fn = cm[i].sum() - tp
        fp = cm[:, i].sum() - tp
        rec = tp / (tp + fn) if (tp + fn) > 0 else 0.0
        prec = tp / (tp + fp) if (tp + fp) > 0 else 0.0
        recall[i] = rec
        precision[i] = prec
        f1[i] = 2 * prec * rec / (prec + rec) if (prec + rec) > 0 else 0.0

    return {
        "cm": cm,
        "recall": recall,
        "precision": precision,
        "f1": f1,
        "macro_f1": float(np.mean(list(f1.values()))),
    }


def evaluate_baseline(X_te, y_te, export_dir: Path) -> dict | None:
    import joblib
    path = export_dir / "baseline.joblib"
    if not path.exists():
        print("[evaluate] 未找到 baseline.joblib，跳过")
        return None
    model = joblib.load(path)
    proba = model.predict_proba(X_te)
    pred = model.predict(X_te)
    return {
        "acc": float((pred == y_te).mean()),
        "top3": top_k_accuracy(y_te, proba, 3),
        "kl": kl_divergence(y_te, proba),
        "proba": proba,
    }


def _evaluate_torch(ckpt_path: Path, build_model, X_te, y_te, device="auto") -> dict | None:
    """共享的 torch 模型评估：加载 checkpoint、重建模型、算 acc / Top-3 / KL。"""
    import torch
    import torch.nn.functional as F

    if device == "auto":
        device = "cuda" if torch.cuda.is_available() else "cpu"

    if not ckpt_path.exists():
        print(f"[evaluate] 未找到 {ckpt_path.name}，跳过")
        return None
    # weights_only=False：checkpoint 由本仓库 train.py 生成（含自定义 meta 字典），
    # 非外部输入，反序列化风险可控；仅在本机评估自己的产物时使用。
    ckpt = torch.load(ckpt_path, map_location=device, weights_only=False)
    model = build_model(ckpt).to(device)
    model.load_state_dict(ckpt["state_dict"])
    model.eval()

    Xt = torch.as_tensor(X_te, dtype=torch.float32, device=device)
    with torch.no_grad():
        logits = model(Xt)
        proba = F.softmax(logits, dim=1).cpu().numpy()
        pred = logits.argmax(dim=1).cpu().numpy()
    return {
        "acc": float((pred == y_te).mean()),
        "top3": top_k_accuracy(y_te, proba, 3),
        "kl": kl_divergence(y_te, proba),
        "proba": proba,
    }


def evaluate_mlp(X_te, y_te, export_dir: Path, device="auto") -> dict | None:
    from src.models.mlp import WorkerMLP

    def build(ckpt):
        return WorkerMLP(
            input_dim=ckpt["input_dim"],
            num_actions=ckpt["num_actions"],
            hidden_dims=ckpt["hidden_dims"],
            activation=ckpt["activation"],
        )

    return _evaluate_torch(export_dir / "mlp.pt", build, X_te, y_te, device)


def evaluate_attention(X_te, y_te, export_dir: Path, device="auto") -> dict | None:
    from src.models.attention import WorkerAttention

    def build(ckpt):
        return WorkerAttention(
            input_dim=ckpt["input_dim"],
            num_actions=ckpt["num_actions"],
            d_model=ckpt["d_model"],
            n_heads=ckpt["n_heads"],
            n_layers=ckpt["n_layers"],
            dim_feedforward=ckpt["dim_feedforward"],
            head_dims=ckpt["head_dims"],
        )

    return _evaluate_torch(export_dir / "attention.pt", build, X_te, y_te, device)


def main():
    cfg = yaml.safe_load((ROOT / "config" / "model_config.yaml").read_text(encoding="utf-8"))
    export_dir = ROOT / cfg["paths"]["export_dir"]

    X, y = load_data(cfg)
    _, _, (X_te, y_te) = split(
        X, y, cfg["split"]["val_ratio"], cfg["split"]["test_ratio"], cfg["data"]["seed"])

    print(f"[evaluate] 测试集 {len(y_te)} 条，行为数 {len(ACTIONS)}\n")

    rows = []
    base = evaluate_baseline(X_te, y_te, export_dir)
    if base:
        rows.append(("baseline(效用函数)", base))
    mlp = evaluate_mlp(X_te, y_te, export_dir)
    if mlp:
        rows.append(("mlp(神经网络)", mlp))
    att = evaluate_attention(X_te, y_te, export_dir)
    if att:
        rows.append(("attention(注意力)", att))

    if not rows:
        print("[evaluate] 没有任何已训练模型，请先运行 src/train.py")
        return

    print(f"{'模型':<20s} {'准确率':>8s} {'Top-3':>8s} {'KL散度':>8s}")
    print("-" * 48)
    for name, r in rows:
        print(f"{name:<20s} {r['acc']*100:7.2f}% {r['top3']*100:7.2f}% {r['kl']:8.4f}")

    # 每行为召回率 + macro-F1：暴露长尾标签下稀有类是否真正学到
    print(f"\n[稀有类召回] 每行为 recall (%) 与 macro-F1:")
    header = f"{'行为':<16s}"
    for name, r in rows:
        header += f" {name.split('(')[0][:8]:>9s}"
    print(header)
    n_classes = len(ACTIONS)
    for name, r in rows:
        rep = per_class_report(y_te, r["proba"].argmax(axis=1), n_classes)
        r["_recall"] = rep["recall"]
        r["_macro_f1"] = rep["macro_f1"]
    for i, a in enumerate(ACTIONS):
        zh = ACTION_LABELS_ZH.get(a, a)
        line = f"{a:<16s}"
        for name, r in rows:
            line += f" {r['_recall'][i]*100:8.2f}%"
        print(line)
    mf1 = f"{'macro-F1':<16s}"
    for name, r in rows:
        mf1 += f" {r['_macro_f1']*100:8.2f}%"
    print(mf1)

    # 行为分布对比（真实 vs 各模型）
    print("\n[行为分布] 真实 vs 预测（Top-1）:")
    true_dist = np.bincount(y_te, minlength=len(ACTIONS)).astype(float)
    true_dist = true_dist / true_dist.sum()
    print(f"{'行为':<16s} {'真实':>8s}", end="")
    for name, r in rows:
        pred_dist = np.bincount(r["proba"].argmax(axis=1), minlength=len(ACTIONS)).astype(float)
        pred_dist = pred_dist / pred_dist.sum()
        _ = pred_dist
        print(f"{name.split('(')[0]:>12s}", end="")
    print()
    for i, a in enumerate(ACTIONS):
        zh = ACTION_LABELS_ZH.get(a, a)
        line = f"{a:<16s} {true_dist[i]*100:7.2f}%"
        for name, r in rows:
            pred_dist = np.bincount(r["proba"].argmax(axis=1), minlength=len(ACTIONS)).astype(float)
            pred_dist = pred_dist / pred_dist.sum()
            line += f" {pred_dist[i]*100:11.2f}%"
        print(line)


if __name__ == "__main__":
    main()
