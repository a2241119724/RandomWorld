namespace LAB2D.Domain.Time
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 昼夜相位变化时触发。
    /// 由 GameTimeManager 发布：黄昏预警、夜晚波次联动等消费方订阅此事件。
    /// </summary>
    public sealed class GamePhaseChangedEvent : IGameEvent
    {
        /// <summary>变化前的相位。</summary>
        public GamePhase OldPhase;

        /// <summary>变化后的相位（当前相位）。</summary>
        public GamePhase NewPhase;

        /// <summary>变化发生时的游戏天索引（从 0 开始）。</summary>
        public int DayIndex;
    }

    /// <summary>
    /// 跨天时触发（新一天开始）。
    /// 由 GameTimeManager 发布：每日随机天气等"按天一次"的消费方订阅此事件。
    /// </summary>
    public sealed class GameDayChangedEvent : IGameEvent
    {
        /// <summary>新一天的天索引（从 1 开始，即刚跨入的天）。</summary>
        public int DayIndex;
    }
}
