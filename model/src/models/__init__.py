"""模型定义：效用函数 baseline 与 MLP 策略网络。"""
from .baseline_utility import UtilityBaseline

# MLP 依赖 torch；未安装时 baseline 仍可独立训练/评估
try:
    from .mlp import WorkerMLP
except ImportError:  # pragma: no cover
    WorkerMLP = None

__all__ = ["UtilityBaseline", "WorkerMLP"]
