namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 工人饥饿与疲劳状态常量。
    /// 这些值会影响 Worker 移动速度、普通任务进度、HUD 文案和 Editor 菜单路径；
    /// 修改时需要同步验证工人接任务、吃饭、睡觉和 HUD 展示手感。
    /// </summary>
    public static class WorkerConditionConstant
    {
        /// <summary>
        /// 饥饿值自然衰减速度，沿用原 GlobalInit 中的默认表现并集中维护。
        /// 调大该值会让工人更频繁进入吃饭任务。
        /// </summary>
        public const float HungryDecayPerSecond = 0.1f;

        /// <summary>
        /// 疲劳值自然衰减速度，沿用原 GlobalInit 中的默认表现并集中维护。
        /// 调大该值会让工人更频繁进入睡觉任务。
        /// </summary>
        public const float TiredDecayPerSecond = 0.01f;

        /// <summary>
        /// 进入饥饿或疲劳提示的比例阈值。
        /// 当前值表示低于最大值的 35% 时开始显示状态异常并套用轻度惩罚。
        /// </summary>
        public const float WarningRatio = 0.35f;

        /// <summary>
        /// 进入濒临停工状态的比例阈值。
        /// 当前值表示低于最大值的 5% 时套用最强的非致命效率惩罚。
        /// </summary>
        public const float CriticalRatio = 0.05f;

        /// <summary>
        /// 单项饥饿时的移动速度倍率。
        /// 数值低于 1 会减慢工人移动，不直接改变寻路路径。
        /// </summary>
        public const float HungryMoveSpeedMultiplier = 0.86f;

        /// <summary>
        /// 单项疲劳时的移动速度倍率。
        /// 数值低于 1 会减慢工人移动，不直接改变寻路路径。
        /// </summary>
        public const float TiredMoveSpeedMultiplier = 0.9f;

        /// <summary>
        /// 饥饿且疲劳时的移动速度倍率。
        /// 这是复合异常状态的惩罚，避免工人在低状态下仍保持满效率。
        /// </summary>
        public const float ExhaustedMoveSpeedMultiplier = 0.72f;

        /// <summary>
        /// 濒临停工时的移动速度倍率。
        /// 不设为 0，避免工人无法走向食物或床位而卡死。
        /// </summary>
        public const float CriticalMoveSpeedMultiplier = 0.58f;

        /// <summary>
        /// 单项饥饿时的普通任务进度倍率。
        /// 吃饭和睡觉任务不会使用该惩罚。
        /// </summary>
        public const float HungryWorkProgressMultiplier = 0.82f;

        /// <summary>
        /// 单项疲劳时的普通任务进度倍率。
        /// 吃饭和睡觉任务不会使用该惩罚。
        /// </summary>
        public const float TiredWorkProgressMultiplier = 0.76f;

        /// <summary>
        /// 饥饿且疲劳时的普通任务进度倍率。
        /// 用于强化生存管理的玩家反馈。
        /// </summary>
        public const float ExhaustedWorkProgressMultiplier = 0.6f;

        /// <summary>
        /// 濒临停工时的普通任务进度倍率。
        /// 保留少量进度，降低任务永远无法完成的风险。
        /// </summary>
        public const float CriticalWorkProgressMultiplier = 0.45f;

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
        /// 带 F013 前缀便于查找和回滚。
        /// </summary>
        public const string HudCanvasName = "Feature_F013_WorkerCondition_Canvas";

        /// <summary>
        /// 工人状态 HUD 根节点名。
        /// 带 F013 前缀避免与已有 UI 节点冲突。
        /// </summary>
        public const string HudRootName = "Feature_F013_WorkerConditionHUD_Root";

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
