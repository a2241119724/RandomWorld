namespace LAB2D
{
    /// <summary>
    /// 成就类别枚举
    /// 用途：分类成就条目，供成就面板筛选、成就弹窗图标选择和后续扩展使用。
    /// 使用场景：AchievementData 绑定类别，AchievementPanel 按类别筛选，AchievementPopup 按类别选择默认图标。
    /// 允许扩展：可追加新类别值，不得删除或重命名已有值。
    /// </summary>
    public enum AchievementCategory
    {
        /// <summary>战斗类成就（击杀、连击、暴击、Boss）</summary>
        Combat,

        /// <summary>收集类成就（物品拾取、稀有度收集）</summary>
        Collection,

        /// <summary>生存类成就（等级、死亡、存活时间）</summary>
        Survival,

        /// <summary>波次类成就（波次通关、完美波次）</summary>
        Wave,

        /// <summary>工人运营类成就（任务完成、殖民地规模）</summary>
        Worker,
    }
}
