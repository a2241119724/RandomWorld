namespace LAB2D.Constant
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 工人补给缺口提示常量。
    /// 集中维护 F014 的刷新节奏、Tip 冷却、HUD 节点名、菜单路径和默认文案；修改数值会影响玩家看到补给提示的频率和 UI 接入位置。
    /// </summary>
    public static class WorkerSupplyConstant
    {
        /// <summary>
        /// 补给缺口扫描间隔，避免每帧遍历工人、仓库和床位数据。
        /// 调低会更实时，但会增加运行时字符串拼接和只读统计频率。
        /// </summary>
        public const float MonitorRefreshInterval = 1.0f;

        /// <summary>
        /// 补给缺口 Tip 冷却时间，避免低补给状态持续存在时刷屏。
        /// </summary>
        public const float TipCooldownSeconds = 12.0f;

        /// <summary>
        /// 每份食物默认恢复的饥饿值。
        /// 该值与当前吃饭任务的恢复规则保持一致，仅用于估算缺口，不会扣减仓库资源。
        /// </summary>
        public const int FoodRecoverValuePerItem = 10;

        /// <summary>
        /// HUD 默认显示隐藏热键。
        /// 只在没有 UI 输入框聚焦时生效，避免输入穿透。
        /// </summary>
        public const KeyCode HudToggleKey = InputKeyConstant.ToggleWorkerSupplyHud;

        /// <summary>
        /// 工人补给提示 Editor 菜单根路径。
        /// 仅供 Editor 脚本复用，运行时代码不会引用 UnityEditor。
        /// </summary>
        public const string MenuRoot = "工具/工人补给提示/";

        /// <summary>
        /// Editor 自动创建独立 Canvas 时使用的节点名。
        /// </summary>
        public const string HudCanvasName = "WorkerSupplyHUDCanvas";

        /// <summary>
        /// 工人补给缺口 HUD 根节点名。
        /// </summary>
        public const string HudRootName = "WorkerSupplyHUDRoot";

        /// <summary>
        /// 工人补给缺口 HUD 文本节点名。
        /// WorkerSupplyHUD 会按该名称查找 Text 组件。
        /// </summary>
        public const string HudTextName = "WorkerSupplyText";

        /// <summary>
        /// HUD 最多展示的单个工人问题行数。
        /// 调大可显示更多细节，但需要同步调整 HUD 高度。
        /// </summary>
        public const int MaxHudIssueLines = 5;

        /// <summary>
        /// 无 Worker 数据时的默认 HUD 文案。
        /// 仅用于显示，不影响任何业务状态。
        /// </summary>
        public const string EmptyHudText = "工人补给: 暂无可检查工人";

        /// <summary>
        /// 未发现明显补给缺口时的默认 HUD 文案。
        /// </summary>
        public const string NoIssueText = "工人补给: 食物与床位暂无明显缺口";
    }
}
