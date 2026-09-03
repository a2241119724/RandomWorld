namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// 引擎无关的波次规则配置。
    /// </summary>
    public sealed class WaveConfigModel
    {
        public int BaseEnemyCount { get; set; }

        public int EnemiesPerWaveIncrease { get; set; }

        public int MaxAliveEnemies { get; set; }

        public int TotalWaves { get; set; }

        public float DifficultyScalePerWave { get; set; }

        /// <summary>
        /// 新种妖兽（Charge/Shoot）开始混入的波次；之前的波次只用旧池（Common/Seek）。
        /// 默认 3，与 WaveConfig.newEnemyStartWave 一致。
        /// </summary>
        public int NewEnemyStartWave { get; set; } = 3;
    }
}
