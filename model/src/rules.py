"""数据采样：纯极值随机组合训练集 + 现实分布测试集。

职责：
- ``derive_state_bounds``：由 feature_schema 推导每个原始 key 的采样边界
  （ratio 的 max_value / dtype:int 等信息直接读 schema，不再依赖规则表）。
- ``generate_training_samples``：纯极值随机组合训练集（连续特征只取 min/max 极值、
  枚举取全值随机、固定值取定值，随机组合 n_train_total 条不同状态）。
- ``generate_test_samples``：现实分布测试集（连续 uniform 小数、int 随机、枚举随机）。

标签默认返回主类 ``idle``；传入 ``label_fn(batch_states)->list[str]`` 可替换
（如 LLM 教师，批量一次调用）。

所有随机均用 ``np.random.default_rng(seed)``，同 seed 结果完全一致。
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import numpy as np

from .actions import ACTION_INDEX

DEFAULT_ACTION = "idle"  # 兜底主类：无规则表后 LLM 失败/无标签时的默认行为


# ---------------------------------------------------------------------------
# 采样边界
# ---------------------------------------------------------------------------
@dataclass
class Bound:
    """单个原始 key 的采样边界。"""
    kind: str                     # "continuous" | "int" | "enum" | "fixed"
    lo: float | None = None
    hi: float | None = None
    categories: list | None = None   # kind=="enum"
    value: Any = None                # kind=="fixed"


def derive_state_bounds(schema) -> dict[str, Bound]:
    """由 feature_schema 推导每个原始 key 的采样边界（schema 自包含，无规则表）。

    ratio → [0, max_value]（max_value 缺省 100，max_key 本身为 fixed）；
    minmax → [min,max]；log → [0,max]；passthrough → [0,1]；onehot → categories。
    ``dtype: int`` 的键按整数采样（int 而非 continuous）。
    """
    bounds: dict[str, Bound] = {}
    for feat in schema.features:
        kind = feat["kind"]
        key = feat["key"]
        if kind == "ratio":
            max_key = feat["max_key"]
            maxv = float(feat.get("max_value", 100.0))
            bounds[key] = Bound("continuous", 0.0, maxv)
            bounds[max_key] = Bound("fixed", value=maxv)
        elif kind == "minmax":
            lo, hi = float(feat["min"]), float(feat["max"])
            bk = "int" if feat.get("dtype") == "int" else "continuous"
            bounds[key] = Bound(bk, lo, hi)
        elif kind == "log":
            bounds[key] = Bound("continuous", 0.0, float(feat["max"]))
        elif kind == "passthrough":
            bk = "int" if feat.get("dtype") == "int" else "continuous"
            bounds[key] = Bound(bk, 0.0, 1.0)
        elif kind == "onehot":
            bounds[key] = Bound("enum", categories=list(feat["categories"]))
        else:
            raise ValueError(f"未知 kind: {kind}（特征 {feat['name']}）")
    return bounds


def _norm_val(v: Any) -> Any:
    """数值精简：np 标量转 Python 原生、float 保留 1 位小数（供序列化 key，对齐 llm_teacher._rounded）。"""
    if isinstance(v, (bool, np.bool_)):
        return v
    if isinstance(v, (int, np.integer)):
        return int(v)
    if isinstance(v, (float, np.floating)):
        return round(float(v), 1)
    return v  # str / np.str_ 原样


def state_key(st: dict, schema_keys: list[str]) -> str:
    """稳定序列化状态：按 schema_keys 顺序取值、规范化后 '|' 连接。

    同一状态两次序列化结果一致，作规则文件/投票进度的共享 key（跨平台、跨批一致）。
    ``schema_keys = list(derive_state_bounds(schema))``（与采样 all_keys 一致，含 *_max 常量）。
    """
    return "|".join(str(_norm_val(st[k])) for k in schema_keys)


def _random_extreme(bound: Bound, rng: np.random.Generator) -> Any:
    """训练集取值：连续/int 随机取 {lo, hi} 极值，枚举随机取一个 category。"""
    if bound.kind == "enum":
        return rng.choice(bound.categories)
    if bound.kind == "fixed":
        return bound.value
    v = rng.choice([bound.lo, bound.hi])
    return int(v) if bound.kind == "int" else float(v)


def _random_real(bound: Bound, rng: np.random.Generator) -> Any:
    """测试集取值：连续 uniform 小数、int 随机整数、枚举随机。"""
    if bound.kind == "enum":
        return rng.choice(bound.categories)
    if bound.kind == "fixed":
        return bound.value
    if bound.kind == "int":
        return int(rng.integers(bound.lo, bound.hi + 1))
    return float(rng.uniform(bound.lo, bound.hi))


# ---------------------------------------------------------------------------
# 数据生成
# ---------------------------------------------------------------------------
def generate_training_samples(
    schema,
    cfg: dict,
    seed: int,
    label_fn=None,
) -> tuple[list[dict[str, Any]], np.ndarray]:
    """生成训练集：纯极值随机组合（连续特征只取 {min,max}、枚举取全值随机）。

    每条样本对每个连续特征随机取一个极值（min 或 max），枚举特征随机取全值中
    一个，组合成一条不同状态；共 ``n_train_total`` 条。**模型看不到中间态**
    （如 hungry=50），学不到连续决策斜坡（hungry 降 → eat 概率升）——这是当前
    方案的已知局限。

    标签默认返回主类 ``idle``；传入 ``label_fn(batch_states)->list[str]`` 可
    替换打标签逻辑（如 LLM 教师，批量一次调用）。

    返回 ``(states, labels_int64)``。
    """
    rng = np.random.default_rng(seed)
    bounds = derive_state_bounds(schema)
    n_total = int(cfg["data"].get("n_train_total", 10000))
    all_keys = list(bounds)

    states: list[dict[str, Any]] = []
    for _ in range(n_total):
        st = {key: _random_extreme(bounds[key], rng) for key in all_keys}
        states.append(st)

    if label_fn is None:
        label_fn = lambda batch: [DEFAULT_ACTION] * len(batch)  # noqa: E731
    label_idx = np.array([ACTION_INDEX[a] for a in label_fn(states)], dtype=np.int64)
    return states, label_idx


def generate_test_samples(
    n: int,
    schema,
    cfg: dict,
    seed: int,
    label_fn=None,
) -> tuple[list[dict[str, Any]], np.ndarray]:
    """生成现实分布测试集：连续 uniform 小数 / int 随机 / 枚举随机。

    标签默认返回主类 ``idle``；传入 ``label_fn(batch_states)->list[str]`` 可
    替换打标签逻辑（如 LLM 教师，批量一次调用）。

    返回 ``(states, labels_int64)``。
    """
    rng = np.random.default_rng(seed)
    bounds = derive_state_bounds(schema)

    states: list[dict[str, Any]] = []
    for _ in range(n):
        st = {key: _random_real(bounds[key], rng) for key in bounds}
        states.append(st)

    if label_fn is None:
        label_fn = lambda batch: [DEFAULT_ACTION] * len(batch)  # noqa: E731
    label_idx = np.array([ACTION_INDEX[a] for a in label_fn(states)], dtype=np.int64)
    return states, label_idx
