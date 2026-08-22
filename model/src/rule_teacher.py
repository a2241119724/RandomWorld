"""规则文件打标器：从多模型投票生成的规则文件离线打标签，不依赖浏览器/网络。

规则文件由 ``python -m src.web_labeler --vote-rules`` 生成：所有正常平台对训练集
状态重复打标，逐状态多数投票取标签最多者（平局按固定优先级），每状态投票明细
固化在 JSON 里。``llm.provider: rules`` 时训练集用本打标器，状态不在规则文件里
直接抛错（数据不残缺、不静默兜底 idle——对齐历史「不兜底」教训）。

与 ``LLMTeacher`` / ``WebTeacher`` 同接口（``label(states) -> list[str]``、
``_system_prompt``、``source``），generate_data.py 用「教师来源」纳入缓存 key。
"""
from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from .llm_teacher import build_system_prompt
from .rules import state_key

PROJECT_ROOT = Path(__file__).resolve().parent.parent


class RuleTeacher:
    """规则文件打标器（离线查表）。"""

    source = "rules"  # 标签来源：纳入 train 缓存 key，防与 web/api 教师缓存串扰

    def __init__(self, cfg, schema, rule_file):
        llm_cfg = cfg["llm"] if isinstance(cfg, dict) else cfg.get("llm", {})
        self.rule_file = Path(rule_file)
        if not self.rule_file.is_absolute():
            self.rule_file = PROJECT_ROOT / self.rule_file
        if not self.rule_file.exists():
            raise FileNotFoundError(
                f"[rules] 规则文件不存在: {self.rule_file}。"
                f"请先运行 python -m src.web_labeler --vote-rules 生成")
        data = json.loads(self.rule_file.read_text(encoding="utf-8"))
        self._schema_keys = data["schema_keys"]
        self._table: dict[str, str] = {e["key"]: e["label"] for e in data["entries"]}
        self.n = len(self._table)
        self.model = f"rules({self.rule_file.name})"
        self.batch_size = 0  # 查表无批次限制
        self._system_prompt = build_system_prompt(schema)  # 缓存 key 依赖（对齐现有教师）

    def label(self, states: list[dict[str, Any]]) -> list[str]:
        """按规则表逐状态查标；缺失状态抛错（数据不残缺、不静默兜底）。"""
        out: list[str] = []
        for st in states:
            key = state_key(st, self._schema_keys)
            if key not in self._table:
                raise RuntimeError(
                    f"[rules] 规则文件 {self.rule_file.name} 缺少该状态"
                    f"（可能换了 seed/采样/schema）。请重新运行 --vote-rules 生成匹配规则。"
                    f"key 前 80 字符: {key[:80]}")
            out.append(self._table[key])
        return out
