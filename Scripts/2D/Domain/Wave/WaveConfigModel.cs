namespace LAB2D
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
    }
}
