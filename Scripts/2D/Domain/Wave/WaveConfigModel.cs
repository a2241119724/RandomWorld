namespace LAB2D
{
    /// <summary>
    /// Engine-agnostic wave rule configuration.
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
