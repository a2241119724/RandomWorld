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
    # 模式/思考强度设置（规则生成时提升推理深度）：(打开面板选择器, 展开选择器,
    # 展开方式, 目标选项)。展开方式: "hover"/"click"/""（空=无展开步骤）；目标选项:
    # CSS 选择器或纯文字。空 = 无强度切换（如 kimi 2026-08-23 实测：点 .current-effort
    # 弹模型面板 → hover .effort-current 展开 → 点「进阶」；wenxin 点 .ci-input-mode-button-text
    # 开面板 → 直接点「任务」）。区别于 toggles：toggles 是开关型，这里是级联菜单型。
    effort_selector: tuple = ()
    # 深度思考按钮选择器（CSS）：点开即选中（无 aria-pressed，靠 class 含 selected 标记）。
    # 元宝等平台（2026-08-23 实测：dt-button-id="deep_think"，点后 class 加 ThinkSelector_selected、
    # dt-model-id 切 hunyuan_t1）。区别于 toggles 的 aria-pressed 开关型。空 = 无。
    deep_think_selector: str = ""
    # 该平台规则生成是否走深度思考通道（toggles 深度思考/思考 / deep_think_selector /
    # effort_selector 切强推理目标）。写入规则文件 deep_think 字段，RuleTeacher 投票权重翻倍
    # （用户 2026-08-23 确认：doubao/chatgpt/kimi/yuanbao/deepseek_api 深度思考 ×2，wenxin 任务模式≠深度思考）。
    deep_think: bool = False
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
        deep_think=True,  # 深度思考开关真实存在
        note="已校准（2026-08-22）；2026-08-22 被风控限制（登录可达但发消息失败/降速）→ 全局 delay_sec 已降速，恢复后此平台自动重新可用",
    ),
    "wenxin": Platform(
        name="wenxin", url="https://wenxin.baidu.com/",
        new_chat=("span:has-text('开启新对话')", "button:has-text('新对话')", "span:has-text('新对话')"),
        toggles=(),  # 2026-08-23 改版：新版已移除「深度思考」「联网搜索」开关（改为「快速/任务」输入模式，实测任务=专业技能≠深度思考）；_set_toggle 按文字找不到元素只会静默 no-op，置空避免误导
        effort_selector=(".ci-input-mode-button-text", "", "", ".ci-input-mode-title:has-text('任务')"),  # 2026-08-23 实测：点「快速」胶囊开面板 → 选项直接可见点「任务」模式（专业技能，比快速更充分推理）
        batch_size=32,  # 新格式表格 prompt 长度约减半 → 16→32 翻倍（32 条 ≈ 旧 16 条体量）；仍配 _complete_labels 防截断追问补全
        deep_think=False,  # 任务模式≠深度思考（用户 2026-08-23 确认）
        note="文心一言（已校准 2026-08-22；长输入疑似被输入框截断→batch_size=16，配合追问补全兜底；2026-08-23 确认新版无深度思考开关，规则生成切「任务」输入模式提升推理，靠 DeepSeek API 通道保深度）",
    ),
    "qianwen": Platform(
        name="qianwen", url="https://platform.qianwenai.com/try-ai/chat?models=qwen3.8-max",
        toggles=("深度思考", "联网搜索", "搜索"),
        deep_think=True,  # 深度思考开关真实存在
        batch_size=64,  # 新格式表格 prompt 长度约减半 → 32→64 翻倍（64 条 ≈ 旧 32 条体量）；账号欠费中待处理
        note="通义千问（URL 直接锚定模型 qwen3.8-max，无需手动选择）",
    ),
    "doubao": Platform(
        name="doubao", url="https://www.doubao.com/chat/",
        markdown=("[class*='md-box-root']", "[class*='markdown']"),  # 2026-08-22 校准：全站 CSS Modules hash 类名，兜底 markdown 全落空致回复超时；回复正文为 md-box-root 容器，收发由 data-foundation-type=receive-* 区分
        toggles=(),  # 2026-08-23 确认新版豆包无「深度思考」「联网搜索」开关（旧 toggles 纯属无效猜测）；_set_toggle 按文字找不到只会静默 no-op，置空避免误导
        effort_selector=("[data-valid-btn='model-select-action-btn']", "", "", "[role='menuitem']:has-text('豆包 2.1')"),  # 2026-08-23 实测：模型按钮当前「豆包 快速」，点开菜单（radix dropdown）选「豆包 2.1 Turbo」（带「专家」badge），切后按钮文字真实变化；规则生成切 Turbo 提推理、打标用默认快速
        deep_think=True,  # 用户 2026-08-23 确认：豆包 2.1 Turbo 算深度思考（虽无开关但推理更强），投票权重翻倍
        batch_size=16,  # 新格式表格 prompt 减半后 32 触发人机校验 → 降回 16（已验证零回显）；风控是主要约束，batch 不宜再升
        note="豆包（已校准 2026-08-22：回复容器 md-box-root，data-streaming=false 标记生成完成；旧格式 batch=16 曾偶发回显、8 稳定；新格式 16 零回显；32 触发人机校验 → 定 16；2026-08-23 确认新版无深度思考开关，模型按钮 [data-valid-btn=model-select-action-btn] 从「快速」切「豆包 2.1 Turbo」）",
    ),
    "yuanbao": Platform(
        name="yuanbao", url="https://yuanbao.tencent.com/chat/naQivTmsDa",
        toggles=(),  # 2026-08-23 实测：元宝深度思考按钮无 aria-pressed（_set_toggle 按 aria-pressed 找不到）→ 旧 toggles=("深度思考","联网搜索") 是无效猜测，置空改走 deep_think_selector
        deep_think_selector="[dt-button-id='deep_think']",  # 2026-08-23 实测：点后 class 加 ThinkSelector_selected、dt-model-id 切 hunyuan_t1（深度思考真实生效）
        deep_think=True,  # 深度思考按钮真实存在
        batch_size=64,  # 新格式表格 prompt 长度约减半 → 32→64 翻倍
        note="腾讯元宝（已校准 2026-08-23：回复容器 [class*='markdown']（hyc-common-markdown）命中默认候选；深度思考按钮 [dt-button-id='deep_think'] 无 aria-pressed、点后 class 含 ThinkSelector_selected；输入框 ql-editor contenteditable；思考过程 hyc-component-deepsearch-cot 先渲染、正文 hyc-content-md 后完成）",
    ),
    "kimi": Platform(
        name="kimi", url="https://www.kimi.com/",
        editor=("[contenteditable='true']", "[role='textbox']", "textarea"),
        new_chat=("[aria-label='新建会话']", "button:has-text('新建会话')"),
        toggles=("深度思考", "联网搜索"),
        effort_selector=(".current-effort", ".effort-current", "hover", "进阶"),  # 2026-08-23 实测路径：点强度胶囊开面板 → hover effort-current 展开 → 点「进阶」
        deep_think=True,  # 思考强度「进阶」= 深度推理，用户 2026-08-23 确认权重翻倍
        send_button="[class*='send-button-container']",  # 2026-08-22 校准：发送按钮是 div 容器+SVG（非 button），Enter 未触发发送须点击兜底
        batch_size=64,  # 新格式表格 prompt 长度约减半 → 32→64 翻倍
        note="Kimi（已校准 2026-08-22：Lexical 编辑器 chat-input-editor；发送按钮 send-button-container 为 div 须点击；无独立深度思考开关，思考强度走模型面板级联菜单：点 .current-effort → hover .effort-current → 选强度（快速/标准/进阶），规则生成切「进阶」、打标用账号侧「标准」持久化）",
    ),
    "chatgpt": Platform(
        name="chatgpt", url="https://chatgpt.com/",
        editor=("#prompt-textarea", "[contenteditable='true']", "[role='textbox']"),
        markdown=("[class*='markdown']", "[class*='message-content']"),
        new_chat=("button:has-text('New chat')", "button:has-text('新建聊天')", "a[href='/']"),
        toggles=("思考",),  # 2026-08-23 实测：深度思考开关是「思考」胶囊（__composer-pill，aria-pressed 翻转），非英文 Reasoning；「搜索网页」是模式按钮非开关、默认不启用，不放进 toggles
        deep_think=True,  # 「思考」胶囊 = 深度思考
        send_keys=("Enter",),
        batch_size=64,  # 新格式表格 prompt 长度约减半 → 32→64 翻倍
        note="ChatGPT（已校准 2026-08-22：探活冒烟通过回复 OK；输入框 ProseMirror、回复容器 markdown；2026-08-23 确认深度思考开关=「思考」胶囊 aria-pressed；风控严可能触发验证/登录门槛）",
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
