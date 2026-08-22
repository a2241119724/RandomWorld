"""LLM 教师：用 DeepSeek 按「现实生活优先级」的常识对状态打标签。

训练/测试标签全部由本模块打（规则表已删除，`src/rules.py` 只负责采样）：
- ``build_system_prompt`` 把 14 种行为定义 + 每个状态字段的中文说明与数值范围
  组装成系统 prompt（范围由 ``derive_state_bounds`` 自动推导，新增特征无需改这里）。
- ``LLMTeacher.label(states)`` 按 ``batch_size`` 分批调用 DeepSeek（OpenAI 兼容端点），
  要求返回 ``{"labels": [...]}`` JSON；`temperature=0` 保证确定性。
- 解析失败/超时重试 ``max_retries`` 次后，对未通过校验的条目回退到主类 ``idle``
  （无规则表兜底，主类作最后防线，确保数据永不残缺）。

缓存逻辑由调用方（data/generate_data.py）处理：``data/cache/llm_labels_{split}.npy``。
key 从环境变量 ``DEEPSEEK_API_KEY`` 读取，不落库。
"""
from __future__ import annotations

import json
import os
import re
from typing import Any

from .actions import ACTIONS, ACTION_LABELS_ZH
from .rules import derive_state_bounds

# ---------------------------------------------------------------------------
# 状态字段中文说明（key → 语义；数值范围由 derive_state_bounds 自动补充）
# ---------------------------------------------------------------------------
KEY_ZH: dict[str, str] = {
    # 生存（端点 + 正常范围锚点：LLM 对「越小越饿」这类相对表述不可靠，
    # 会把 hungry=50 也判成饿。给出明确阈值锚点，常识才有意义）
    "hungry": "饥饿度。0~30=真正饿了必须尽快进食；30~70=正常，不影响干活；70~100=吃饱喝足完全不需要进食",
    "tired": "疲劳度。0~40=精力充沛可正常干活；40~70=有些累但能坚持；70~100=累到极限必须休息",
    "spirit": "精气神。0~40=萎靡需要恢复；40~70=正常；70~100=精神饱满",
    # 生命 / 成长
    "hp": "生命值。0=濒死，100=满血；数值越低越危险",
    "mp": "能量/魔力。0=耗尽，100=满",
    "level": "等级",
    "exp": "经验值",
    "atn": "攻击力",
    "int_": "智力",
    "def_": "防御力",
    "res": "抗性",
    "crt": "暴击率",
    "csd": "暴击伤害",
    "spd": "速度",
    "hit": "命中率",
    # 人格
    "mood": "心情。0=极差，100=极好；数值越低越差",
    "ambition": "事业心（越高越想干大事）",
    "diligence": "勤奋（越高越爱干活）",
    "sociality": "社交欲（越高越爱社交）",
    "greed": "贪婪（越高越爱攒物资）",
    "laziness": "懒惰（越高越不爱动）",
    # 经济
    "gold": "金币（可买食物/建材）",
    # 阶段 / 目标
    "bed_available": "是否有床可用（0=没有床，1=有床）",
    "life_stage": "生存阶段（bootstrap=起步 / settled=安顿 / established=发展）",
    "home_build_stage": "建家阶段（0=未建 / 1=在建 / 2=已建）",
    "current_goal": "当前目标（earn_money=赚钱 / build_structure=建造 / stock_food=囤粮 / craft_equipment=造装备）",
    # 社交
    "favorability": "好感度（越高他人越愿帮忙）",
    # 全局上下文
    "weather_temperature": "气温（摄氏度，负值=严寒）",
    "task_pressure": "任务压力（越大越紧张）",
    "time_of_day": "一天中的时刻（0~24，深夜/清晨容易困）",
    # 局部视野
    "nearby_food": "附近可采/可食用的食物数量。0=附近完全没有食物（无法就地吃饭）",
    "nearby_resource": "附近可采集的资源数量。0=附近没有资源",
    "nearby_building": "附近建筑数量",
    "nearby_worker": "附近工人数量",
}


# ---------------------------------------------------------------------------
# Prompt 构造
# ---------------------------------------------------------------------------
def _range_text(key: str, bound) -> str:
    """单个字段的一行说明；*_max 常量与未知 key 返回 None（跳过）。"""
    zh = KEY_ZH.get(key)
    if zh is None:
        return None  # hungry_max 等上限常量，不描述
    if bound.kind == "enum":
        choices = " / ".join(str(c) for c in bound.categories)
        return f"- {key} {zh}: 取值 {choices}"
    if bound.kind == "int":
        return f"- {key} {zh}: 整数，范围 {bound.lo}~{bound.hi}"
    if bound.kind == "fixed":
        return f"- {key} {zh}: 固定值 {bound.value}"
    return f"- {key} {zh}: 范围 {bound.lo}~{bound.hi}（可含小数）"


def build_system_prompt(schema) -> str:
    """构造系统 prompt：行为定义 + 状态字段说明（范围由 feature_schema 自动推导）。"""
    bounds = derive_state_bounds(schema)
    state_lines = "\n".join(
        line for key, bound in bounds.items()
        if (line := _range_text(key, bound)) is not None
    )
    action_lines = "\n".join(
        f"- {name:<12s} {zh}" for name, zh in ACTION_LABELS_ZH.items()
    )
    return f"""你是村庄生存模拟游戏中工人（worker）的智能行为决策系统。
你会看到一名工人的完整状态，请根据【现实生活优先级】判断他此刻最应该做的一件事，
从可选行为中选出唯一一个。

【铁律：行动必须现实可行】
- eat（吃饭）只在附近有食物（nearby_food>0）或能从仓库取到食物时才可行。
  饿了但 nearby_food=0 时，必须去 self_gather 采集（或 withdraw 取仓库），绝不能选 eat。
- sleep（睡床）需要 bed_available=1；没有床但必须休息时用 ground_sleep（地面睡）。
- 其余行动同样要匹配状态里的现实条件（如没有资源可拾取就选不了 pickup）。

【优先级原则】（由高到低）
1. 目标驱动：有明确当前目标（current_goal）时，只要饥饿/疲劳处于正常范围（hungry 30~70、tired 40~70），就优先推进目标——
   赚钱→采集/拾取资源；建造→自我建造或取仓库建材；囤粮→采集食物；造装备→采集资源。
2. 生理极限优先于一切：hungry<30（真正饿了）必须进食（有食物吃，没有去采集/取仓库）；tired>70（累到极限）必须休息（有床睡床，没床地面睡）。
3. 状态安全：hp/spirit<40 时先恢复，不要在濒死或虚脱状态下干活。
4. 天气与环境：严寒（<0°C）尽量在室内避寒；酷热出门要确保有食物补给。
5. 无目标且状态正常时：有余力再经营与社交（发布/接受悬赏、存入/取出仓库、拾取/搬运资源、种植）。
6. 没有任何压力也无目标时，才空闲或漫游。

【行为多样性】
不同的工人状态应产生多样的行为。绝大多数工人**不会**永远在吃饭睡觉：
hungry>70（吃饱）+ tired<40（精力足）时，工人不会选择 eat/sleep，而会去采集、建造、经营或社交；
只有真正饿了/累了（见上面阈值）才选择 eat / sleep / ground_sleep。

【可选行为】（只能选一个，输出它的英文名）
{action_lines}

【状态字段说明】（字段名后带 *_max 的为对应属性的上限常量，可忽略）
{state_lines}"""


def _rounded(v: Any) -> Any:
    """数值精简为 JSON 可序列化：numpy 标量转 Python 原生类型，float 保留 1 位小数。

    采样函数（rules.py）可能产出 np.int64 / np.str_ / np.float64，json.dumps
    无法处理 np.int64（非 int 子类），会抛 ``int64 is not JSON serializable``。
    """
    import numpy as np

    if isinstance(v, (bool, np.bool_)):
        return v
    if isinstance(v, (int, np.integer)):
        return int(v)
    if isinstance(v, (float, np.floating)):
        return round(float(v), 1)
    return v  # str / np.str_ 原样（均可 JSON 序列化）


# 字符串枚举 → 数字映射（与 config/feature_schema.yaml 的 categories 顺序一致；改 schema 需同步）
ENUM_TO_INT: dict[str, dict[str, int]] = {
    "life_stage": {"bootstrap": 0, "settled": 1, "established": 2},
    "current_goal": {"earn_money": 0, "build_structure": 1, "stock_food": 2, "craft_equipment": 3},
}


def build_user_prompt(batch: list[dict[str, Any]]) -> str:
    """构造打标签 user prompt：状态以「列名表头 + 每行值数组」表格形式给出。

    状态字段 key 固定，不再每行重复——列名只出现一次，行内只含值数组（字符串枚举
    映射为数字，映射标注在列名里），prompt 长度约减半 → 同 token 下每批可放更多条
    （doubao 大 prompt 整批回显的诱因就是 prompt 过长）。
    """
    keys = list(batch[0].keys())
    # 列名：枚举字段标注数字化映射（0=bootstrap/1=settled/...），其余保持原名
    header_parts = []
    for k in keys:
        m = ENUM_TO_INT.get(k)
        if m:
            mapping = " / ".join(f"{v}={i}" for v, i in m.items())
            header_parts.append(f"{k}({mapping})")
        else:
            header_parts.append(k)
    header = ", ".join(header_parts)
    # 每行值数组：枚举映射为数字，其余 _rounded 精简为原生类型
    rows = []
    for i, st in enumerate(batch, 1):
        vals = []
        for k in keys:
            m = ENUM_TO_INT.get(k)
            v = st[k]
            if m and str(v) in m:
                vals.append(str(m[str(v)]))
            else:
                vals.append(str(_rounded(v)))
        rows.append(f"工人{i}: [{', '.join(vals)}]")
    return (
        f"请判断以下 {len(batch)} 名工人各自此刻最应该做的一件事。\n"
        "只输出一个 JSON 对象（不要任何其他文字），格式：\n"
        '{"labels": ["行为英文名", "行为英文名", ...]}\n'
        f"labels 的长度必须正好等于 {len(batch)}，一个都不能少；禁止省略、分批输出或向我提问。\n"
        "输出必须一次完整给出全部结果，除那个 JSON 对象外禁止任何解释文字。\n"
        "工人状态以表格给出：第一行为列名，后续每行是一名工人，行内值按列名顺序一一对应。\n"
        f"列名: {header}\n" + "\n".join(rows)
    )


def _parse_labels(text: str, n: int) -> list[str | None] | None:
    """解析 LLM 返回文本。成功返回长度 n 的列表（非法项为 None）；
    无法解析或长度不符返回 None（触发整批重试）。"""
    cleaned = text.strip()
    if cleaned.startswith("```"):
        cleaned = re.sub(r"^```[a-zA-Z]*\n?", "", cleaned)
        cleaned = re.sub(r"\n?```$", "", cleaned).strip()
    data = None
    try:
        data = json.loads(cleaned)
    except json.JSONDecodeError:
        m = re.search(r"\{.*\}", cleaned, re.S)
        if m:
            try:
                data = json.loads(m.group(0))
            except json.JSONDecodeError:
                data = None
    if not isinstance(data, dict):
        return None
    raw = data.get("labels")
    # 容忍 ±2 数量偏差（如 wenxin 实测多输出 1 个）：多出截断为前 n 个，不足用 None 补齐（None → 兜底 idle）
    if not isinstance(raw, list) or abs(len(raw) - n) > 2:
        return None
    if len(raw) > n:
        raw = raw[:n]
    raw = raw + [None] * (n - len(raw))
    return [v if isinstance(v, str) and v in ACTIONS else None for v in raw]


# ---------------------------------------------------------------------------
# LLM 教师
# ---------------------------------------------------------------------------
class LLMTeacher:
    """DeepSeek 打标签器（OpenAI 兼容端点）。"""

    def __init__(self, cfg, schema):
        llm_cfg = cfg["llm"] if isinstance(cfg, dict) else cfg.get("llm", {})
        self.source = "api"  # 标签来源标记：纳入缓存 key，防不同教师缓存串扰
        self.model = llm_cfg.get("model", "deepseek-chat")
        # 换模型兜底：当前模型重试耗尽后依次尝试这些模型（用户要求失败不兜底 idle）
        self.model_fallbacks = list(llm_cfg.get("model_fallbacks", []))
        self.base_url = llm_cfg.get("base_url", "https://api.deepseek.com")
        self.temperature = llm_cfg.get("temperature", 0.0)
        self.batch_size = llm_cfg.get("batch_size", 32)
        self.timeout = llm_cfg.get("timeout", 60)
        self.max_retries = llm_cfg.get("max_retries", 3)

        api_key = os.environ.get("DEEPSEEK_API_KEY")
        if not api_key:
            raise RuntimeError(
                "缺少 DEEPSEEK_API_KEY 环境变量。"
                "请先设置：export DEEPSEEK_API_KEY=sk-...（Windows: $env:DEEPSEEK_API_KEY=\"sk-...\"）"
            )
        from openai import OpenAI  # 延迟导入：降低无 key 时的启动成本

        self.client = OpenAI(api_key=api_key, base_url=self.base_url, timeout=self.timeout)
        self._system_prompt = build_system_prompt(schema)

    # ---- 对外入口 ----
    def label(self, states: list[dict[str, Any]]) -> list[str]:
        """给一批状态打标签（分批调 API，带进度打印）。"""
        out: list[str] = []
        n = len(states)
        for i in range(0, n, self.batch_size):
            batch = states[i:i + self.batch_size]
            out += self._label_batch(batch)
            print(f"[llm_teacher] 已打标签 {min(i + self.batch_size, n)}/{n}")
        return out

    # ---- 内部 ----
    def _label_batch(self, batch: list[dict[str, Any]]) -> list[str]:
        """打一个批次：当前模型重试耗尽后换 fallback 模型重试；全部失败抛异常（不兜底 idle）。"""
        models = [self.model, *self.model_fallbacks]
        last_err: Exception | None = None
        for model in models:
            for attempt in range(self.max_retries + 1):
                try:
                    text = self._call_api(batch, model)
                    parsed = _parse_labels(text, len(batch))
                    if parsed is not None and all(a is not None for a in parsed):
                        # 批内个别非法项也算失败（用户要求不兜底）：只有全合法才接受
                        return parsed
                    last_err = ValueError(f"输出无法解析/长度不符: {text[:120]!r}")
                except Exception as e:  # 网络/超时/限流等
                    last_err = e
                if attempt < self.max_retries:
                    print(f"[llm_teacher] 批次重试 {attempt + 1}/{self.max_retries} ({model}): {last_err}")
            if model != models[-1]:
                print(f"[llm_teacher] 模型 {model} 重试耗尽，换模型 {models[models.index(model) + 1]}")
        raise RuntimeError(f"批次 {len(batch)} 条所有模型重试失败，不兜底默认 action: {last_err}")

    def _call_api(self, batch: list[dict[str, Any]], model: str | None = None) -> str:
        resp = self.client.chat.completions.create(
            model=model or self.model,
            messages=[
                {"role": "system", "content": self._system_prompt},
                {"role": "user", "content": build_user_prompt(batch)},
            ],
            temperature=self.temperature,
            response_format={"type": "json_object"},
        )
        return resp.choices[0].message.content or ""

    # ------------------------------------------------------------------
    # 规则生成（--gen-rules）：DeepSeek API 也是规则生成模型之一
    # ------------------------------------------------------------------
    def generate_rule_set(self, schema, rule_dir) -> dict | None:
        """让 DeepSeek API 写一份结构化规则文件内容；失败返回 None。

        规则生成用深度思考：reasoner 模型优先（思维链推理规则覆盖/边界），
        chat 兜底。返回 ``{version, platform, schema_keys, fields, rules}``
        （platform = "deepseek_api"，与网页平台 deepseek.json 区分）。
        """
        from .web_labeler import (build_rule_fields, build_rule_prompt,
                                  _parse_rules, _validate_rules)  # 惰性导入防循环
        schema_keys = list(derive_state_bounds(schema))
        prompt = build_rule_prompt(schema)
        # 深度思考优先：reasoner 在前，chat 兜底
        models = ([m for m in [self.model, *self.model_fallbacks] if "reasoner" in m]
                  + [m for m in [self.model, *self.model_fallbacks] if "reasoner" not in m])
        last_err: Exception | None = None
        for model in models:
            for attempt in range(self.max_retries + 1):
                try:
                    text = self._call_rule_api(prompt, model)
                    parsed = _parse_rules(text)
                    valid = _validate_rules(parsed, schema_keys) if parsed else []
                    if valid:
                        print(f"[gen-rules:deepseek_api] 模型 {model} 写规则 "
                              f"{len(parsed)} 条，校验通过 {len(valid)} 条")
                        return {
                            "version": 2,
                            "platform": "deepseek_api",
                            "schema_keys": schema_keys,
                            "fields": build_rule_fields(schema),
                            "rules": valid,
                            "deep_think": True,  # deepseek-reasoner = 深度思考，投票权重翻倍
                        }
                    last_err = ValueError(f"规则无法解析/校验: {text[:120]!r}")
                except Exception as e:
                    last_err = e
                if attempt < self.max_retries:
                    print(f"[gen-rules:deepseek_api] 重试 {attempt + 1}/{self.max_retries} "
                          f"({model}): {last_err}")
            if model != models[-1]:
                print(f"[gen-rules:deepseek_api] 模型 {model} 重试耗尽，换模型 "
                      f"{models[models.index(model) + 1]}")
        print(f"[gen-rules:deepseek_api] 全部模型失败，跳过: {last_err}")
        return None

    def _call_rule_api(self, prompt: str, model: str | None = None) -> str:
        """调 DeepSeek 生成规则。reasoner 模型不支持 temperature/response_format → 区分传参。"""
        model = model or self.model
        kwargs: dict[str, Any] = dict(
            model=model,
            messages=[{"role": "user", "content": prompt}],
        )
        if "reasoner" not in model:
            kwargs["temperature"] = self.temperature
            kwargs["response_format"] = {"type": "json_object"}
        resp = self.client.chat.completions.create(**kwargs)
        return resp.choices[0].message.content or ""
