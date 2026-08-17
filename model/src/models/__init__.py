"""模型定义：效用函数 baseline、MLP 策略网络与注意力模型。"""
from .baseline_utility import UtilityBaseline

# MLP / 注意力依赖 torch；未安装时 baseline 仍可独立训练/评估
try:
    from .mlp import WorkerMLP
except ImportError:  # pragma: no cover
    WorkerMLP = None

try:
    from .attention import WorkerAttention
except ImportError:  # pragma: no cover
    WorkerAttention = None

__all__ = ["UtilityBaseline", "WorkerMLP", "WorkerAttention"]
