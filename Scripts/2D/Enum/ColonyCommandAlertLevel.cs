namespace LAB2D.Enum
{
    /// <summary>
    /// 殖民地运营指挥中心警戒等级。
    /// 用于统一表达 Worker 人力、补给、任务拥堵和阻塞诊断聚合后的整体状态，可被 HUD、Tip、Editor 菜单和后续运营事件复用。
    /// 后续允许追加更细等级，但不得删除或重命名已有值，避免破坏 UI 展示与报告签名。
    /// </summary>
    public enum ColonyCommandAlertLevel
    {
        /// <summary>
        /// 稳定：暂无明显运营问题，任务与补给处于健康状态。
        /// </summary>
        Stable,

        /// <summary>
        /// 关注：存在少量等待任务或轻微状态波动，建议玩家留意但不需要立即处理。
        /// </summary>
        Notice,

        /// <summary>
        /// 警告：存在补给缺口、任务阻塞或明显拥堵，需要玩家调整殖民地运营。
        /// </summary>
        Warning,

        /// <summary>
        /// 危急：存在临界工人、严重拥堵或大量阻塞任务，应优先处理。
        /// </summary>
        Critical,
    }
}
