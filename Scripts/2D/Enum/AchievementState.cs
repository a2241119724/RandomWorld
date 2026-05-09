namespace LAB2D
{
    /// <summary>
    /// 成就状态枚举
    /// 用途：标识单个成就的解锁与领取状态，供 AchievementManager、AchievementPanel 和 AchievementPopup 使用。
    /// 使用场景：成就列表展示、弹窗触发判断、存档中成就状态序列化。
    /// 允许扩展：可追加新状态值，不得删除或重命名已有值。
    /// </summary>
    public enum AchievementState
    {
        /// <summary>未解锁 — 条件尚未达成</summary>
        Locked,

        /// <summary>已解锁 — 条件达成但未查看/未领取奖励</summary>
        Unlocked,

        /// <summary>已领取 — 玩家已查看并领取成就奖励（若有点数奖励）</summary>
        Claimed,
    }
}
