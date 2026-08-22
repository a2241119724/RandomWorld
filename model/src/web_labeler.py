"""网页版 DeepSeek 免费打标签器（Playwright 浏览器自动化）。

替代 OpenAI 兼容 API 打标签：特征/输出扩展导致缓存失效时，重打标签不再需要付费
（用户决策，2026-08-22）。

原理：
- 用 Playwright 驱动真实浏览器打开 chat.deepseek.com，使用持久化 profile
  （``data/browser_profile/``）：首次运行手动登录一次，之后复用会话（不存密码）。
- 每批状态构造与 API 路径相同的提示词（``build_user_prompt``），新建对话粘贴并发送，
  等回复结束后抽取最后一条助手消息文本，复用 ``llm_teacher._parse_labels`` 校验。
- 与 ``LLMTeacher`` 同接口（``label(states) -> list[str]``、``_system_prompt``），
  generate_data.py 用「教师来源」纳入缓存 key，两种教师缓存互不串扰。

风险/权衡（已与用户确认）：
- 网页版是聊天界面不是接口，抽取响应依赖 DOM，站点改版即需微调选择器常量。
- 自动化网页版可能触发反爬/限流/封号：用真实浏览器窗口 + 批间停顿降低概率。
- 网页无法设 temperature=0，标签有随机性——靠缓存固化保证可复现（与 API 同理）。
- 断点续跑：每打一批把进度写进 progress 文件，进程崩溃/登录过期后重跑从断点继续。

用法：
    python -m src.web_labeler --probe            # 打开页面 dump DOM（登录后校准选择器）
    python -m src.web_labeler --states 4         # 冒烟：打 4 条状态
    python -m src.web_labeler --train            # 打整个训练集（经 generate_data 缓存）
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path
from typing import Any, Optional

from .actions import ACTIONS
from .config import ModelConfig
from .features import FeatureSchema
from .llm_teacher import build_system_prompt, build_user_prompt, _parse_labels
from .rules import DEFAULT_ACTION, generate_training_samples

PROJECT_ROOT = Path(__file__).resolve().parent.parent

# ---------------------------------------------------------------------------
# chat.deepseek.com DOM 选择器（站点改版时在此微调；--probe 可 dump 实际结构）
# ---------------------------------------------------------------------------
CHAT_URL = "https://chat.deepseek.com/"
# 输入框（旧版 textarea#chat-input；新版可能为 contenteditable / role=textbox）
EDITOR_SELECTORS = [
    "textarea#chat-input",
    "textarea[placeholder*='输入']",
    "textarea",
    "[contenteditable='true']",
    "[role='textbox']",
    "[data-testid='chat-input']",
]
# 助手消息正文（DeepSeek 渲染 markdown 的容器）
MARKDOWN_SELECTORS = [
    ".ds-markdown",
    "[class*='ds-markdown']",
    "[class*='markdown']",
]
# 新对话按钮（DeepSeek 用 span 文字「开启新对话」，class 为混淆名不可靠）
NEW_CHAT_SELECTORS = [
    "span:has-text('开启新对话')",
    "button:has-text('新对话')",
    "span:has-text('新对话')",
    "[class*='new-chat']",
    "[class*='newConversation']",
]
# 停止生成按钮（生成中出现的方块图标按钮；出现后消失 = 回复结束）
STOP_SELECTORS = [
    "[class*='stop']",
    "button[title*='停止']",
    "button[aria-label*='停止']",
]


def _first_visible(page, selectors: list[str]) -> Any:
    """返回第一个可见的元素（无则 None）。"""
    for sel in selectors:
        try:
            loc = page.locator(sel)
            if loc.count() > 0 and loc.first.is_visible():
                return loc.first
        except Exception:
            continue
    return None


class WebTeacher:
    """网页版 DeepSeek 打标签器（与 LLMTeacher 同接口）。"""

    def __init__(self, cfg, schema, progress_file: Optional[Path] = None):
        llm_cfg = cfg["llm"] if isinstance(cfg, dict) else cfg.get("llm", {})
        web_cfg = llm_cfg.get("web", {})
        self.source = "web"  # 标签来源标记：纳入缓存 key，防不同教师缓存串扰
        self.model = "chat.deepseek.com（网页版）"
        self.batch_size = int(web_cfg.get("batch_size", llm_cfg.get("batch_size", 32)))
        self.headless = bool(web_cfg.get("headless", False))
        self.delay_sec = float(web_cfg.get("delay_sec", 2))
        self.stable_sec = float(web_cfg.get("stable_sec", 3))
        self.chat_timeout = float(web_cfg.get("chat_timeout", 240))
        self.max_retries = int(llm_cfg.get("max_retries", 3))
        self.login_timeout = float(web_cfg.get("login_timeout", 300))
        self._system_prompt = build_system_prompt(schema)

        profile_dir = web_cfg.get("profile_dir", "data/browser_profile")
        self.profile_dir = Path(profile_dir)
        if not self.profile_dir.is_absolute():
            self.profile_dir = PROJECT_ROOT / self.profile_dir
        self.progress_file = Path(progress_file) if progress_file else None

        self._pw = None
        self._ctx = None
        self._page = None

    # ------------------------------------------------------------------
    # 浏览器生命周期
    # ------------------------------------------------------------------
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
        self._page.goto(CHAT_URL)

    def _close(self) -> None:
        try:
            if self._ctx is not None:
                self._ctx.close()
        except Exception:
            pass
        if self._pw is not None:
            self._pw.stop()
        self._ctx = self._page = None

    def _ensure_ready(self) -> None:
        """等登录完成：输入框可见即视为已登录。"""
        page = self._page
        print("[web_labeler] 若浏览器中未登录，请手动登录 chat.deepseek.com（手机号/扫码），脚本会自动继续…")
        deadline = time.time() + self.login_timeout
        while time.time() < deadline:
            if _first_visible(page, EDITOR_SELECTORS) is not None:
                return
            time.sleep(2)
        raise RuntimeError("登录超时/未完成（输入框一直未出现）")

    # ------------------------------------------------------------------
    # 对话操作
    # ------------------------------------------------------------------
    def _new_chat(self) -> None:
        """开启新会话（防上下文串扰）。点击后确认助手消息数归零。"""
        page = self._page
        for sel in NEW_CHAT_SELECTORS:
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
        page.goto(CHAT_URL)
        time.sleep(3)

    def _send_prompt(self, prompt: str) -> None:
        """把提示词填入输入框并发送；确认输入框清空（消息已送出），失败则降级按键。"""
        page = self._page
        editor = _first_visible(page, EDITOR_SELECTORS)
        if editor is None:
            raise RuntimeError("找不到输入框")
        try:
            editor.fill(prompt)          # contenteditable/textarea 均支持
        except Exception:
            editor.click()
            page.keyboard.insert_text(prompt)
        for key in ("Enter", "Control+Enter"):
            page.keyboard.press(key)
            if self._editor_cleared(editor, timeout=6):
                return
        raise RuntimeError("发送失败：输入框未清空（可能无发送按钮或快捷键变化）")

    def _editor_cleared(self, editor, timeout: float) -> bool:
        """发送后输入框内容应清空（React 清空）。"""
        deadline = time.time() + timeout
        while time.time() < deadline:
            try:
                if not editor.input_value().strip():
                    return True
            except Exception:
                return True  # 元素结构变化，假定已发送
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

    def _disable_toggles(self) -> None:
        """关闭「深度思考」「联网搜索」开关：加快生成、防止模型去联网搜索拖慢/跑偏。

        DeepSeek 用带 aria-pressed 的 div 切换按钮 + span 文字标签（class 混淆不可靠），
        找到文字向上找最近带 aria-pressed 的元素，若为开启态则点击关闭。
        """
        page = self._page
        for label in ("深度思考", "联网搜索", "搜索"):
            try:
                clicked = page.evaluate(
                    """(label) => {
                        const spans = [...document.querySelectorAll('span')]
                            .filter(s => s.textContent.trim() === label);
                        for (const s of spans) {
                            let t = s.closest('[aria-pressed]');
                            if (!t) continue;
                            const on = t.getAttribute('aria-pressed') === 'true'
                                || /(selected|active)/.test(t.className || '');
                            if (on) { t.click(); return true; }
                        }
                        return false;
                    }""",
                    label,
                )
                if clicked:
                    print(f"[web_labeler] 已关闭开关：{label}")
                    time.sleep(0.5)
            except Exception:
                continue

    def _assistant_texts(self) -> list[str]:
        page = self._page
        for sel in MARKDOWN_SELECTORS:
            try:
                loc = page.locator(sel)
                if loc.count() > 0:
                    return [el.inner_text() for el in loc.all()]
            except Exception:
                continue
        return []

    def _wait_reply(self, n_before: int) -> str:
        """等最后一条助手消息出现并返回其文本。

        完成判定：文本稳定 stable_sec 秒；或回复以 `}` 结尾（JSON 闭合）且稳定 1.5s
        ——DeepSeek 是流式输出，结尾 `}` 只在真正结束时出现，可提前判定。
        """
        page = self._page
        deadline = time.time() + self.chat_timeout
        last_text = ""
        stable_since = time.time()
        while time.time() < deadline:
            texts = self._assistant_texts()
            if len(texts) > n_before:
                cur = texts[-1]
                if cur != last_text:
                    last_text = cur
                    stable_since = time.time()
                else:
                    stable = time.time() - stable_since
                    if stable > self.stable_sec or (cur.strip().endswith("}") and stable > 1.5):
                        return cur
            time.sleep(2)
        raise TimeoutError(f"回复超时（{self.chat_timeout:.0f}s）")

    # ------------------------------------------------------------------
    # 打标签
    # ------------------------------------------------------------------
    def label(self, states: list[dict[str, Any]]) -> list[str]:
        """给状态列表打标签（分批 + 断点续跑）。"""
        n = len(states)
        out = self._load_progress(n)
        self._launch()
        t0 = time.monotonic()
        try:
            self._ensure_ready()
            for i in range(0, n, self.batch_size):
                batch = states[i:i + self.batch_size]
                if all(x is not None for x in out[i:i + self.batch_size]):
                    continue  # 已完成批次（断点续跑）
                labels = self._label_batch(batch)
                out[i:i + self.batch_size] = labels
                self._save_progress(out)
                elapsed = time.monotonic() - t0
                print(f"[web_labeler] 已打标签 {min(i + self.batch_size, n)}/{n} 本批耗时 {elapsed:.0f}s（进度已落盘）")
                t0 = time.monotonic()
        finally:
            self._close()
        if any(x is None for x in out):
            raise RuntimeError("存在未完成标签（进程可能被中断，重跑续传）")
        return out

    def _label_batch(self, batch: list[dict[str, Any]]) -> list[str]:
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
                if parsed is not None:
                    return [a if a is not None else DEFAULT_ACTION for a in parsed]
                last_err = ValueError(f"输出无法解析/长度不符: {text[:120]!r}")
            except Exception as e:
                last_err = e
            if attempt < self.max_retries:
                print(f"[web_labeler] 批次重试 {attempt + 1}/{self.max_retries}: {last_err}")
                time.sleep(self.delay_sec)
        print(f"[web_labeler] WARN 批次 {self.max_retries + 1} 次失败，主类兜底: {last_err}")
        return [DEFAULT_ACTION] * len(batch)

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
    # 探活：dump DOM 结构（站点改版时校准选择器）
    # ------------------------------------------------------------------
    def probe(self) -> None:
        self._launch()
        try:
            self._ensure_ready()
        except RuntimeError as e:
            print(f"[probe] {e}")
            return
        page = self._page
        print("=== 输入框候选 ===")
        for sel in EDITOR_SELECTORS:
            loc = page.locator(sel)
            if loc.count():
                print(f"  {sel}: 命中 {loc.count()} 个, 可见={loc.first.is_visible()}, class={loc.first.get_attribute('class')!r}")
        print("=== 按钮（前 40 个，含文字）===")
        for el in page.query_selector_all("button")[:40]:
            txt = el.inner_text().strip()[:40]
            cls = el.get_attribute("class")
            if txt or cls:
                print(f"  button text={txt!r} class={cls!r}")
        print("=== class 含 markdown/send/new/stop 的元素 ===")
        for sel in ("[class*='markdown']", "[class*='send']", "[class*='new-chat']",
                    "[class*='newConversation']", "[class*='stop']"):
            loc = page.locator(sel)
            if loc.count():
                print(f"  {sel}: {loc.count()} 个")
        html = page.content()
        probe_html = self.profile_dir / "_probe.html"
        probe_html.write_text(html, encoding="utf-8")
        print(f"=== 完整 HTML 已存 {probe_html}（{len(html)} 字符，可 grep 定位）===")


def _make_teacher(progress_file: Optional[Path] = None) -> WebTeacher:
    cfg = ModelConfig()
    schema = FeatureSchema.load(cfg.schema_path)
    return WebTeacher(cfg, schema, progress_file=progress_file)


def main() -> None:
    ap = argparse.ArgumentParser(description="网页版 DeepSeek 打标签器")
    ap.add_argument("--probe", action="store_true", help="打开页面 dump DOM 结构（登录后校准选择器）")
    ap.add_argument("--states", type=int, default=4, help="冒烟测试状态条数")
    ap.add_argument("--train", action="store_true", help="打整个训练集（走 generate_data 缓存）")
    args = ap.parse_args()

    if args.probe:
        _make_teacher().probe()
        return

    teacher = _make_teacher()
    cfg = ModelConfig()
    schema = FeatureSchema.load(cfg.schema_path)
    if args.train:
        # 训练集走 generate_data 的缓存（含断点续跑），入口统一在 data/generate_data.py
        print("训练集请运行: python data/generate_data.py（先配置 llm.provider=web）")
        return
    tr_states, _ = generate_training_samples(schema, cfg.raw, cfg.data_seed, label_fn=None)
    states = tr_states[: args.states]
    print(f"[web_labeler] 冒烟：{len(states)} 条状态 -> 网页版打标签")
    labels = teacher.label(states)
    for st, a in zip(states, labels):
        print(f"  hungry={st.get('hungry')} tired={st.get('tired')} nearby_food={st.get('nearby_food')} -> {a}")


if __name__ == "__main__":
    sys.exit(main())
