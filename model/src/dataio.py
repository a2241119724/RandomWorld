"""数据 IO：加载训练/验证/测试数据与确定性切分。

新语义（相对旧的 20 万随机池一次性 split）：
- 训练集 = 边界覆盖集（``train_x.npy``/``train_y.npy``），内部确定性切 train/val（val 供早停监控）。
- 测试集 = 独立现实分布集（``test_x.npy``/``test_y.npy``），直接加载，绝不混入训练切分。
"""
from __future__ import annotations

import numpy as np


def load_train_val(cfg, seed: int):
    """加载训练边界集并确定性切分为 ``(X_tr,y_tr),(X_va,y_va)``。"""
    processed = cfg.processed_dir
    X = np.load(processed / "train_x.npy")
    y = np.load(processed / "train_y.npy")
    return split_arrays(X, y, cfg["split"]["val_ratio"], seed)


def load_test(cfg):
    """加载独立现实分布测试集 ``(X_te, y_te)``。"""
    processed = cfg.processed_dir
    return np.load(processed / "test_x.npy"), np.load(processed / "test_y.npy")


def split_arrays(X, y, val_ratio: float, seed: int):
    """确定性洗牌切分为 ``(X_tr,y_tr),(X_va,y_va)``（同 seed 结果一致）。"""
    rng = np.random.default_rng(seed)
    n = len(y)
    idx = rng.permutation(n)
    n_val = int(n * val_ratio)
    val_idx = idx[:n_val]
    train_idx = idx[n_val:]
    return (X[train_idx], y[train_idx]), (X[val_idx], y[val_idx])
