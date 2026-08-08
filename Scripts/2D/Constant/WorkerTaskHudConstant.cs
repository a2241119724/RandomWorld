namespace LAB2D.Constant
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 工人任务队列 HUD 常量。
    /// 集中维护 F015 的刷新节奏、显示热键、菜单路径、节点名、默认文案和压力阈值；修改后需要验证 HUD 展示位置和任务压力文案。
    /// </summary>
    public static class WorkerTaskHudConstant
    {
        /// <summary>
        /// 任务队列 HUD 默认刷新间隔。
        /// 调低会更实时，但会增加任务队列统计和文本拼接频率。
        /// </summary>
        public const float HudRefreshInterval = 0.5f;

        /// <summary>
        /// 任务队列 HUD 默认显示隐藏热键。
        /// 只在没有 UI 输入框聚焦时生效，避免输入穿透。
        /// </summary>
        public const KeyCode HudToggleKey = InputKeyConstant.ToggleWorkerTaskAndAchievementHud;

        /// <summary>
        /// 任务队列 HUD Editor 菜单根路径。
        /// 仅供 Editor 脚本复用，运行时代码不会引用编辑器命名空间。
        /// </summary>
        public const string MenuRoot = "工具/任务队列 HUD/";

        /// <summary>
        /// Editor 自动创建独立 Canvas 时使用的节点名。
        /// </summary>
        public const string HudCanvasName = "WorkerTaskQueueHUDCanvas";

        /// <summary>
        /// 任务队列 HUD 根节点名。
        /// </summary>
        public const string HudRootName = "WorkerTaskQueueHUD";

        /// <summary>
        /// 任务队列 HUD 文本节点名。
        /// WorkerTaskQueueHUD 会按该名称查找 Text 组件。
        /// </summary>
        public const string HudTextName = "WorkerTaskQueueText";

        /// <summary>
        /// HUD 无任务时的默认文案。
        /// 仅用于显示，不影响任务队列。
        /// </summary>
        public const string NoTaskText = "任务队列: 当前没有待处理任务";

        /// <summary>
        /// WorkerTaskManager 尚未初始化时的默认文案。
        /// 通常只会在非 Game 场景或 Play Mode 初始化前出现。
        /// </summary>
        public const string ManagerUnavailableText = "任务队列: WorkerTaskManager 未初始化";

        /// <summary>
        /// HUD 最多展示的任务类型行数。
        /// 当前任务类型较少，保留上限可避免后续扩展后文本溢出。
        /// </summary>
        public const int MaxHudTaskTypeLines = 8;

        /// <summary>
        /// 等待中任务达到该数量时显示为繁忙。
        /// 该阈值只影响 UI 文案，不影响任务调度。
        /// </summary>
        public const int MediumWaitingTaskThreshold = 4;

        /// <summary>
        /// 等待中任务达到该数量时显示为拥堵。
        /// 该阈值只影响 UI 文案，不影响任务调度。
        /// </summary>
        public const int HighWaitingTaskThreshold = 10;

        /// <summary>
        /// HUD 默认宽度。
        /// 调整时需要同步检查与 WorkerConditionHUD/WorkerSupplyHUD 的屏幕占位。
        /// </summary>
        public const float HudWidth = 580.0f;

        /// <summary>
        /// HUD 默认高度。
        /// 调整时需要确认所有任务类型行可以正常显示。
        /// </summary>
        public const float HudHeight = 210.0f;

        /// <summary>
        /// HUD 默认 X 轴锚点偏移。
        /// 采用左上角堆叠位置，便于和其他 Worker 类 HUD 组合查看。
        /// </summary>
        public const float HudAnchoredX = 20.0f;

        /// <summary>
        /// HUD 默认 Y 轴锚点偏移。
        /// 默认放在 WorkerConditionHUD/WorkerSupplyHUD 下方，减少覆盖已有 HUD 的概率。
        /// </summary>
        public const float HudAnchoredY = -560.0f;

        /// <summary>
        /// HUD 文本横向内边距。
        /// </summary>
        public const float HudPaddingX = 12.0f;

        /// <summary>
        /// HUD 文本纵向内边距。
        /// </summary>
        public const float HudPaddingY = 8.0f;

        /// <summary>
        /// HUD 默认字号。
        /// </summary>
        public const int HudFontSize = 15;
    }
}
