"""规则引擎：读 config/decision_rules.yaml，生成训练/测试数据并打标签。

职责：
- ``RuleSet``：加载并校验规则表；``evaluate(state)`` 自上而下匹配返回行为。
- ``derive_state_bounds``：由 feature_schema + sampling_overrides 推导每个原始 key 的采样边界。
- ``generate_training_samples``：边界覆盖训练集（连续特征取 min/max、枚举取全值，
  非条件特征随机取极值），标签 = RuleSet.evaluate（保证训练集与规则函数自洽）。
- ``generate_test_samples``：现实分布测试集（连续随机小数、枚举随机），同一规则表打标签。

所有随机均用 ``np.random.default_rng(seed)``，同 seed 结果完全一致。
"""
from __future__ import annotations

import itertools
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import numpy as np
import yaml

from .actions import ACTIONS, ACTION_INDEX
from .features import FeatureSchema

PROJECT_ROOT = Path(__file__).resolve().parent.parent


class RuleSetError(ValueError):
    """规则表不合法时抛出。"""


def _value_eq(a, b) -> bool:
    """类型不敏感相等：数值与数值按数值比（容忍 0 vs 0.0），否则按字符串比。"""
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return float(a) == float(b)
    return str(a) == str(b)


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


def derive_state_bounds(schema: FeatureSchema, overrides: dict) -> dict[str, Bound]:
    """由 feature_schema + sampling_overrides 推导每个原始 key 的采样边界。

    ratio → [0, max_key]（max_key 本身为 fixed）；minmax → [min,max]；
    log → [0,max]；passthrough → [0,1]；onehot → categories。
    """
    int_keys = set(overrides.get("int_keys", []))
    ratio_max = overrides.get("ratio_max", {})
    bounds: dict[str, Bound] = {}
    for feat in schema.features:
        kind = feat["kind"]
        key = feat["key"]
        if kind == "ratio":
            max_key = feat["max_key"]
            maxv = float(ratio_max.get(max_key, 100.0))
            bounds[key] = Bound("continuous", 0.0, maxv)
            bounds[max_key] = Bound("fixed", value=maxv)
        elif kind == "minmax":
            lo, hi = float(feat["min"]), float(feat["max"])
            bounds[key] = Bound("int" if key in int_keys else "continuous", lo, hi)
        elif kind == "log":
            bounds[key] = Bound("continuous", 0.0, float(feat["max"]))
        elif kind == "passthrough":
            bounds[key] = Bound("int" if key in int_keys else "continuous", 0.0, 1.0)
        elif kind == "onehot":
            bounds[key] = Bound("enum", categories=list(feat["categories"]))
        else:
            raise RuleSetError(f"未知 kind: {kind}（特征 {feat['name']}）")
    return bounds


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
# 规则条件
# ---------------------------------------------------------------------------
@dataclass
class RuleCondition:
    key: str
    ops: list[tuple[str, Any]] = field(default_factory=list)  # ("le"|"ge"|"eq"|"in"|"ne", 值)

    @classmethod
    def parse(cls, key: str, spec: dict) -> "RuleCondition":
        ops: list[tuple[str, Any]] = []
        if "max" in spec:
            ops.append(("le", spec["max"]))
        if "min" in spec:
            ops.append(("ge", spec["min"]))
        if "eq" in spec:
            ops.append(("eq", spec["eq"]))
        if "in" in spec:
            ops.append(("in", spec["in"]))
        if "neq" in spec:
            ops.append(("ne", spec["neq"]))
        if not ops:
            raise RuleSetError(f"规则条件 {key} 没有可识别操作符（min/max/eq/in/neq）")
        return cls(key, ops)

    def matches(self, value: Any) -> bool:
        for op, v in self.ops:
            if op == "le" and not (value <= v):
                return False
            if op == "ge" and not (value >= v):
                return False
            if op == "eq" and not _value_eq(value, v):
                return False
            if op == "in" and not any(_value_eq(value, x) for x in v):
                return False
            if op == "ne" and _value_eq(value, v):
                return False
        return True

    def boundary_values(self, bound: Bound) -> list:
        """训练集：该条件特征应取的边界值（与全局下界/上界/取值去重）。"""
        vals: list = []
        for op, v in self.ops:
            if op == "le":
                vals += [bound.lo, v]
            elif op == "ge":
                vals += [v, bound.hi]
            elif op == "eq":
                vals += [v]
            elif op == "in":
                vals += list(v)
            elif op == "ne":
                pass  # 非等条件按无信息处理（取随机极值）
        # 按值去重（容忍 int/float/str 混用，0 与 0.0 视为同一值）
        seen: set[str] = set()
        out: list = []
        for v in vals:
            rep = f"{float(v):.10g}" if isinstance(v, (int, float)) else repr(v)
            if rep not in seen:
                seen.add(rep)
                out.append(v)
        return out

    def allowed_ops(self, bound: Bound) -> tuple[str, ...]:
        return ("eq", "in", "ne") if bound.kind == "enum" else ("le", "ge", "eq")


# ---------------------------------------------------------------------------
# 规则与规则集
# ---------------------------------------------------------------------------
@dataclass
class Rule:
    name: str
    conditions: dict[str, RuleCondition]
    action: str

    def matches(self, state: dict[str, Any]) -> bool:
        return all(cond.matches(state[cond.key]) for cond in self.conditions.values())


class RuleSet:
    """已加载并校验的规则表，可反复 evaluate / 用于生成数据。"""

    def __init__(self, version: int, rules: list[Rule], default_action: str,
                 sampling_overrides: dict):
        self.version = version
        self.rules = rules
        self.default_action = default_action
        self.sampling_overrides = sampling_overrides

    @classmethod
    def load(cls, path: str | Path = "config/decision_rules.yaml") -> "RuleSet":
        raw = yaml.safe_load((PROJECT_ROOT / path).read_text(encoding="utf-8"))
        if not isinstance(raw.get("rules"), list) or not raw["rules"]:
            raise RuleSetError(f"{path} 缺少非空 rules 列表")

        rules: list[Rule] = []
        for item in raw["rules"]:
            action = item["action"]
            if action not in ACTIONS:
                raise RuleSetError(f"规则 {item.get('name', '?')} 的 action '{action}' 不在 ACTIONS: {ACTIONS}")
            when = item.get("when", {})
            conditions = {
                key: RuleCondition.parse(key, spec)
                for key, spec in when.items()
            }
            rules.append(Rule(item.get("name", action), conditions, action))

        # 校验：恰一条 when:{} 兜底
        empty = [r for r in rules if not r.conditions]
        if len(empty) != 1:
            raise RuleSetError(f"规则表必须有且仅有一条 when:{{}} 兜底规则，当前 {len(empty)} 条")
        default_action = empty[0].action

        overrides = raw.get("sampling_overrides", {})
        return cls(raw.get("version", 1), rules, default_action, overrides)

    def condition_keys(self) -> set[str]:
        return {key for rule in self.rules for key in rule.conditions}

    def validate_bounds(self, bounds: dict[str, Bound]) -> None:
        """校验条件 key 存在且操作符与边界类型匹配。"""
        for rule in self.rules:
            for key, cond in rule.conditions.items():
                if key not in bounds:
                    raise RuleSetError(f"规则 '{rule.name}' 条件 key '{key}' 不在 feature_schema 推导的边界中")
                allowed = cond.allowed_ops(bounds[key])
                for op, _ in cond.ops:
                    if op not in allowed:
                        raise RuleSetError(
                            f"规则 '{rule.name}' 条件 '{key}' 用 {op} 不合法（边界类型 "
                            f"{bounds[key].kind} 仅允许 {allowed}）")

    def evaluate(self, state: dict[str, Any]) -> str:
        """自上而下第一条命中的规则 action；全部不命中 → 兜底。"""
        for rule in self.rules:
            if rule.matches(state):
                return rule.action
        return self.default_action

    def label_index(self, state: dict[str, Any]) -> int:
        return ACTION_INDEX[self.evaluate(state)]


# ---------------------------------------------------------------------------
# 数据生成
# ---------------------------------------------------------------------------
def generate_training_samples(
    ruleset: RuleSet,
    schema: FeatureSchema,
    cfg: dict,
    seed: int,
    label_fn=None,
) -> tuple[list[dict[str, Any]], np.ndarray, dict[str, int]]:
    """生成训练集（boundary 边界覆盖采样）。

    对每条非兜底规则：条件特征取边界值组合（小笛卡尔积）× repeats，非条件特征
    随机取极值；兜底规则生成 n_default_train 个随机极值样本。连续特征只取
    min/max + 少数规则阈值，**模型看不到中间态**（如 hungry=50），学不到连续决策
    斜坡（hungry 降 → eat 概率升）——这是当前方案的已知局限。

    标签默认用规则表批量求值；传入 ``label_fn(batch_states)->list[str]`` 可替换
    打标签逻辑（如 LLM 教师，批量一次调用）。per_rule_hits 始终按规则表 evaluate
    统计，保留「规则表覆盖度」语义。

    返回 ``(states, labels_int64, per_rule_hits)``，其中 per_rule_hits 统计每条规则
    实际贡献自身 action 的样本数（为 0 说明该规则被更早规则遮蔽，即「不可达」）。
    """
    rng = np.random.default_rng(seed)
    overrides = ruleset.sampling_overrides
    bounds = derive_state_bounds(schema, overrides)
    ruleset.validate_bounds(bounds)

    if label_fn is None:
        label_fn = lambda batch: [ruleset.evaluate(st) for st in batch]  # noqa: E731

    n_per = cfg["data"].get("n_train_per_rule", 64)
    repeats = cfg["data"].get("repeats", 4)
    n_default = cfg["data"].get("n_default_train", 128)

    all_keys = list(bounds)

    def gen_state(cond_values: dict[str, Any]) -> dict[str, Any]:
        st: dict[str, Any] = {}
        for key in all_keys:
            st[key] = cond_values[key] if key in cond_values else _random_extreme(bounds[key], rng)
        return st

    states: list[dict[str, Any]] = []
    per_rule_hits: dict[str, int] = {}

    for rule in ruleset.rules:
        if not rule.conditions:  # 兜底
            hits = 0
            for _ in range(n_default):
                st = gen_state({})
                states.append(st)
                if ruleset.evaluate(st) == rule.action:
                    hits += 1
            per_rule_hits[rule.name] = hits
            continue

        cond_keys = list(rule.conditions)
        combo_lists = [rule.conditions[k].boundary_values(bounds[k]) for k in cond_keys]
        seen: set[tuple[str, ...]] = set()
        hits = 0
        generated = 0
        for combo in itertools.product(*combo_lists):
            if generated >= n_per:
                break
            sig = tuple(str(v) for v in combo)
            if sig in seen:
                continue
            seen.add(sig)
            cond_values = dict(zip(cond_keys, combo))
            for _ in range(repeats):
                if generated >= n_per:
                    break
                st = gen_state(cond_values)
                states.append(st)
                generated += 1
                if ruleset.evaluate(st) == rule.action:
                    hits += 1
        per_rule_hits[rule.name] = hits

    label_idx = np.array([ACTION_INDEX[a] for a in label_fn(states)], dtype=np.int64)
    return states, label_idx, per_rule_hits


def generate_test_samples(
    n: int,
    ruleset: RuleSet,
    schema: FeatureSchema,
    cfg: dict,
    seed: int,
    label_fn=None,
) -> tuple[list[dict[str, Any]], np.ndarray]:
    """生成现实分布测试集：连续 uniform 小数 / int 随机 / 枚举随机。

    标签默认用规则表批量求值；传入 ``label_fn(batch_states)->list[str]`` 可替换
    打标签逻辑（如 LLM 教师，批量一次调用）。

    返回 ``(states, labels_int64)``。
    """
    rng = np.random.default_rng(seed)
    overrides = ruleset.sampling_overrides
    bounds = derive_state_bounds(schema, overrides)
    ruleset.validate_bounds(bounds)

    if label_fn is None:
        label_fn = lambda batch: [ruleset.evaluate(st) for st in batch]  # noqa: E731

    states: list[dict[str, Any]] = []
    for _ in range(n):
        st = {key: _random_real(bounds[key], rng) for key in bounds}
        states.append(st)

    label_idx = np.array([ACTION_INDEX[a] for a in label_fn(states)], dtype=np.int64)
    return states, label_idx
