"""数据 IO：加载训练/验证/测试数据与确定性切分。

切分语义：
- 训练集 = 边界覆盖集（``train_x.npy``/``train_y.npy``），**全部用于训练**（不再内部切 val）。
- 现实分布集（``test_x.npy``/``test_y.npy``）确定性地切成两份：前 ``split.val_ratio``
  做验证集（供早停监控——反映中间态真实泛化，而非纯极值），其余做独立测试集
  （最终评估）。val/test 永不混入训练切分。
"""
from __future__ import annotations

import numpy as np


def _load_arrays(processed):
    return (np.load(processed / "train_x.npy"), np.load(processed / "train_y.npy"),
            np.load(processed / "test_x.npy"), np.load(processed / "test_y.npy"))


def _split_test(X_te_full, y_te_full, cfg):
    """现实分布集确定性切分：前 ``val_ratio`` 做验证，其余做独立测试。

    索引切（test 集由 generate_data 随机 iid 生成，前/后无系统性偏差），
    不依赖 seed 参数——所有调用方（train/evaluate/visualize）结果一致。
    """
    ratio = float(cfg["split"]["val_ratio"])
    n_val = int(len(y_te_full) * ratio)
    val = X_te_full[:n_val], y_te_full[:n_val]
    test = X_te_full[n_val:], y_te_full[n_val:]
    return val, test


def load_train_val(cfg, seed: int):
    """返回 ``(X_tr,y_tr),(X_va,y_va)``：训练集 = 全部边界集；验证集 = 现实分布集前段。

    ``seed`` 保留签名兼容（训练集全量、验证集按索引切，均已确定）。
    """
    X, y, X_te_full, y_te_full = _load_arrays(cfg.processed_dir)
    (X_va, y_va), _ = _split_test(X_te_full, y_te_full, cfg)
    return (X, y), (X_va, y_va)


def load_test(cfg):
    """返回独立现实分布测试集（``split.val_ratio`` 切出的后段，与验证集不相交）。"""
    _, _, X_te_full, y_te_full = _load_arrays(cfg.processed_dir)
    _, (X_te, y_te) = _split_test(X_te_full, y_te_full, cfg)
    return X_te, y_te
