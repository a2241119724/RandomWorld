"""模型抽象基类：统一三种模型的训练/评估/保存/导出接口。

新增模型只需：实现 ``DecisionModel`` 子类 → ``@register`` → model_config.yaml
加超参段；train / evaluate / export / visualize 因遍历注册表自动生效，
无需改任何分发代码。
"""
from __future__ import annotations

from abc import ABC, abstractmethod

import numpy as np


class DecisionModel(ABC):
    """所有模型的统一门面。具体模型负责实现与各自后端（sklearn/torch）的差异。"""

    name: str = ""              # 注册名（registry key，如 "mlp"）
    filename: str = ""          # 持久化文件名（"mlp.pt"）
    section: str = ""           # model_config.yaml 中的超参段名（"mlp"）

    # ---- 构造 ----
    @classmethod
    @abstractmethod
    def from_config(cls, cfg, input_dim: int, num_actions: int, seed: int | None = None):
        """从 model_config 构造未训练模型实例。"""

    # ---- 训练 / 推理 ----
    @abstractmethod
    def fit(self, X_tr, y_tr, X_va, y_va, X_te, y_te, cfg, seed: int) -> dict:
        """在训练集上训练，返回指标 dict（含 test_acc 等）。"""

    @abstractmethod
    def predict(self, X) -> np.ndarray:
        """返回 argmax 类别索引，shape (N,) int64。"""

    @abstractmethod
    def predict_proba(self, X) -> np.ndarray:
        """返回每类概率，shape (N, num_actions) float32。"""

    # ---- 持久化 ----
    @abstractmethod
    def save(self, export_dir, cfg, seed: int):
        """持久化训练产物，返回产物路径。"""

    @classmethod
    @abstractmethod
    def load(cls, export_dir, cfg=None, device="auto"):
        """从产物加载已训练模型；产物不存在返回 None。"""

    # ---- 导出（Unity 侧产物）----
    @abstractmethod
    def export(self, export_dir, cfg):
        """导出 Unity 推理产物（ONNX / weights.json / .bytes）。"""

    # ---- 可视化 ----
    @abstractmethod
    def feature_importance(self) -> np.ndarray:
        """每特征重要性，shape (input_dim,)。"""

    @abstractmethod
    def structure(self) -> dict:
        """结构描述（visualize 用），必须含 input / output 维度。"""

    def describe(self) -> dict:
        return {"name": self.name, **self.structure()}
