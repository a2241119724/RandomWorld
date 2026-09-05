namespace LAB2D.Constant
{
    /// <summary>
    /// 成就系统公共常量
    /// 用途：集中管理成就系统的 UI 节点名、默认文案、路径、阈值和事件名，避免在业务脚本中硬编码。
    /// 修改风险：修改默认值会影响所有成就 UI 表现，需同步检查 AchievementPopup 和 AchievementPanel。
    /// </summary>
    public static class AchievementConstant
    {
        // --- UI 根节点名（挂载到 UI/ 下，复用 UI 的 Canvas） ---

        /// <summary>成就弹窗根节点名称</summary>
        public const string PopupRootName = "AchievementPopup";

        /// <summary>成就面板根节点名称</summary>
        public const string PanelRootName = "AchievementPanel";

        // --- 默认文案 ---

        /// <summary>成就解锁弹窗默认标题</summary>
        public const string DefaultUnlockTitle = "成就解锁！";

        /// <summary>成就面板默认标题</summary>
        public const string DefaultPanelTitle = "成就";

        /// <summary>成就点数单位后缀</summary>
        public const string PointsSuffix = " 点";

        // --- 默认数值 ---

        /// <summary>弹窗自动隐藏延迟（秒）</summary>
        public const float PopupAutoHideDelay = 4.0f;

        /// <summary>弹窗淡入动画时长（秒）</summary>
        public const float PopupFadeInDuration = 0.5f;

        /// <summary>弹窗淡出动画时长（秒）</summary>
        public const float PopupFadeOutDuration = 0.5f;

        // --- 事件名 ---

        // --- 菜单路径 ---

        /// <summary>Editor 菜单根路径</summary>
        public const string EditorMenuRoot = "工具/成就/";
    }
}
