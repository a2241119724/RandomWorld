namespace LAB2D.Domain.Wave
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 波次开始时触发。
    /// 由 WaveManager 发布，UI/表现层可订阅更新波次 HUD。
    /// </summary>
    public sealed class WaveStartedEvent : IGameEvent
    {
        /// <summary>当前波次索引（从 1 开始）</summary>
        public int WaveIndex;

        /// <summary>当前难度缩放因子</summary>
        public float DifficultyScale;
    }

    /// <summary>
    /// 波次结束时触发。
    /// 由 WaveManager 发布，UI/表现层可订阅显示波次结算。
    /// </summary>
    public sealed class WaveEndedEvent : IGameEvent
    {
        /// <summary>刚结束的波次索引</summary>
        public int WaveIndex;

        /// <summary>已完成波次总数</summary>
        public int TotalWavesCompleted;
    }

    /// <summary>
    /// 全部波次完成时触发。
    /// 由 WaveManager 发布，仅在设置了 totalWaves > 0 时可能触发。
    /// </summary>
    public sealed class AllWavesClearedEvent : IGameEvent
    {
        /// <summary>总完成波次数</summary>
        public int TotalWavesCompleted;
    }

    /// <summary>
    /// 波间休息开始时触发。
    /// 由 WaveManager 发布，UI 可订阅显示倒计时。
    /// </summary>
    public sealed class WaveRestStartedEvent : IGameEvent
    {
        /// <summary>休息时长（秒）</summary>
        public float RestDuration;
    }
}
