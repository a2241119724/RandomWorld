namespace LAB2D.Enum
{
    /// <summary>
    /// 工人任务队列拥堵等级。
    /// 用于 Tip、HUD、Editor 菜单和后续任务目标提示判断；允许后续只追加更细等级，不应删除或重命名已有值。
    /// </summary>
    public enum WorkerTaskCongestionLevel
    {
        /// <summary>
        /// 未获取到任务队列或当前没有可判断数据。
        /// </summary>
        None = 0,

        /// <summary>
        /// 等待任务较少，队列处于平稳状态。
        /// </summary>
        Smooth = 1,

        /// <summary>
        /// 等待任务已达到繁忙阈值，适合在摘要中给出建议。
        /// </summary>
        Busy = 2,

        /// <summary>
        /// 等待任务已达到拥堵阈值，适合触发玩家 Tip。
        /// </summary>
        Congested = 3,

        /// <summary>
        /// 等待任务严重积压，应优先提示玩家暂停新增任务。
        /// </summary>
        Critical = 4,
    }
}
