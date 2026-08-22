"""模型注册表 + 工厂：新增模型 = 实现 DecisionModel 子类 + ``@register`` + 配置段。

train / evaluate / export / visualize 全部经 ``create_model`` / ``load_model``
按名字分发，不再有 if/elif 硬编码。
"""
from __future__ import annotations

from .base import DecisionModel

MODEL_REGISTRY: dict[str, type[DecisionModel]] = {}


def register(name: str):
    """类装饰器：把 DecisionModel 子类注册到 MODEL_REGISTRY。"""
    def deco(cls):
        if not (isinstance(cls, type) and issubclass(cls, DecisionModel)):
            raise TypeError(f"@register 只能用于 DecisionModel 子类，收到 {cls!r}")
        cls.name = name
        MODEL_REGISTRY[name] = cls
        return cls
    return deco


def list_models() -> list[str]:
    """返回已注册模型名（按注册顺序）。"""
    return list(MODEL_REGISTRY)


def create_model(name: str, cfg, input_dim: int, num_actions: int,
                 seed: int | None = None) -> DecisionModel:
    """工厂：按名字构造未训练模型。"""
    if name not in MODEL_REGISTRY:
        raise KeyError(f"未注册的模型 '{name}'，可选: {list_models()}")
    return MODEL_REGISTRY[name].from_config(cfg, input_dim, num_actions, seed)


def load_model(name: str, export_dir, cfg=None, device="auto") -> DecisionModel | None:
    """按名字加载已训练模型；产物不存在返回 None。"""
    if name not in MODEL_REGISTRY:
        raise KeyError(f"未注册的模型 '{name}'，可选: {list_models()}")
    return MODEL_REGISTRY[name].load(export_dir, cfg, device)
