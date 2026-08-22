"""网页版 AI 免费打标签器（Playwright 浏览器自动化，多平台并行）。

替代付费 API 打标签：特征/输出扩展导致缓存失效时，重打标签免费（用户决策）。
支持多个免费网页平台并行（chat.deepseek.com / 文心一言 / 通义千问；已移除豆包/火山方舟/小米 MiMo）：

- **每平台独立持久化 profile**（``data/browser_profile/<平台>/``）：首次运行弹出浏览器
  手动登录一次，之后复用会话（不存密码）。DeepSeek 旧会话（根目录）自动迁移。
- **可用性判定**（``check_usable``）：登录可达 + 发极简提示能收到回复。``--probe-all``
  逐个打开全部平台等你登录，判定后 dump 每平台 DOM 供校准选择器（``web_platforms.py``）。
- **并行池**（``WebTeacherPool``）：把批次队列分发给「可用平台」并行打（线程；每线程
  各建自己的 Playwright 上下文，互不干扰），逐批落盘进度（断点续跑）。
- **自动关「深度思考/联网搜索」类开关**（实测提速一个数量级）、每批新对话防上下文串扰。

与 ``LLMTeacher`` 同接口（``label(states) -> list[str]``、``_system_prompt``、``source``），
generate_data.py 用「教师来源」纳入缓存 key，两种教师缓存互不串扰。

用法（在 model/ 目录下）：
    python -m src.web_labeler --probe-all        # 打开全部平台登录 → 判可用性 + dump DOM
    python -m src.web_labeler --probe deepseek   # 单平台探活
    python -m src.web_labeler --states 8         # 冒烟：并行打 8 条状态
    python -m src.web_labeler --train            # 打整个训练集（经 generate_data 缓存）
"""
from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
import threading
import time
from pathlib import Path
from typing import Any, Optional

from .actions import ACTIONS, ACTION_LABELS_ZH
from .config import ModelConfig
from .features import FeatureSchema
from .llm_teacher import (KEY_ZH, _parse_labels, _range_text,
                          build_system_prompt, build_user_prompt)
from .rules import derive_state_bounds, generate_training_samples
from .web_platforms import PLATFORM_DEFS, get_platform

PROJECT_ROOT = Path(__file__).resolve().parent.parent

# 冒烟/可用性探针提示词：极简，任何模型都应按字面回复一个短词
PROBE_PROMPT = "请只回复 OK 两个英文字母，不要输出任何其他内容。"


# ---------------------------------------------------------------------------
# 规则生成（--gen-rules）：让每个模型把「状态 → 行为」决策逻辑写成结构化规则
# ---------------------------------------------------------------------------
def build_rule_fields(schema) -> dict[str, dict]:
    """程序从 schema 生成字段描述（模型不需重复写字段信息，规则文件仍完整可审查）。"""
    bounds = derive_state_bounds(schema)
    fields: dict[str, dict] = {}
    for key, bound in bounds.items():
        line = _range_text(key, bound)
        if line is not None:
            fields[key] = {"desc": line}
    return fields


def build_rule_prompt(schema) -> str:
    """构造「写规则」prompt：字段描述 + 行为定义 + 规则 JSON 格式要求。"""
    fields = build_rule_fields(schema)
    state_lines = "\n".join(f"- {key} {info['desc']}" for key, info in fields.items())
    action_lines = "\n".join(f"- {name:<12s} {zh}" for name, zh in ACTION_LABELS_ZH.items())
    return f"""你是村庄生存模拟游戏中工人行为决策系统。请把「工人状态 → 此刻最该做的一件事」的决策逻辑写成一组可执行规则，规则要体现【现实生活优先级】。

【状态字段与取值范围】（字段名 + 中文含义 + 取值）
{state_lines}

【可选行为】（action 只能取其中一个英文名）
{action_lines}

【规则格式】严格只输出一个 JSON 对象，不要任何其他文字、不要 markdown 代码块：
{{"rules": [{{"when": {{"字段名": {{"运算符": 阈值}}}}, "action": "行为"}}, ...]}}

要求：
- when 内所有约束需同时满足（AND）；一条规则只约束最相关的 1~3 个字段
- 数值字段运算符：lt(<) / lte(<=) / gt(>) / gte(>=) / btw(区间 [下限, 上限])
  例：{{"hungry": {{"lte": 30}}}} 表示 hungry<=30；{{"hungry": {{"btw": [30, 70]}}}} 表示 30<=hungry<=70
- 枚举字段运算符：eq(等于) / in(在列表中)
  例：{{"current_goal": {{"eq": "earn_money"}}}}；{{"life_stage": {{"in": ["bootstrap", "settled"]}}}}
- 规则按优先级排列（先紧急后常态），覆盖关键现实情形：
  1) 真正饿了（hungry 很低）→ 附近有食物 eat，没食物 self_gather（或 withdraw 取仓库）
  2) 累到极限（tired 很高）→ 有床 sleep，没床 ground_sleep
  3) 生命/精气神危险（hp/spirit 很低）→ 先恢复
  4) 有明确目标且状态正常 → 推进目标（赚钱→self_gather/pickup、建造→self_build、囤粮→采集食物、造装备→采集资源）
  5) 精力旺盛无压力 → 经营/社交/拾取搬运（store/withdraw/post_bounty/self_carry/self_plant/pickup）
  6) 实在无事可做才 idle/wander
- 字段名必须是上面列出的；action 必须是行为英文名；阈值须在字段取值范围内
- 最后建议加一条兜底规则（when 为空对象 {{}}，或最宽泛条件），保证任何状态都能匹配到行为
  （如 {{"when": {{}}, "action": "idle"}}）；若你确认前面规则已覆盖全部情况可不加
- 输出 8~20 条规则，宁可多覆盖不要漏场景"""


def _parse_rules(text: str) -> list[dict] | None:
    """解析模型输出的规则 JSON（容忍 markdown 代码块/多余文字）。成功返回规则 dict 列表。"""
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
    if isinstance(data, dict) and isinstance(data.get("rules"), list):
        return [r for r in data["rules"] if isinstance(r, dict)]
    return None


def _validate_rules(rules: list[dict], schema_keys: list[str]) -> list[dict]:
    """校验规则：字段名在 schema_keys、约束运算符/值类型合法、action 在 ACTIONS。返回合法子集。

    空 when（``{}``）= 兜底规则：任何状态都命中（``derive`` 的 ``all([])`` 恒真），
    排到列表末尾保证「最后命中」——前置规则不匹配时兜底保底，避免全无票抛错。
    """
    from .rule_teacher import _SYM_OPS  # 共享运算符归一（避免重复定义）

    valid = []
    for r in rules:
        when = r.get("when")
        action = r.get("action")
        if not isinstance(when, dict) or action not in ACTIONS:
            continue
        if not when:
            valid.append(r)  # 兜底规则：无约束，保留并稍后重排到末尾
            continue
        ok = True
        for field, cond in when.items():
            if field not in schema_keys or not isinstance(cond, dict) or not cond:
                ok = False
                break
            for op, target in cond.items():
                op = _SYM_OPS.get(op, op) if isinstance(op, str) else op
                if op in ("lt", "lte", "gt", "gte"):
                    if not isinstance(target, (int, float)):
                        ok = False
                elif op == "btw":
                    if not (isinstance(target, (list, tuple)) and len(target) == 2
                            and all(isinstance(x, (int, float)) for x in target)):
                        ok = False
                elif op == "in":
                    if not isinstance(target, (list, tuple)) or not target:
                        ok = False
                elif op != "eq":
                    ok = False  # 未知运算符
                if not ok:
                    break
            if not ok:
                break
        if ok:
            valid.append(r)
    # 兜底规则（空 when）重排到末尾：保证前置规则不匹配时才兜底
    valid = [r for r in valid if r.get("when")] + [r for r in valid if not r.get("when")]
    return valid


def _first_visible(page, selectors: tuple[str, ...]) -> Any:
    """返回第一个可见的元素（无则 None）。"""
    for sel in selectors:
        try:
            loc = page.locator(sel)
            if loc.count() > 0 and loc.first.is_visible():
                return loc.first
        except Exception:
            continue
    return None


def _extract_partial_labels(text: str) -> list[str] | None:
    """从回复里提取 labels 列表（容忍不完整/末尾带反问文字，如 wenxin 惰性输出）。

    支持 ``{"labels": [...]}`` 对象与纯 ``[...]`` 数组（追问补全轮的输出形态）。
    非法项丢弃；提取不到返回 None。
    """
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
    if isinstance(data, dict) and isinstance(data.get("labels"), list):
        return [v for v in data["labels"] if isinstance(v, str) and v in ACTIONS]
    m = re.search(r"\[.*\]", cleaned, re.S)
    if m:
        try:
            arr = json.loads(m.group(0))
            if isinstance(arr, list):
                return [v for v in arr if isinstance(v, str) and v in ACTIONS]
        except json.JSONDecodeError:
            pass
    return None


class WebTeacher:
    """单平台网页打标签器（与 LLMTeacher 同接口）。"""

    def __init__(self, cfg, schema, platform, progress_file: Optional[Path] = None):
        llm_cfg = cfg["llm"] if isinstance(cfg, dict) else cfg.get("llm", {})
        web_cfg = llm_cfg.get("web", {})
        self.platform = platform
        self.source = "web"  # 标签来源标记：纳入缓存 key，防不同教师缓存串扰
        self.model = f"{platform.name}（网页版）"
        # 平台级 batch_size 优先（个别平台对长 prompt 处理差，需在 Platform 定义里调小）
        self.batch_size = int(platform.batch_size) if platform.batch_size else int(
            web_cfg.get("batch_size", llm_cfg.get("batch_size", 32)))
        self.headless = bool(web_cfg.get("headless", False))
        # 平台级延迟优先（风控严的平台在 Platform.delay_sec 调大；0 = config 全局值）
        self.delay_sec = float(platform.delay_sec) if platform.delay_sec else float(
            web_cfg.get("delay_sec", 2))
        self.stable_sec = float(web_cfg.get("stable_sec", 3))
        self.chat_timeout = float(web_cfg.get("chat_timeout", 240))
        self.max_retries = int(llm_cfg.get("max_retries", 3))
        self.login_timeout = float(web_cfg.get("login_timeout", 300))
        self._system_prompt = build_system_prompt(schema)

        profile_root = Path(web_cfg.get("profile_dir", "data/browser_profile"))
        if not profile_root.is_absolute():
            profile_root = PROJECT_ROOT / profile_root
        self.profile_dir = profile_root / platform.name
        self._migrate_legacy_profile(profile_root)  # 旧版 DeepSeek 会话迁到子目录
        self.progress_file = Path(progress_file) if progress_file else None

        self._pw = None
        self._ctx = None
        self._page = None

    # ------------------------------------------------------------------
    # 浏览器生命周期
    # ------------------------------------------------------------------
    @staticmethod
    def _migrate_legacy_profile(profile_root: Path) -> None:
        """2026-08-22 前 DeepSeek 会话直接放 profile 根目录；现在每平台一个子目录，做一次性迁移。"""
        old = profile_root
        new = profile_root / "deepseek"
        if new.exists() or not (old / "Default").exists():
            return
        print(f"[web_labeler] 迁移旧 DeepSeek 登录会话: {old} -> {new}")
        shutil.copytree(old, new, dirs_exist_ok=True)

    def _launch(self) -> None:
        if self._page is not None:
            return
        from playwright.sync_api import sync_playwright

        self._pw = sync_playwright().start()
        self.profile_dir.mkdir(parents=True, exist_ok=True)
        self._ctx = self._pw.chromium.launch_persistent_context(
            str(self.profile_dir),
            headless=self.headless,
            args=["--disable-blink-features=AutomationControlled"],
        )
        self._page = self._ctx.pages[0] if self._ctx.pages else self._ctx.new_page()
        self._page.goto(self.platform.url)

    def _close(self) -> None:
        try:
            if self._ctx is not None:
                self._ctx.close()
        except Exception:
            pass
        if self._pw is not None:
            self._pw.stop()
        self._ctx = self._page = None

    def _ensure_ready(self, timeout: Optional[float] = None) -> None:
        """等登录完成：任一输入框候选可见即视为已登录。timeout=None 用 login_timeout。"""
        page = self._page
        deadline = time.time() + (timeout if timeout is not None else self.login_timeout)
        while time.time() < deadline:
            if _first_visible(page, self.platform.editor) is not None:
                # 登录就绪后校准一次模型选择（防 profile 漂移，平台有 model_selector 才动作）
                if not getattr(self, "_model_selected", False):
                    self._select_model()
                    self._model_selected = True
                return
            time.sleep(2)
        raise RuntimeError(f"登录超时/未完成（{self.platform.name} 输入框一直未出现）")

    def _select_model(self) -> None:
        """启动时按 platform.model_selector 校准模型选择（页面有目标选项才动作）。

        当前按钮文字匹配才动作（幂等：已是目标模型时无匹配跳过）；下拉无目标选项或点击
        失败时静默跳过，不阻塞打标（profile 已记住时此步可无操作）。
        """
        page = self._page
        for cur, target in self.platform.model_selector:
            try:
                cur_el = page.locator(f"text='{cur}'").first
                if cur_el.count() == 0 or not cur_el.is_visible():
                    continue
                cur_el.click()
                time.sleep(1.0)
                tgt_el = page.locator(f"text='{target}'").first
                if tgt_el.count() == 0 or not tgt_el.is_visible():
                    page.keyboard.press("Escape")
                    continue
                tgt_el.click()
                time.sleep(1.0)
                # 校验：当前按钮不再是 cur（已是目标）才算成功
                if page.locator(f"text='{cur}'").first.count() == 0:
                    print(f"[web_labeler:{self.platform.name}] 模型校准: {cur} -> {target}")
            except Exception:
                continue

    # ------------------------------------------------------------------
    # 对话操作
    # ------------------------------------------------------------------
    def _new_chat(self) -> None:
        """开启新会话（防上下文串扰）。点击后确认助手消息数归零；无新对话按钮则刷新兜底。"""
        page = self._page
        for sel in self.platform.new_chat:
            try:
                loc = page.locator(sel)
                if loc.count() == 0 or not loc.first.is_visible():
                    continue
                loc.first.click()
                if self._wait_until(lambda: len(self._assistant_texts()) == 0, timeout=4):
                    time.sleep(1)
                    return
            except Exception:
                continue
        # 兜底：刷新页面（回到当前会话，仍可继续；正确性由 n_before 等待机制保证）
        page.goto(self.platform.url)
        time.sleep(3)

    def _send_prompt(self, prompt: str) -> None:
        """把提示词填入输入框并发送；确认输入框清空（消息已送出），失败则降级按键/发送按钮。

        注意：部分平台输入框是 React 重挂载的（fill 时元素被 detach），每次操作都按选择器
        重新查元素，不用上一次的旧句柄。
        """
        page = self._page
        ed = _first_visible(page, self.platform.editor)
        if ed is None:
            raise RuntimeError(f"找不到输入框（{self.platform.name}）")
        tag = ed.evaluate("e => e.tagName").upper()
        if tag == "DIV":
            # contenteditable 富文本（ProseMirror/tiptap 等）：fill 不触发框架状态，用真实键入
            ed.click()
            page.keyboard.insert_text(prompt)
        else:
            try:
                ed.fill(prompt)          # textarea/input
            except Exception:
                ed.click()
                page.keyboard.insert_text(prompt)
        for key in self.platform.send_keys:
            page.keyboard.press(key)
            if self._editor_cleared(timeout=6):
                return
        if self.platform.send_button:
            btn = _first_visible(page, (self.platform.send_button,))
            if btn is not None:
                btn.click()
                if self._editor_cleared(timeout=8):
                    return
        raise RuntimeError(f"发送失败：输入框未清空（{self.platform.name} 快捷键/发送按钮均未生效）")

    def _editor_cleared(self, timeout: float) -> bool:
        """发送后输入框内容应清空（React 清空）。按选择器重新查元素，避免重挂载误判；
        contenteditable 富文本用 inner_text 判断，input/textarea 用 input_value。"""
        deadline = time.time() + timeout
        while time.time() < deadline:
            ed = _first_visible(self._page, self.platform.editor)
            if ed is None:
                return True  # 输入框消失，视为已发送
            try:
                val = ed.input_value()          # input/textarea
            except Exception:
                try:
                    val = ed.inner_text() or ""  # contenteditable 富文本
                except Exception:
                    time.sleep(1)
                    continue
            if not val.strip():
                return True
            time.sleep(1)
        return False

    def _wait_until(self, pred, timeout: float) -> bool:
        deadline = time.time() + timeout
        while time.time() < deadline:
            try:
                if pred():
                    return True
            except Exception:
                pass
            time.sleep(1)
        return False

    def _set_toggle(self, label: str, on: bool) -> None:
        """把指定开关（按 span 文字，带 aria-pressed）设为 on/off；找不到静默跳过。

        各平台用带 aria-pressed 的 div 切换按钮 + span 文字标签（class 常混淆不可靠），
        按文字找最近带 aria-pressed 的元素，状态不符才点击。
        """
        page = self._page
        try:
            page.evaluate(
                """([label, on]) => {
                    const spans = [...document.querySelectorAll('span')]
                        .filter(s => s.textContent.trim() === label);
                    for (const s of spans) {
                        let t = s.closest('[aria-pressed]');
                        if (!t) continue;
                        const now = t.getAttribute('aria-pressed') === 'true'
                            || /(selected|active)/.test(t.className || '');
                        if (now !== on) { t.click(); return true; }
                    }
                    return false;
                }""",
                [label, on],
            )
        except Exception:
            pass

    def _disable_toggles(self) -> None:
        """关闭「深度思考」「联网搜索」类开关：加快生成、防搜索跑偏（打标用）。"""
        for label in self.platform.toggles:
            self._set_toggle(label, False)

    def _assistant_texts(self) -> list[str]:
        page = self._page
        for sel in self.platform.markdown:
            try:
                loc = page.locator(sel)
                if loc.count() > 0:
                    return [el.inner_text() for el in loc.all()]
            except Exception:
                continue
        return []

    def _wait_reply(self, n_before: int, timeout: Optional[float] = None) -> str:
        """等最后一条助手消息出现并返回其文本。

        完成判定：文本稳定 stable_sec 秒；或回复以 `}` 结尾（JSON 闭合）且稳定 1.5s
        ——流式输出下结尾 `}` 只在真正结束时出现，可提前判定。
        """
        page = self._page
        deadline = time.time() + (timeout if timeout is not None else self.chat_timeout)
        last_text = ""
        stable_since = time.time()
        while time.time() < deadline:
            texts = self._assistant_texts()
            if len(texts) > n_before:
                cur = texts[-1]
                # 最后一条为空 = 回复还在生成（wenxin 深度思考时思考内容先渲染、
                # 答案容器空着排在末尾），跳过稳定性判定继续等，防空回复误判完成
                if not cur.strip():
                    time.sleep(2)
                    continue
                if cur != last_text:
                    last_text = cur
                    stable_since = time.time()
                else:
                    stable = time.time() - stable_since
                    if stable > self.stable_sec or (cur.strip().endswith("}") and stable > 1.5):
                        print(f"[web_labeler:{self.platform.name}] 回复判定完成 len={len(cur)} "
                              f"结尾={cur.strip()[-60:]!r} 稳定={stable:.1f}s 助手消息数={len(texts)}")
                        return cur
            time.sleep(2)
        raise TimeoutError(f"回复超时（{self.platform.name}）")

    # ------------------------------------------------------------------
    # 打标签（单平台顺序版，供冒烟/回归）
    # ------------------------------------------------------------------
    def label(self, states: list[dict[str, Any]]) -> list[str]:
        n = len(states)
        out = self._load_progress(n)
        self._launch()
        try:
            self._ensure_ready()
            for i in range(0, n, self.batch_size):
                batch = states[i:i + self.batch_size]
                if all(x is not None for x in out[i:i + self.batch_size]):
                    continue  # 已完成批次（断点续跑）
                labels = self._label_batch(batch)
                out[i:i + self.batch_size] = labels
                self._save_progress(out)
                print(f"[web_labeler:{self.platform.name}] 已打 {min(i + self.batch_size, n)}/{n}")
        finally:
            self._close()
        if any(x is None for x in out):
            raise RuntimeError("存在未完成标签（进程可能被中断，重跑续传）")
        return out

    def _label_batch(self, batch: list[dict[str, Any]]) -> list[str]:
        """成功返回长度 n 的全合法标签列表；失败抛异常（不兜底默认 action，由 pool 换平台重打）。"""
        prompt = self._system_prompt + "\n\n" + build_user_prompt(batch)
        last_err: Optional[Exception] = None
        for attempt in range(self.max_retries + 1):
            try:
                self._ensure_ready()
                self._new_chat()          # 新会话，防上下文串扰
                self._disable_toggles()  # 关深度思考/联网搜索：加快生成、防搜索跑偏
                n_before = len(self._assistant_texts())  # 发送前的助手消息数
                self._send_prompt(prompt)
                text = self._wait_reply(n_before)  # 等消息数超过发送前，再取最新一条
                parsed = _parse_labels(text, len(batch))
                # 批内个别非法项也算失败（用户要求不兜底）：只有全合法才接受
                if parsed is not None and all(a is not None for a in parsed):
                    time.sleep(self.delay_sec)  # 批间降速：成功一批后停顿，防触发限流（此前只在重试时 sleep，批间无停顿）
                    return parsed
                # wenxin 惰性输出：只给部分 labels 还反问「要不要补全」→ 追问拿剩余
                completed = self._complete_labels(text, len(batch))
                if completed is not None:
                    time.sleep(self.delay_sec)
                    return completed
                # 诊断：抓到什么、助手消息节点数、流式状态 → 区分截断/多节点/JSON 非法
                try:
                    _ts = self._assistant_texts()
                    _dss = self._page.evaluate("""() => {
                        const e = document.querySelector('[data-streaming]');
                        return e ? e.getAttribute('data-streaming') : null;
                    }""")
                    print(f"[web_labeler:{self.platform.name}] 诊断: 助手消息数={len(_ts)} "
                          f"各长度={[len(t) for t in _ts][-4:]} data-streaming={_dss}")
                except Exception:
                    pass
                last_err = ValueError(
                    f"输出无法解析/长度不符: len={len(text)} 开头={text[:150]!r} 结尾={text[-150:]!r}")
            except Exception as e:
                last_err = e
            if attempt < self.max_retries:
                print(f"[web_labeler:{self.platform.name}] 批次重试 {attempt + 1}/{self.max_retries}: {last_err}")
                time.sleep(self.delay_sec)
        raise RuntimeError(
            f"[web_labeler:{self.platform.name}] 批次 {len(batch)} 条重试 {self.max_retries + 1} 次失败，"
            f"不兜底默认 action: {last_err}")
    def _complete_labels(self, first_text: str, n: int) -> list[str] | None:
        """处理 wenxin 惰性输出：首轮只给部分 labels + 反问「要不要补全」→ 追问拿剩余。

        在同一会话内追加「请只输出剩余 N 个 labels 的 JSON 数组」，每轮合并新解析出的
        labels，最多补 3 轮；凑满 n 返回完整列表，否则返回 None（走重试/兜底）。
        """
        have = _extract_partial_labels(first_text)
        if not have or len(have) >= n:
            return None
        labels = list(have)
        for _ in range(3):
            n_missing = n - len(labels)
            if n_missing <= 0:
                break
            n_before = len(self._assistant_texts())
            self._send_prompt(
                f"是的，请只输出剩余 {n_missing} 个 labels 的 JSON 数组，不要任何其它文字。")
            try:
                text = self._wait_reply(n_before, timeout=60)
            except Exception as e:
                print(f"[web_labeler:{self.platform.name}] 补全轮失败: {e}")
                break
            more = _extract_partial_labels(text)
            if not more:
                print(f"[web_labeler:{self.platform.name}] 补全轮无可解析 labels: {text[:100]!r}")
                break
            labels += more
        if len(labels) >= n:
            print(f"[web_labeler:{self.platform.name}] 补全完成：首轮 {len(have)} + 追问补 {n - len(have)}")
            return labels[:n]
        return None

    # ------------------------------------------------------------------
    # 可用性判定（登录可达 + 冒烟回复）
    # ------------------------------------------------------------------

    # ------------------------------------------------------------------
    # 可用性判定（登录可达 + 冒烟回复）
    # ------------------------------------------------------------------
    def check_usable(self, timeout: float = 30, skip_launch: bool = False) -> bool:
        """判平台是否真的可用：能登录 + 发极简提示能收到回复。

        这是「一开始判断是否可用」的自动化实现——选择器没校准/没登录/被风控的平台
        在此如实返回 False，不会混进并行池污染标签。
        """
        try:
            if not skip_launch:
                self._launch()
            self._ensure_ready(timeout=timeout)
        except Exception as e:
            print(f"[web_labeler:{self.platform.name}] 不可用（登录/入口）：{e}")
            return False
        try:
            self._new_chat()
            self._disable_toggles()
            n_before = len(self._assistant_texts())
            self._send_prompt(PROBE_PROMPT)
            text = self._wait_reply(n_before, timeout=45)
            ok = "OK" in text.upper() or len(text.strip()) <= 30
            print(f"[web_labeler:{self.platform.name}] 冒烟{'通过' if ok else '失败'}：回复={text.strip()[:60]!r}")
            return ok
        except Exception as e:
            print(f"[web_labeler:{self.platform.name}] 不可用（冒烟异常）：{e}")
            return False

    # ------------------------------------------------------------------
    # 规则生成（--gen-rules）：让本平台写一份结构化规则
    # ------------------------------------------------------------------
    def generate_rule_set(self, schema, rule_dir: Path) -> dict | None:
        """让本平台写一份结构化规则文件内容；失败返回 None（重试 max_retries 次）。

        返回 ``{version, platform, schema_keys, fields, rules}``（fields 由程序填充，
        rules 为模型所写并经 ``_validate_rules`` 校验）。
        """
        schema_keys = list(derive_state_bounds(schema))
        prompt = build_rule_prompt(schema)
        for attempt in range(self.max_retries + 1):
            try:
                self._ensure_ready()
                self._new_chat()
                # 写规则用深度思考（推理字段语义/覆盖边界），但关闭联网搜索（防外部信息干扰规则推导）。
                # 平台 toggles 为空（如 wenxin 新版已无「深度思考」开关，改快速/任务模式）则此处空转，
                # 该平台规则按普通模式生成，深度思考由 DeepSeek API 通道（deepseek-reasoner）保证。
                for label in self.platform.toggles:
                    self._set_toggle(label, "深度思考" in label or "Reasoning" in label)
                n_before = len(self._assistant_texts())
                self._send_prompt(prompt)
                text = self._wait_reply(n_before)
                parsed = _parse_rules(text)
                valid = _validate_rules(parsed, schema_keys) if parsed else []
                if valid:
                    print(f"[gen-rules:{self.platform.name}] 模型写规则 {len(parsed)} 条，"
                          f"校验通过 {len(valid)} 条")
                    return {
                        "version": 2,
                        "platform": self.platform.name,
                        "schema_keys": schema_keys,
                        "fields": build_rule_fields(schema),
                        "rules": valid,
                    }
                print(f"[gen-rules:{self.platform.name}] 规则校验失败: "
                      f"len={len(text)} 开头={text[:120]!r}")
            except Exception as e:
                print(f"[gen-rules:{self.platform.name}] 失败: {e}")
            if attempt < self.max_retries:
                print(f"[gen-rules:{self.platform.name}] 重试 {attempt + 1}/{self.max_retries}")
                time.sleep(self.delay_sec)
        return None

    # ------------------------------------------------------------------
    # 断点续跑
    # ------------------------------------------------------------------
    def _load_progress(self, n: int) -> list[Optional[str]]:
        if self.progress_file and self.progress_file.exists():
            try:
                arr = json.loads(self.progress_file.read_text(encoding="utf-8"))
                if isinstance(arr, list) and len(arr) == n:
                    print(f"[web_labeler] 从断点续跑：{sum(1 for x in arr if x is not None)}/{n} 已打")
                    return arr
            except Exception:
                pass
        return [None] * n

    def _save_progress(self, arr: list[Optional[str]]) -> None:
        if self.progress_file:
            self.progress_file.parent.mkdir(parents=True, exist_ok=True)
            self.progress_file.write_text(json.dumps(arr, ensure_ascii=False), encoding="utf-8")

    # ------------------------------------------------------------------
    # 探活：dump DOM 结构（登录后校准选择器）
    # ------------------------------------------------------------------
    def probe_dom(self) -> None:
        page = self._page
        print(f"=== {self.platform.name} 输入框候选 ===")
        for sel in self.platform.editor:
            try:
                loc = page.locator(sel)
                if loc.count():
                    print(f"  {sel}: 命中 {loc.count()} 个, 可见={loc.first.is_visible()}, "
                          f"class={loc.first.get_attribute('class')!r}")
            except Exception:
                continue
        print("=== 可见输入/文本域 ===")
        try:
            for el in page.query_selector_all(
                    "textarea, input[type='text'], [contenteditable='true'], [role='textbox']")[:10]:
                try:
                    ph = el.get_attribute("placeholder")
                    cls = el.get_attribute("class")
                    print(f"  <{el.evaluate('e => e.tagName')}> placeholder={ph!r} class={cls!r}")
                except Exception:
                    continue
        except Exception:
            pass
        print("=== 按钮（前 40 个，含文字）===")
        try:
            for el in page.query_selector_all("button")[:40]:
                txt = el.inner_text().strip()[:40]
                cls = el.get_attribute("class")
                if txt or cls:
                    print(f"  button text={txt!r} class={cls!r}")
        except Exception:
            pass
        print("=== class 含 markdown/message/answer/send/new/stop 的元素 ===")
        for sel in ("[class*='markdown']", "[class*='message']", "[class*='answer']",
                    "[class*='send']", "[class*='new']", "[class*='stop']", "[class*='chat-input']"):
            try:
                loc = page.locator(sel)
                if loc.count():
                    print(f"  {sel}: {loc.count()} 个")
            except Exception:
                continue
        print("=== 常见按钮/开关文字是否出现 ===")
        for label in ("深度思考", "联网搜索", "搜索", "新对话", "开启新对话", "发送", "停止"):
            try:
                if page.locator(f"span:has-text('{label}')").count():
                    print(f"  span:has-text('{label}'): 命中")
            except Exception:
                continue
        html = page.content()
        probe_html = self.profile_dir / "_probe.html"
        probe_html.write_text(html, encoding="utf-8")
        print(f"=== 完整 HTML 已存 {probe_html}（{len(html)} 字符，可 grep 定位）===")


class WebTeacherPool:
    """多平台并行打标签器（与单教师同接口 label/_system_prompt/source/batch_size）。

    批次队列分发给「可用平台」（check_usable 通过者），每平台一个线程、各建自己的
    Playwright 上下文。逐批落盘进度，进程崩溃/登录过期后重跑从断点续传。
    """

    def __init__(self, teachers: list[WebTeacher], progress_file: Optional[Path] = None,
                 check_timeout: float = 30):
        self.teachers = teachers
        self.progress_file = Path(progress_file) if progress_file else None
        self.check_timeout = check_timeout
        self.batch_size = max((t.batch_size for t in teachers), default=32)
        self.source = "web"
        self.model = "+".join(t.model for t in teachers) if teachers else "web"
        self._system_prompt = teachers[0]._system_prompt if teachers else ""

    def label(self, states: list[dict[str, Any]]) -> list[str]:
        n = len(states)
        out = self._load_progress(n)
        if not self.teachers:
            raise RuntimeError("没有可用的网页平台（web.platforms 为空）")
        lock = threading.Lock()
        cursor = [0]  # 共享游标：每平台按各自 batch_size 动态拉取下一段，快平台多拉、慢平台少拉
        failed: list = []  # 失败批次 [start, end, tried_platforms:set]，换平台重打
        usable = {t.platform.name: False for t in self.teachers}
        t0 = time.monotonic()

        def worker(t: WebTeacher) -> None:
            if not t.check_usable(self.check_timeout):
                t._close()
                return
            usable[t.platform.name] = True
            try:
                while True:
                    # 1) 优先接失败批次（换平台重打，只接自己没试过的）；2) 否则游标拉新
                    span = None
                    with lock:
                        for item in failed:
                            if t.platform.name not in item[2]:
                                item[2].add(t.platform.name)
                                span = (item[0], item[1])
                                break
                    if span is None:
                        with lock:
                            start = cursor[0]
                            # 跳过断点续跑中已完成的段（动态游标不预置固定队列，需在此排除）
                            while start < n and all(
                                    x is not None for x in out[start:min(start + t.batch_size, n)]):
                                start += t.batch_size
                            cursor[0] = start
                            if start >= n:
                                break
                            end = min(start + t.batch_size, n)
                            cursor[0] = end
                            span = (start, end)
                    try:
                        labels = t._label_batch(states[span[0]:span[1]])
                    except Exception as e:
                        with lock:
                            if not any(item[0] == span[0] and item[1] == span[1] for item in failed):
                                failed.append([span[0], span[1], {t.platform.name}])
                            print(f"[web_pool:{t.platform.name}] 批次 {span[0]}:{span[1]} 失败，"
                                  f"换平台重打: {e}")
                        continue
                    with lock:
                        out[span[0]:span[1]] = labels
                        # 成功：从失败列表移除（该批次已被换平台接管完成）
                        for i, item in enumerate(failed):
                            if item[0] == span[0] and item[1] == span[1]:
                                del failed[i]
                                break
                        self._save_progress(out)
                        done = sum(1 for x in out if x is not None)
                        print(f"[web_pool:{t.platform.name}] 已打 {done}/{n}（本批 {len(labels)} 条，"
                              f"总耗时 {time.monotonic() - t0:.0f}s）")
            finally:
                t._close()

        threads = [threading.Thread(target=worker, args=(t,), daemon=True) for t in self.teachers]
        for th in threads:
            th.start()
        for th in threads:
            th.join()

        ok_names = [name for name, ok in usable.items() if ok]
        print(f"[web_pool] 可用平台: {ok_names or '（无）'}")
        if not ok_names:
            raise RuntimeError(
                "所有平台均不可用（未登录/选择器未校准/冒烟失败）。\n"
                "请先 python -m src.web_labeler --probe-all 登录并校准，再重跑。")
        if failed:
            spans = [(s, e) for s, e, _ in failed]
            raise RuntimeError(
                f"以下批次所有可用平台均失败（不兜底默认 action，数据不残缺）：{spans}\n"
                f"请检查平台可用性后重跑（断点续传会跳过已成功批次）。")
        if any(x is None for x in out):
            raise RuntimeError("存在未完成标签（可用平台中途失败），重跑断点续传")
        return out

    # ---- 断点续跑（与单教师共享同格式）----
    def _load_progress(self, n: int) -> list[Optional[str]]:
        if self.progress_file and self.progress_file.exists():
            try:
                arr = json.loads(self.progress_file.read_text(encoding="utf-8"))
                if isinstance(arr, list) and len(arr) == n:
                    print(f"[web_pool] 从断点续跑：{sum(1 for x in arr if x is not None)}/{n} 已打")
                    return arr
            except Exception:
                pass
        return [None] * n

    def _save_progress(self, arr: list[Optional[str]]) -> None:
        if self.progress_file:
            self.progress_file.parent.mkdir(parents=True, exist_ok=True)
            self.progress_file.write_text(json.dumps(arr, ensure_ascii=False), encoding="utf-8")

    # ------------------------------------------------------------------
    # 规则生成（--gen-rules）：每个可用平台各写一份结构化规则文件
    # ------------------------------------------------------------------
    def generate_rules(self, schema, rule_dir: Path) -> dict[str, int]:
        """让每个可用平台各写一份规则文件到 rule_dir/<platform>.json（断点续跑：已存在跳过）。

        平台冒烟失败/写规则失败 → 跳过不阻塞其余平台。返回 {platform: 规则条数}。
        """
        rule_dir = Path(rule_dir)
        if not rule_dir.is_absolute():
            rule_dir = PROJECT_ROOT / rule_dir
        rule_dir.mkdir(parents=True, exist_ok=True)
        results: dict[str, int] = {}
        lock = threading.Lock()

        def worker(t: WebTeacher) -> None:
            pname = t.platform.name
            out = rule_dir / f"{pname}.json"
            if out.exists():
                try:
                    data = json.loads(out.read_text(encoding="utf-8"))
                    n = len(data.get("rules", [])) if isinstance(data, dict) else 0
                    print(f"[gen-rules] 断点续跑：{pname} 已生成（{n} 条规则），跳过")
                    with lock:
                        results[pname] = n
                except Exception:
                    pass
                t._close()
                return
            if not t.check_usable(self.check_timeout):
                t._close()
                return
            try:
                doc = t.generate_rule_set(schema, rule_dir)
            finally:
                t._close()
            if doc is None:
                print(f"[gen-rules:{pname}] 写规则失败，跳过")
                return
            out.write_text(json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")
            with lock:
                results[pname] = len(doc["rules"])
            print(f"[gen-rules] {pname} 规则已写入 {out}")

        threads = [threading.Thread(target=worker, args=(t,), daemon=True)
                   for t in self.teachers]
        for th in threads:
            th.start()
        for th in threads:
            th.join()
        return results


# ---------------------------------------------------------------------------
# 工厂 + CLI
# ---------------------------------------------------------------------------
def _cfg():
    cfg = ModelConfig()
    schema = FeatureSchema.load(cfg.schema_path)
    return cfg, schema


def _platform_names(ap_args) -> list[str]:
    cfg = _cfg()[0]
    llm_cfg = cfg["llm"]
    web_cfg = llm_cfg.get("web", {})
    if getattr(ap_args, "probe_all", False) or not getattr(ap_args, "platforms", None):
        names = web_cfg.get("platforms", [])
        if not names:
            names = ["deepseek", "wenxin", "qianwen"]
    else:
        names = [x.strip() for x in ap_args.platforms.split(",") if x.strip()]
    return names


def make_teachers(cfg, schema, names: list[str], progress_file=None) -> list[WebTeacher]:
    teachers = []
    for name in names:
        try:
            teachers.append(WebTeacher(cfg, schema, get_platform(name), progress_file=progress_file))
        except KeyError as e:
            print(f"[web_labeler] 跳过未知平台: {e}")
    return teachers


def make_pool(cfg, schema, names: list[str], progress_file=None) -> WebTeacherPool:
    check_timeout = float(cfg["llm"].get("web", {}).get("check_timeout", 30))
    return WebTeacherPool(make_teachers(cfg, schema, names, progress_file),
                          progress_file=progress_file, check_timeout=check_timeout)


def probe_all(names: list[str]) -> None:
    """逐个打开平台等用户登录 → 判定可用性 → dump DOM 供校准选择器。"""
    cfg, schema = _cfg()
    print(f"[probe-all] 将逐个打开 {len(names)} 个平台：{names}")
    print("[probe-all] 请在各自浏览器窗口登录（手机号/扫码）；登录完成后脚本自动继续（每平台最多等登录超时）。")
    print("[probe-all] 冒烟判定的含义：登录可达 + 能收到回复 = 可用；只登录但发消息失败 = 选择器未校准。")
    results = []
    for name in names:
        print(f"\n=== 探活平台: {name}（{get_platform(name).note}） ===")
        t = WebTeacher(cfg, schema, get_platform(name))
        try:
            t._launch()
            try:
                t._ensure_ready()  # 长超时，等用户登录
                editor_ok = True
                print(f"[probe] {name}：已登录（找到输入框）")
            except RuntimeError as e:
                editor_ok = False
                print(f"[probe] {name}：未检测到登录 —— {e}")
            t.probe_dom()
            if editor_ok:
                ok = t.check_usable(timeout=30, skip_launch=True)
                results.append((name, "可用（登录+冒烟通过）" if ok else "已登录但冒烟失败（选择器待校准）"))
            else:
                results.append((name, "未登录/不可达"))
        finally:
            t._close()
    print("\n=== 探活汇总 ===")
    for name, status in results:
        print(f"  {name:<10s} {status}")
    usable = [n for n, s in results if s.startswith("可用")]
    print(f"[probe-all] 可用平台: {usable or '（无，需先登录/校准）'}")
    print("[probe-all] 校准步骤：看各平台 _probe.html dump 的选择器，回填 web_platforms.py 后重跑本命令。")


def main() -> None:
    ap = argparse.ArgumentParser(description="网页版 AI 打标签器（多平台并行）")
    ap.add_argument("--probe-all", action="store_true", help="逐个打开全部平台，登录后判可用性 + dump DOM")
    ap.add_argument("--probe", type=str, metavar="NAME", help="单平台探活（如 deepseek）")
    ap.add_argument("--platforms", type=str, help="覆盖 web.platforms，逗号分隔（如 deepseek,wenxin）")
    ap.add_argument("--states", type=int, default=None, help="冒烟状态条数（默认 4 条冒烟）")
    ap.add_argument("--train", action="store_true", help="打整个训练集（走 generate_data 缓存）")
    ap.add_argument("--gen-rules", action="store_true",
                    help="让每个可用网页平台 + DeepSeek API 各写一份规则文件到 llm.rule_dir"
                         "（写规则用深度思考；provider: rules 的输入）")
    args = ap.parse_args()

    cfg, schema = _cfg()
    if args.probe_all or args.probe:
        names = [args.probe] if args.probe else _platform_names(args)
        probe_all(names)
        return

    names = _platform_names(args)
    print(f"[web_labeler] 平台: {names}")
    if args.gen_rules:
        rule_dir = Path(cfg["llm"].get("rule_dir") or "data/rules/models")
        if not rule_dir.is_absolute():
            rule_dir = PROJECT_ROOT / rule_dir
        print(f"[gen-rules] 让 {names} 各写一份规则文件 -> {rule_dir}/（已存在跳过）")
        pool = make_pool(cfg, schema, names)
        results = pool.generate_rules(schema, rule_dir)
        # DeepSeek API 也作为规则生成模型（深度思考 reasoner 优先；需 DEEPSEEK_API_KEY，无则跳过）
        api_out = rule_dir / "deepseek_api.json"
        if api_out.exists():
            try:
                data = json.loads(api_out.read_text(encoding="utf-8"))
                n = len(data.get("rules", [])) if isinstance(data, dict) else 0
                print(f"[gen-rules] 断点续跑：deepseek_api 已生成（{n} 条规则），跳过")
                results["deepseek_api"] = n
            except Exception:
                pass
        else:
            try:
                from .llm_teacher import LLMTeacher
                doc = LLMTeacher(cfg, schema).generate_rule_set(schema, rule_dir)
                if doc:
                    api_out.write_text(json.dumps(doc, ensure_ascii=False, indent=1),
                                       encoding="utf-8")
                    results["deepseek_api"] = len(doc["rules"])
                    print(f"[gen-rules] deepseek_api 规则已写入 {api_out}")
                else:
                    print("[gen-rules] DeepSeek API 写规则失败，跳过")
            except Exception as e:
                print(f"[gen-rules] DeepSeek API 不可用（未设 DEEPSEEK_API_KEY?）：{e}")
        if not results:
            print("[gen-rules] 无通道成功生成规则（未登录/冒烟失败/规则校验失败/无 API key）")
            return 1
        for pname, n in results.items():
            print(f"[gen-rules]   {pname:<10s} {n} 条规则")
        print(f"[gen-rules] 完成。运行 generate_data（llm.provider: rules）用这些规则打训练集。")
        return
    pool = make_pool(cfg, schema, names)
    if args.train:
        # 训练集走 generate_data 的缓存（含断点续跑），入口统一在 data/generate_data.py
        print("训练集请运行: python data/generate_data.py（先配置 llm.provider=web）")
        return
    tr_states, _ = generate_training_samples(schema, cfg.raw, cfg.data_seed, label_fn=None)
    states = tr_states[: args.states or 4]
    print(f"[web_labeler] 冒烟：{len(states)} 条状态 -> 多平台并行打标签")
    labels = pool.label(states)
    for st, a in zip(states, labels):
        print(f"  hungry={st.get('hungry')} tired={st.get('tired')} nearby_food={st.get('nearby_food')} -> {a}")


if __name__ == "__main__":
    sys.exit(main())
