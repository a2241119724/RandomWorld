"""规则文件打标器：加载所有模型的规则文件，对训练集逐状态推导 + 多数投票。

规则文件由 ``python -m src.web_labeler --gen-rules`` 生成：每个可用模型（网页版平台）
根据字段描述与取值范围写出一组结构化规则（``{"when": {字段: 约束}, "action": 行为}``）。
打标时对每个状态用每模型的规则推导一个标签（无命中=该模型此状态无票），
逐状态多数投票取标签最多者（平局按 ``VOTE_PRIORITY`` 取高者）。

``llm.provider: rules`` 时训练集用本打标器；全部模型都推导不到的状态直接抛错
（数据不残缺、不静默兜底 idle——对齐历史「不兜底」教训）。

与 ``LLMTeacher`` / ``WebTeacher`` 同接口（``label(states) -> list[str]``、
``_system_prompt``、``source``），generate_data.py 用「教师来源」纳入缓存 key。
"""
from __future__ import annotations

import json
import operator
from collections import Counter
from pathlib import Path
from typing import Any

from .llm_teacher import build_system_prompt
from .rules import _norm_val

PROJECT_ROOT = Path(__file__).resolve().parent.parent

# 多数投票平局优先级（干活/生产优先于休息/闲逛；index 小 = 更优先）
VOTE_PRIORITY = [
    "self_gather", "self_build", "self_plant", "pickup",
    "store", "withdraw", "post_bounty", "accept_bounty",
    "self_carry", "eat", "sleep", "ground_sleep", "wander", "idle",
]

# 约束运算符（宽松解析：数字键 + 符号变体）
_CMP_OPS: dict[str, Any] = {
    "lt": operator.lt, "lte": operator.le, "gt": operator.gt, "gte": operator.ge,
}
_SYM_OPS: dict[str, str] = {
    "<": "lt", "<=": "lte", ">": "gt", ">=": "gte", "==": "eq", "=": "eq",
}


def _match(cond: dict, value: Any) -> bool:
    """单字段约束匹配。``cond`` = {运算符: 目标}，多个约束需全部满足（由调用方 AND 组合）。

    支持运算符：``lt/lte/gt/gte``（数值比较）、``btw``（闭区间 [lo, hi]）、
    ``eq``（相等）、``in``（包含于列表）；符号变体 ``< <= > >= == =`` 自动归一。
    值先经 ``_norm_val`` 归一（np 标量 → 原生、float 保留 1 位，与序列化 key 一致）。
    """
    v = _norm_val(value)
    for op, target in cond.items():
        op = _SYM_OPS.get(op, op) if isinstance(op, str) else op
        if op == "eq":
            if v != target:
                return False
        elif op == "in":
            if not isinstance(target, (list, tuple)) or v not in target:
                return False
        elif op == "btw":
            if not (isinstance(target, (list, tuple)) and len(target) == 2):
                return False
            lo, hi = target
            if not (lo <= v <= hi):
                return False
        elif op in _CMP_OPS:
            fn = _CMP_OPS[op]
            if not (isinstance(v, (int, float)) and isinstance(target, (int, float))):
                return False
            if not fn(v, target):
                return False
        else:  # 未知运算符：该约束永不满足（模型输出变体时宁可丢票不误判）
            return False
    return True


def derive(rule_set: dict, st: dict[str, Any]) -> str | None:
    """用一组规则对一个状态推导标签：首条 when 全部满足的规则返回其 action；无命中返回 None。"""
    for r in rule_set.get("rules", []):
        when = r.get("when")
        if not isinstance(when, dict):
            continue
        if all(_match(cond, st.get(f)) for f, cond in when.items()):
            return r.get("action")
    return None


class RuleTeacher:
    """规则文件打标器（离线：加载全部模型规则 → 推导 + 多数投票）。"""

    source = "rules"  # 标签来源：纳入 train 缓存 key，防与 web/api 教师缓存串扰

    def __init__(self, cfg, schema, rule_dir):
        self.rule_dir = Path(rule_dir)
        if not self.rule_dir.is_absolute():
            self.rule_dir = PROJECT_ROOT / self.rule_dir
        if not self.rule_dir.exists():
            raise FileNotFoundError(
                f"[rules] 规则目录不存在: {self.rule_dir}。"
                f"请先运行 python -m src.web_labeler --gen-rules 生成各模型规则文件")
        self._rule_sets: list[dict] = []
        for f in sorted(self.rule_dir.glob("*.json")):
            try:
                data = json.loads(f.read_text(encoding="utf-8"))
            except Exception as e:
                print(f"[rules] 跳过无法解析的规则文件 {f.name}: {e}")
                continue
            rules = data.get("rules") if isinstance(data, dict) else None
            if isinstance(rules, list) and rules:
                self._rule_sets.append({"platform": f.stem, "rules": rules})
            else:
                print(f"[rules] 跳过无规则的文件 {f.name}")
        if not self._rule_sets:
            raise ValueError(
                f"[rules] 规则目录 {self.rule_dir} 没有任何含合法规则的模型文件"
                f"（请先运行 --gen-rules，或检查字段/动作是否与 schema 匹配）")
        self.n_models = len(self._rule_sets)
        self.model = f"rules({self.n_models}模型)"
        self.batch_size = 0  # 推导无批次限制
        self._system_prompt = build_system_prompt(schema)  # 缓存 key 依赖（对齐现有教师）

    def label(self, states: list[dict[str, Any]]) -> list[str]:
        """逐状态用各模型规则推导 + 多数投票；全部模型都推导不到 → 抛错（不兜底）。"""
        out: list[str] = []
        for st in states:
            tally: Counter = Counter()
            for rs in self._rule_sets:
                lab = derive(rs, st)
                if lab:
                    tally[lab] += 1
            if not tally:
                raise RuntimeError(
                    f"[rules] {self.n_models} 个模型的规则均推导不到该状态（规则覆盖不全），不兜底。"
                    f"请检查 {self.rule_dir} 下规则文件，或重新运行 --gen-rules。"
                    f"状态样本 hungry={st.get('hungry')} tired={st.get('tired')} "
                    f"nearby_food={st.get('nearby_food')} current_goal={st.get('current_goal')}")
            # 多数投票，平局按 VOTE_PRIORITY 取高者（确定性）
            winner = max(tally.items(), key=lambda kv: (kv[1], -VOTE_PRIORITY.index(kv[0])))[0]
            out.append(winner)
        return out
