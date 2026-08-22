"""网页平台定义：各免费 AI 聊天页面的访问信息（URL + 选择器）。

每个平台 DOM 不同，选择器靠 ``--probe-all`` 探活（用户登录 + dump DOM）后校准回填。
未校准的平台只有 URL 与兜底选择器：可用性判定（登录可达 + 冒烟回复）会如实失败，
校准通过后才能进并行池。站点改版时在此微调。

平台 URL（用户提供，2026-08-22）：
    deepseek  chat.deepseek.com                    已校准
    wenxin    wenxin.baidu.com（文心一言，重定向到 yiyan）
    qianwen   platform.qianwenai.com/try-ai/chat?models=qwen3.8-max（通义千问，URL 锚定 qwen3.8-max）
    doubao    www.doubao.com/chat/                 未校准（曾移除后加回，batch 调小重试）
    yuanbao   yuanbao.tencent.com/chat/naQivTmsDa  未校准（2026-08-22 加入）
    kimi      www.kimi.com/                        未校准（2026-08-22 加入）
    chatgpt   chatgpt.com/                         未校准（2026-08-22 加入）

已移除：ark（ark.volcengine.com 在线体验页有时间使用限制）、mimo（aistudio.xiaomimimo.com，
页面无深度思考开关，关不掉深度思考）——均用户 2026-08-22 决定不用。
"""
from __future__ import annotations

from dataclasses import dataclass, replace


@dataclass(frozen=True)
class Platform:
    name: str
    url: str
    # 输入框候选（找到任一可见即视为「已登录、有聊天入口」）
    editor: tuple = (
        "textarea",
        "[contenteditable='true']",
        "[role='textbox']",
        "input[type='text']",
    )
    # 助手回复正文候选（流式输出渲染容器）
    markdown: tuple = (
        "[class*='markdown']",
        ".ds-markdown",
        "[class*='message-content']",
        "[class*='answer']",
    )
    # 新对话按钮候选（span 文字优先；空 = 刷新页面兜底）
    new_chat: tuple = ()
    # 需关闭的开关（span 文字，如 深度思考/联网搜索）
    toggles: tuple = ()
    # 发送快捷键（依次尝试，成功以输入框清空为准）
    send_keys: tuple = ("Enter", "Control+Enter")
    # 发送按钮选择器（Enter/Ctrl+Enter 都不清空输入框时兜底点击；空 = 无按钮）
    send_button: str = ""
    # 每批状态条数（0 = 用 config llm.web.batch_size；个别平台对长 prompt 处理差需调小）
    batch_size: int = 0
    # 批间延迟秒数（0 = 用 config llm.web.delay_sec；风控严的平台调大）
    delay_sec: float = 0
    # 启动时自动校准模型选择：(当前按钮文字, 目标选项文字)，页面无对应下拉则静默跳过
    model_selector: tuple = ()
    note: str = ""


PLATFORM_DEFS: dict[str, Platform] = {
    "deepseek": Platform(
        name="deepseek",
        url="https://chat.deepseek.com/",
        editor=("textarea", "[contenteditable='true']", "[role='textbox']"),
        markdown=(".ds-markdown", "[class*='markdown']"),
        new_chat=("span:has-text('开启新对话')", "button:has-text('新对话')"),
        toggles=("深度思考", "联网搜索"),
        note="已校准（2026-08-22）；2026-08-22 被风控限制（登录可达但发消息失败/降速）→ 全局 delay_sec 已降速，恢复后此平台自动重新可用",
    ),
    "wenxin": Platform(
        name="wenxin", url="https://wenxin.baidu.com/",
        new_chat=("span:has-text('开启新对话')", "button:has-text('新对话')", "span:has-text('新对话')"),
        toggles=("深度思考", "联网搜索"),
        batch_size=16,  # 输入框疑似截断长输入（32 条 26K 字符用户目测被截断）→ 再调小防截断；截断导致只输出部分 labels，配 _complete_labels 追问补全
        note="文心一言（已校准 2026-08-22；长输入疑似被输入框截断→batch_size=16，配合追问补全兜底）",
    ),
    "qianwen": Platform(
        name="qianwen", url="https://platform.qianwenai.com/try-ai/chat?models=qwen3.8-max",
        toggles=("深度思考", "联网搜索", "搜索"),
        batch_size=32,  # 64 条长输出末尾被截断 → 每批缺 1-4 个 labels 触发追问补全；降到 32 减半输出长度
        note="通义千问（URL 直接锚定模型 qwen3.8-max，无需手动选择）",
    ),
    "doubao": Platform(
        name="doubao", url="https://www.doubao.com/chat/",
        toggles=("深度思考", "联网搜索", "搜索"),
        batch_size=16,  # 2026-08-22 曾因大 prompt（64 条整批回显不执行）被移除 → 先降到 16 实测
        note="豆包（未校准 2026-08-22；曾因大 prompt 整批回显不执行 + 触发人机验证被移除后加回，batch=16 重试，选择器待 --probe 校准）",
    ),
    "yuanbao": Platform(
        name="yuanbao", url="https://yuanbao.tencent.com/chat/naQivTmsDa",
        toggles=("深度思考", "联网搜索"),
        batch_size=32,  # 新平台先按 32 防长输出截断（参照 qianwen 64 截断教训）
        note="腾讯元宝（未校准 2026-08-22；URL 为用户提供会话链接，选择器待 --probe 校准回填）",
    ),
    "kimi": Platform(
        name="kimi", url="https://www.kimi.com/",
        toggles=("深度思考", "联网搜索"),
        batch_size=32,  # 新平台先按 32 防长输出截断（参照 qianwen 64 截断教训）
        note="Kimi（未校准 2026-08-22；选择器待 --probe 校准回填）",
    ),
    "chatgpt": Platform(
        name="chatgpt", url="https://chatgpt.com/",
        editor=("#prompt-textarea", "[contenteditable='true']", "[role='textbox']"),
        markdown=("[class*='markdown']", "[class*='message-content']"),
        new_chat=("button:has-text('New chat')", "button:has-text('新建聊天')", "a[href='/']"),
        toggles=("Reasoning", "Search the web", "深度思考", "联网搜索"),  # 界面以英文为主，中英双语；找不到静默跳过
        send_keys=("Enter",),
        batch_size=32,  # 新平台先按 32 防长输出截断（参照 qianwen 64 截断教训）
        note="ChatGPT（未校准 2026-08-22；选择器为预估初值，待 --probe 校准回填；风控严可能触发验证/登录门槛）",
    ),
}


def get_platform(name: str, overrides: dict | None = None) -> Platform:
    """取平台定义；overrides 可覆盖任意字段（config 里的 web.platform_overrides）。"""
    if name not in PLATFORM_DEFS:
        raise KeyError(f"未知平台 '{name}'，可选: {sorted(PLATFORM_DEFS)}")
    p = PLATFORM_DEFS[name]
    if overrides:
        p = replace(p, **{k: v for k, v in overrides.items() if v is not None})
    return p
