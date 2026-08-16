"""效用函数 baseline：每个行为一个线性评分 score_a = w_a · x + b_a，取最高分。

用 sklearn 的多分类逻辑回归（multinomial）实现 —— 数学上等价于「per-class 线性
效用函数 + softmax」，学到的 ``coef_`` 即各行为的效用权重向量（后续可用 GA/贝叶斯
优化对权重调参，这是遗传算法的正确位置）。
"""
from __future__ import annotations

import numpy as np
from sklearn.linear_model import LogisticRegression


class UtilityBaseline:
    def __init__(self, C: float = 1.0, max_iter: int = 2000, seed: int | None = None):
        # 注：sklearn >= 1.9 已移除 multi_class 参数，多分类默认即 multinomial（softmax）。
        self.model = LogisticRegression(
            solver="lbfgs",
            C=C,
            max_iter=max_iter,
            random_state=seed,
        )

    def fit(self, X: np.ndarray, y: np.ndarray) -> "UtilityBaseline":
        self.model.fit(X, y)
        return self

    def predict(self, X: np.ndarray) -> np.ndarray:
        return self.model.predict(X)

    def predict_proba(self, X: np.ndarray) -> np.ndarray:
        return self.model.predict_proba(X)

    def scores(self, X: np.ndarray) -> np.ndarray:
        """线性效用评分（decision_function），shape (N, num_actions)。"""
        return self.model.decision_function(X)

    @property
    def weights(self) -> np.ndarray:
        """各行为的效用权重矩阵，shape (num_actions, input_dim)。"""
        return self.model.coef_

    @property
    def bias(self) -> np.ndarray:
        """各行为的偏置，shape (num_actions,)。"""
        return self.model.intercept_
