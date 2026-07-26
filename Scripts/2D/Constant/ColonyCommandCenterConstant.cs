namespace LAB2D.Constant
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// A006 殖民地运营指挥中心常量。
    /// 集中维护菜单路径、节点名、Prefab 路径、刷新节奏、Tip 冷却、热键、UI 尺寸和默认文案；修改后需要验证 HUD 布局、Tip 频率和 Editor 菜单生成结果。
    /// </summary>
    public static class ColonyCommandCenterConstant
    {
        /// <summary>
        /// 候选编号，用于报告、日志和节点命名。
        /// </summary>
        public const string CandidateId = "A006";

        /// <summary>
        /// Editor 菜单根路径。
        /// 仅供 Editor 脚本复用，运行时代码不会引用 UnityEditor。
        /// </summary>
        public const string MenuRoot = "工具/智能体/A006 殖民地指挥中心/";

        /// <summary>
        /// Game 场景查找名称。
        /// Editor 菜单通过该名称定位真实 Game.unity 路径。
        /// </summary>
        public const string GameSceneName = "Game";

        /// <summary>
        /// 旧版独立 Canvas 名称（仅用于回滚时清理旧场景残留）。
        /// 新版 HUD 挂载在 UIRoot/Foreground 下复用已有 UI Canvas，不再创建独立 Canvas。
        /// </summary>
        public const string CanvasName = "ColonyCommandCenterHUDCanvas";

        /// <summary>
        /// 指挥中心 HUD 根节点名。
        /// 挂载在 UIRoot/Foreground 下，复用已有 UI Canvas。
        /// </summary>
        public const string HudRootName = "ColonyCommandCenterHUDRoot";

        /// <summary>
        /// HUD 背景节点名。
        /// </summary>
        public const string BackgroundName = "CommandBackground";

        /// <summary>
        /// HUD 标题文本节点名。
        /// </summary>
        public const string TitleTextName = "CommandTitleText";

        /// <summary>
        /// HUD 主摘要文本节点名。
        /// </summary>
        public const string MainTextName = "CommandMainText";

        /// <summary>
        /// HUD 细节文本节点名。
        /// </summary>
        public const string DetailTextName = "CommandDetailText";

        /// <summary>
        /// ResourcesLocal UI Prefab 输出目录。
        /// 由 Editor 菜单创建；运行时代码不会主动写入资源。
        /// </summary>
        public const string PrefabFolderPath = "Assets/ResourcesLocal/Prefabs/UI/ColonyCommandCenter";

        /// <summary>
        /// ResourcesLocal UI Prefab 输出路径。
        /// 由 Editor 菜单创建；若已存在会覆盖同名 Prefab，需要在菜单执行前确认。
        /// </summary>
        public const string PrefabAssetPath = PrefabFolderPath + "/ColonyCommandCenterHUD.prefab";

        /// <summary>
        /// 指挥中心报告刷新间隔。
        /// 调低会更实时，但会增加任务队列和 Worker 状态只读统计频率。
        /// </summary>
        public const float RefreshInterval = 1.0f;

        /// <summary>
        /// 指挥中心 Tip 冷却时间。
        /// 避免持续补给缺口或任务阻塞时反复刷屏。
        /// </summary>
        public const float TipCooldownSeconds = 20.0f;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// 只在没有 UI 输入框聚焦时生效，避免输入穿透。
        /// </summary>
        public const KeyCode HudToggleKey = InputKeyConstant.ToggleColonyCommandCenterHud;

        /// <summary>
        /// 阻塞任务达到该数量时视为警告。
        /// 只影响指挥中心等级和文案，不改变任务调度。
        /// </summary>
        public const int WarningBlockedTaskThreshold = 2;

        /// <summary>
        /// 阻塞任务达到该数量时视为危急。
        /// 只影响指挥中心等级和文案，不改变任务调度。
        /// </summary>
        public const int CriticalBlockedTaskThreshold = 6;

        /// <summary>
        /// HUD 默认宽度。
        /// 调整时需要同步验证右上角布局是否遮挡已有 Foreground UI。
        /// </summary>
        public const float HudWidth = 500.0f;

        /// <summary>
        /// HUD 默认高度。
        /// 调整时需要确认主摘要与细节文本不会溢出。
        /// </summary>
        public const float HudHeight = 330.0f;

        /// <summary>
        /// HUD 默认右上角 X 偏移。
        /// </summary>
        public const float HudAnchoredX = -24.0f;

        /// <summary>
        /// HUD 默认右上角 Y 偏移。
        /// </summary>
        public const float HudAnchoredY = -120.0f;

        /// <summary>
        /// HUD 标题字号。
        /// </summary>
        public const int TitleFontSize = 18;

        /// <summary>
        /// HUD 主文本字号。
        /// </summary>
        public const int MainFontSize = 15;

        /// <summary>
        /// HUD 细节文本字号。
        /// </summary>
        public const int DetailFontSize = 14;

        /// <summary>
        /// HUD 文本内边距。
        /// </summary>
        public const float Padding = 14.0f;

        /// <summary>
        /// 指挥中心无数据时的默认文案。
        /// 仅用于显示，不影响任务系统。
        /// </summary>
        public const string EmptyText = "殖民地指挥中心: 暂无可显示数据";

        /// <summary>
        /// WorkerTaskManager 尚未初始化时的默认文案。
        /// </summary>
        public const string ManagerUnavailableText = "殖民地指挥中心: WorkerTaskManager 未初始化";

        /// <summary>
        /// 指挥中心调试日志前缀。
        /// </summary>
        public const string LogPrefix = "[ColonyCommandCenter]";
    }
}
