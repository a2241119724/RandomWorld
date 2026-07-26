namespace LAB2D.Constant
{
    /// <summary>
    /// 成就系统公共常量
    /// 用途：集中管理成就系统的 UI 节点名、默认文案、路径、阈值和事件名，避免在业务脚本中硬编码。
    /// 修改风险：修改默认值会影响所有成就 UI 表现，需同步检查 AchievementPopup 和 AchievementPanel。
    /// </summary>
    public static class AchievementConstant
    {
        // --- UI 根节点名（挂载到 UI/Foreground 下，复用 UI 的 Canvas） ---

        /// <summary>成就弹窗根节点名称</summary>
        public const string PopupRootName = "AchievementPopupRoot";

        /// <summary>成就面板根节点名称</summary>
        public const string PanelRootName = "AchievementPanelRoot";

        // --- 默认文案 ---

        /// <summary>成就解锁弹窗默认标题</summary>
        public const string DefaultUnlockTitle = "成就解锁！";

        /// <summary>成就面板默认标题</summary>
        public const string DefaultPanelTitle = "成就";

        /// <summary>无成就达成时的提示文本</summary>
        public const string NoAchievementText = "暂无成就记录";

        /// <summary>成就点数单位后缀</summary>
        public const string PointsSuffix = " 点";

        /// <summary>显示/隐藏成就面板的快捷键名</summary>
        /// <summary>显示/隐藏成就面板的快捷键名（与工人任务队列 HUD 共用 F7）</summary>
        public const string TogglePanelKeyName = "F7";

        // --- 默认数值 ---

        /// <summary>弹窗自动隐藏延迟（秒）</summary>
        public const float PopupAutoHideDelay = 4.0f;

        /// <summary>弹窗淡入动画时长（秒）</summary>
        public const float PopupFadeInDuration = 0.5f;

        /// <summary>弹窗淡出动画时长（秒）</summary>
        public const float PopupFadeOutDuration = 0.5f;

        /// <summary>存档 Key 前缀，格式：Achievement_{id}</summary>
        public const string SaveKeyPrefix = "Achievement_";

        /// <summary>成就面板默认宽度</summary>
        public const float PanelDefaultWidth = 1800f;

        /// <summary>成就面板默认高度</summary>
        public const float PanelDefaultHeight = 1320f;

        /// <summary>默认成就点数（每个成就）</summary>
        public const int DefaultAchievementPoints = 10;

        // --- 事件名 ---

        /// <summary>成就解锁事件名（用于事件分发）</summary>
        public const string AchievementUnlockedEvent = "AchievementUnlocked";

        /// <summary>成就进度更新事件名</summary>
        public const string AchievementProgressEvent = "AchievementProgress";

        // --- 菜单路径 ---

        /// <summary>Editor 菜单根路径</summary>
        public const string EditorMenuRoot = "工具/智能体/成就系统/";

        /// <summary>安装成就系统到 Game 场景菜单路径</summary>
        public const string EditorMenuInstallToGame = "工具/智能体/成就系统/安装成就系统到 Game 场景";

        /// <summary>从 Game 场景移除成就系统菜单路径</summary>
        public const string EditorMenuRemoveFromGame = "工具/智能体/成就系统/从 Game 场景移除成就系统";

        /// <summary>创建成就面板 UI Prefab 到 ResourcesLocal 菜单路径</summary>
        public const string EditorMenuCreatePrefab = "工具/智能体/成就系统/创建成就面板 Prefab 到 ResourcesLocal";
    }
}
