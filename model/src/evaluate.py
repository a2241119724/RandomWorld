"""评估与对比：在独立现实测试集上对比注册表内所有已训练模型。

用法（在 model/ 目录下）：
    python src/evaluate.py

指标：准确率 / Top-3 / KL 散度 / per-class 召回 + macro-F1 / 行为分布。
新增模型注册后自动纳入，无需改本文件。
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT))

from src.actions import ACTIONS, ACTION_LABELS_ZH  # noqa: E402
from src.config import ModelConfig  # noqa: E402
from src.dataio import load_test  # noqa: E402
from src.models import list_models, load_model  # noqa: E402


def top_k_accuracy(y_true: np.ndarray, proba: np.ndarray, k: int) -> float:
    top = np.argsort(proba, axis=1)[:, -k:]
    hits = np.array([y_true[i] in top[i] for i in range(len(y_true))])
    return float(hits.mean())


def kl_divergence(y_true: np.ndarray, proba: np.ndarray) -> float:
    """真实标签分布 vs 模型预测平均分布的 KL 散度（越小越一致）。"""
    n_classes = proba.shape[1]
    true_dist = np.bincount(y_true, minlength=n_classes).astype(float)
    true_dist = true_dist / true_dist.sum()
    pred_dist = np.clip(proba.mean(axis=0), 1e-9, 1.0)
    # 真实分布为 0 的类（该行为在测试集从未出现）对 KL 贡献恒为 0（0·log 约定）。
    mask = true_dist > 0
    terms = np.zeros_like(true_dist)
    terms[mask] = true_dist[mask] * np.log(true_dist[mask] / pred_dist[mask])
    return float(terms.sum())


def per_class_report(y_true: np.ndarray, pred: np.ndarray, n_classes: int) -> dict:
    """per-class precision/recall/F1 + macro-F1。"""
    cm = np.zeros((n_classes, n_classes), dtype=np.int64)
    for t, p in zip(y_true, pred):
        cm[t, p] += 1

    recall, precision, f1 = {}, {}, {}
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


def evaluate_model(model, X_te, y_te) -> dict:
    """对任意 DecisionModel 计算指标。"""
    proba = model.predict_proba(X_te)
    pred = proba.argmax(axis=1)
    return {
        "acc": float((pred == y_te).mean()),
        "top3": top_k_accuracy(y_te, proba, 3),
        "kl": kl_divergence(y_te, proba),
        "proba": proba,
    }


def main():
    cfg = ModelConfig()
    export_dir = cfg.export_dir

    X_te, y_te = load_test(cfg)
    n_classes = len(ACTIONS)
    print(f"[evaluate] 现实测试集 {len(y_te)} 条，行为数 {n_classes}\n")

    rows = []
    for name in list_models():
        model = load_model(name, export_dir, cfg.raw)
        if model is None:
            print(f"[evaluate] 未找到 {name} 产物，跳过")
            continue
        r = evaluate_model(model, X_te, y_te)
        rep = per_class_report(y_te, r["proba"].argmax(axis=1), n_classes)
        r["_recall"] = rep["recall"]
        r["_macro_f1"] = rep["macro_f1"]
        rows.append((name, r))

    if not rows:
        print("[evaluate] 没有任何已训练模型，请先运行 src/train.py")
        return

    print(f"{'模型':<16s} {'准确率':>8s} {'Top-3':>8s} {'KL散度':>8s} {'macro-F1':>10s}")
    print("-" * 56)
    for name, r in rows:
        print(f"{name:<16s} {r['acc']*100:7.2f}% {r['top3']*100:7.2f}% "
              f"{r['kl']:8.4f} {r['_macro_f1']*100:9.2f}%")

    # per-class 召回率
    print(f"\n[稀有类召回] 每行为 recall (%)（长尾标签体检）:")
    print(f"{'行为':<16s} " + " ".join(f"{name[:10]:>11s}" for name, _ in rows))
    for i, a in enumerate(ACTIONS):
        zh = ACTION_LABELS_ZH.get(a, a)
        line = f"{a:<16s}"
        for _, r in rows:
            line += f" {r['_recall'][i]*100:10.2f}%"
        print(line)

    # 行为分布对比
    print(f"\n[行为分布] 真实 vs 预测（Top-1）:")
    true_dist = np.bincount(y_te, minlength=n_classes).astype(float) / len(y_te)
    print(f"{'行为':<16s} {'真实':>8s} " + " ".join(f"{name[:10]:>11s}" for name, _ in rows))
    for i, a in enumerate(ACTIONS):
        zh = ACTION_LABELS_ZH.get(a, a)
        line = f"{a:<16s} {true_dist[i]*100:7.2f}%"
        for _, r in rows:
            pred_dist = np.bincount(r["proba"].argmax(axis=1),
                                    minlength=n_classes).astype(float) / len(y_te)
            line += f" {pred_dist[i]*100:10.2f}%"
        print(line)


if __name__ == "__main__":
    main()
