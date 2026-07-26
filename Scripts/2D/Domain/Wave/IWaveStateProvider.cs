namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// 波次状态只读提供者接口。
    /// 允许 WaveEventFeedback 等消费者通过接口读取波次运行时状态，而非直接依赖 WaveManager。
    /// 默认实现由 WaveManager 提供，测试中可替换为桩。
    /// </summary>
    public interface IWaveStateProvider
    {
        /// <summary>当前波次索引（从 1 开始，0 表示未开始）</summary>
        int CurrentWaveIndex { get; }

        /// <summary>已完成的波次总数</summary>
        int TotalWavesCompleted { get; }

        /// <summary>当前波次中存活的敌人数量</summary>
        int EnemiesAliveInWave { get; }

        /// <summary>当前波次中已击杀的敌人数量</summary>
        int EnemiesDefeatedInWave { get; }

        /// <summary>是否正在波次战斗中</summary>
        bool IsWaveActive { get; }

        /// <summary>是否在波间休息中</summary>
        bool IsResting { get; }

        /// <summary>当前难度缩放因子</summary>
        float CurrentDifficultyScale { get; }
    }
}
