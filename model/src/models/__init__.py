"""模型包：导入各模型模块触发注册，并导出 registry 工厂。

新增模型只需在 models/ 下实现 DecisionModel 子类 + @register，然后在本文件
import 该模块即可自动纳入 train/evaluate/export/visualize 流程。
"""
from .registry import MODEL_REGISTRY, register, create_model, load_model, list_models
from .base import DecisionModel

from .mlp import MlpModel  # noqa: F401  （触发注册）
from .attention import AttentionModel  # noqa: F401

__all__ = [
    "MODEL_REGISTRY", "register", "create_model", "load_model", "list_models",
    "DecisionModel", "MlpModel", "AttentionModel",
]
