namespace LAB2D.Constant
{
    using LAB2D;
    using LAB2D.Domain.Worker;
    using UnityEngine;

    /// <summary>
    /// 工人饥饿与疲劳状态常量。
    /// 这些值会影响 Worker 移动速度、普通任务进度、HUD 文案和 Editor 菜单路径；
    /// 修改时需要同步验证工人接任务、吃饭、睡觉和 HUD 展示手感。
    /// 规则相关值现已委托 WorkerConditionRuleService 维护；此常量类保留向后兼容引用。
    /// </summary>
    public static class WorkerConditionConstant
    {
        /// <summary>
        /// 饥饿值自然衰减速度，沿用原 GlobalInit 中的默认表现并集中维护。
        /// 调大该值会让工人更频繁进入吃饭任务。
        /// </summary>
        /// <summary>饥饿值自然衰减速度（约67分钟归零，一天约消耗45点，需2次进食）。</summary>
        public const float HungryDecayPerSecond = 0.025f;

        /// <summary>
        /// 疲劳值自然累积速度，沿用原 GlobalInit 中的默认表现并集中维护。
        /// 调大该值会让工人更频繁进入睡觉任务。
        /// </summary>
        /// <summary>疲劳值自然累积速度（一天约累积27点基础疲劳，配合工作消耗约半天触发睡眠）。</summary>
        public const float TiredDecayPerSecond = 0.015f;

        // ---- 精气神 (Spirit) 常量 ----

        /// <summary>精气神基础衰减速度（约111分钟归零）。</summary>
        public const float SpiritDecayPerSecond = 0.015f;

        /// <summary>工作时精气神额外衰减速度（工作总计0.04/s）。</summary>
        public const float SpiritWorkDecayPerSecond = 0.025f;

        /// <summary>漫游时每秒精气神恢复量（4路点×6s×0.5=12点，有意义的精神补充）。</summary>
        public const float SpiritWanderRestorePerSecond = 0.50f;

        /// <summary>有床睡眠额外恢复的精气神。</summary>
        public const float SpiritSleepRestoreBonus = 30f;

        /// <summary>地面睡眠恢复的精气神（缩小与有床睡眠的差距）。</summary>
        public const float SpiritSleepRestoreOnGround = 20f;

        /// <summary>地面睡眠疲劳降低比例（床=100%清零，地面降65%，减少两极分化）。</summary>
        public const float GroundSleepTiredRestoreRatio = 0.65f;

        /// <summary>每个食物恢复的饥饿值（任务吃和自吃统一口径）。</summary>
        public const float HungryRestorePerFood = 25f;

        /// <summary>精气神低阈值。</summary>
        public const float SpiritLowThreshold = 30f;

        /// <summary>
        /// 进入饥饿或疲劳提示的比例阈值。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float WarningRatio = WorkerConditionRuleService.WarningRatio;

        /// <summary>
        /// 进入濒临停工状态的比例阈值。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float CriticalRatio = WorkerConditionRuleService.CriticalRatio;

        /// <summary>
        /// 单项饥饿时的移动速度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float HungryMoveSpeedMultiplier = WorkerConditionRuleService.HungryMoveSpeedMultiplier;

        /// <summary>
        /// 单项疲劳时的移动速度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float TiredMoveSpeedMultiplier = WorkerConditionRuleService.TiredMoveSpeedMultiplier;

        /// <summary>
        /// 饥饿且疲劳时的移动速度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float ExhaustedMoveSpeedMultiplier = WorkerConditionRuleService.ExhaustedMoveSpeedMultiplier;

        /// <summary>
        /// 濒临停工时的移动速度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float CriticalMoveSpeedMultiplier = WorkerConditionRuleService.CriticalMoveSpeedMultiplier;

        /// <summary>
        /// 单项饥饿时的普通任务进度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float HungryWorkProgressMultiplier = WorkerConditionRuleService.HungryWorkProgressMultiplier;

        /// <summary>
        /// 单项疲劳时的普通任务进度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float TiredWorkProgressMultiplier = WorkerConditionRuleService.TiredWorkProgressMultiplier;

        /// <summary>
        /// 饥饿且疲劳时的普通任务进度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float ExhaustedWorkProgressMultiplier = WorkerConditionRuleService.ExhaustedWorkProgressMultiplier;

        /// <summary>
        /// 濒临停工时的普通任务进度倍率。委托自 WorkerConditionRuleService。
        /// </summary>
        public const float CriticalWorkProgressMultiplier = WorkerConditionRuleService.CriticalWorkProgressMultiplier;

        /// <summary>
        /// 状态提示冷却时间，避免每帧或频繁状态刷新刷屏。
        /// </summary>
        public const float TipCooldownSeconds = 10.0f;

        /// <summary>
        /// 工人状态 HUD 默认刷新间隔。
        /// 调低会更实时，但会增加文本拼接频率。
        /// </summary>
        public const float HudRefreshInterval = 0.5f;

        /// <summary>
        /// 工人状态 HUD 默认显示隐藏热键。
        /// 该热键只在没有 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode HudToggleKey = InputKeyConstant.ToggleWorkerConditionHud;

        /// <summary>
        /// 工人状态 Editor 菜单根路径。
        /// 仅供 Editor 菜单脚本复用，运行时代码不会引用 UnityEditor。
        /// </summary>
        public const string MenuRoot = "工具/工人状态/";

        /// <summary>
        /// Game 场景查找名称。
        /// Editor 菜单通过该名称定位真实 Game.unity 路径。
        /// </summary>
        public const string GameSceneName = "Game";

        /// <summary>
        /// Editor 自动创建独立 Canvas 时使用的节点名。
        /// </summary>
        public const string HudCanvasName = "WorkerConditionHUDCanvas";

        /// <summary>
        /// 工人状态 HUD 根节点名。
        /// </summary>
        public const string HudRootName = "WorkerConditionHUD";

        /// <summary>
        /// 工人状态 HUD 文本节点名。
        /// WorkerConditionHUD 会按该名称查找 Text 组件。
        /// </summary>
        public const string HudTextName = "WorkerConditionText";

        /// <summary>
        /// 项目像素中文字体 Resources 路径。
        /// 若加载失败，Editor 菜单会保留 Unity 默认字体。
        /// </summary>
        public const string FontResourcePath = "Font/ark-pixel-12px-monospaced-zh_cn";

        /// <summary>
        /// HUD 无数据时的默认文案。
        /// 仅用于运行时展示，不影响任何业务状态。
        /// </summary>
        public const string EmptyHudText = "工人状态: 暂无可显示工人";
    }
}
