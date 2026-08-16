"""特征层：读 feature_schema.yaml，从状态字典生成归一化特征向量。

新增输入特征只需在 config/feature_schema.yaml 加一个条目，本模块自动生效，
无需改动代码。
"""
from __future__ import annotations

import math
from pathlib import Path
from typing import Any

import numpy as np
import yaml

# 项目根（model/ 目录），用于相对路径解析
PROJECT_ROOT = Path(__file__).resolve().parent.parent


class FeatureSchemaError(ValueError):
    """特征 schema 或状态字典不合法时抛出。"""


class FeatureSchema:
    """已解析的特征定义，可被反复用于向量化。"""

    def __init__(self, features: list[dict[str, Any]]):
        self.features = features
        self._validate()

    # ---- 读取 ----
    @classmethod
    def load(cls, path: str | Path) -> "FeatureSchema":
        with open(PROJECT_ROOT / path, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)
        return cls(data["features"])

    def _validate(self) -> None:
        seen = set()
        for feat in self.features:
            name = feat["name"]
            if name in seen:
                raise FeatureSchemaError(f"重复的特征名: {name}")
            seen.add(name)
            kind = feat["kind"]
            if kind == "onehot" and "categories" not in feat:
                raise FeatureSchemaError(f"onehot 特征 {name} 缺少 categories")
            if kind in ("minmax", "log") and ("min" if kind == "minmax" else "max") not in feat:
                raise FeatureSchemaError(f"{kind} 特征 {name} 缺少边界参数")

    # ---- 维度 ----
    @property
    def dim(self) -> int:
        """特征向量总维度。"""
        total = 0
        for feat in self.features:
            if feat["kind"] == "onehot":
                total += len(feat["categories"])
            else:
                total += 1
        return total

    def feature_names(self) -> list[str]:
        """每个输出维度的名字（one-hot 展开为 `name=categ`）。"""
        names: list[str] = []
        for feat in self.features:
            if feat["kind"] == "onehot":
                for c in feat["categories"]:
                    names.append(f"{feat['name']}={c}")
            else:
                names.append(feat["name"])
        return names

    # ---- 向量化 ----
    def encode(self, state: dict[str, Any]) -> np.ndarray:
        """把单个状态字典编码为 [dim] 的 float32 特征向量。"""
        parts: list[np.ndarray] = []
        for feat in self.features:
            key = feat["key"]
            if key not in state:
                raise FeatureSchemaError(f"状态字典缺少字段: {key}")
            value = state[key]
            kind = feat["kind"]

            if kind == "ratio":
                max_key = feat["max_key"]
                if max_key not in state:
                    raise FeatureSchemaError(f"状态字典缺少字段: {max_key}")
                maxv = state[max_key]
                v = _ratio(value, maxv)
                parts.append(np.array([v], dtype=np.float32))
            elif kind == "minmax":
                v = _minmax(value, feat["min"], feat["max"])
                parts.append(np.array([v], dtype=np.float32))
            elif kind == "log":
                v = _log(value, feat["max"])
                parts.append(np.array([v], dtype=np.float32))
            elif kind == "passthrough":
                parts.append(np.array([float(value)], dtype=np.float32))
            elif kind == "onehot":
                parts.append(self._onehot(value, feat["categories"]))
            else:
                raise FeatureSchemaError(f"未知 kind: {kind}（特征 {feat['name']}）")

        return np.concatenate(parts).astype(np.float32)

    @staticmethod
    def _onehot(value: Any, categories: list[Any]) -> np.ndarray:
        # 归一化类别到字符串比较，容忍 int/str 混用
        value_str = str(value)
        vec = np.zeros(len(categories), dtype=np.float32)
        for i, c in enumerate(categories):
            if str(c) == value_str:
                vec[i] = 1.0
                return vec
        # 未知类别 → 全零（不报错，保持鲁棒）
        return vec


def _ratio(value: float, maxv: float) -> float:
    if maxv <= 0:
        return 0.0
    return float(np.clip(value / maxv, 0.0, 1.0))


def _minmax(value: float, lo: float, hi: float) -> float:
    if hi <= lo:
        return 0.0
    return float(np.clip((value - lo) / (hi - lo), 0.0, 1.0))


def _log(value: float, maxv: float) -> float:
    # log1p 对非负量做对数压缩；maxv <= 0 时退化为 0
    if maxv <= 0:
        return 0.0
    v = float(np.clip(value, 0.0, maxv))
    return float(math.log1p(v) / math.log1p(maxv))


# ---- 便捷入口 ----
def build_schema(schema_path: str = "config/feature_schema.yaml") -> FeatureSchema:
    return FeatureSchema.load(schema_path)


def encode_many(schema: FeatureSchema, states: list[dict[str, Any]]) -> np.ndarray:
    """批量编码状态列表为 [N, dim] 特征矩阵。"""
    return np.vstack([schema.encode(s) for s in states])
