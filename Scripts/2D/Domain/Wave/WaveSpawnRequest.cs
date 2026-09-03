namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// Engine-independent description of one enemy spawn in a wave.
    /// Unity adapters decide where and how the enemy prefab is created.
    /// </summary>
    public sealed class WaveSpawnRequest
    {
        public int WaveIndex { get; set; }

        public int SpawnIndex { get; set; }

        public int TotalEnemiesInWave { get; set; }

        public float DifficultyScale { get; set; }

        /// <summary>
        /// 敌人种类 Id（<see cref="WaveEnemyKind"/>）。0=Common 缺省，旧调用方不填即旧行为。
        /// </summary>
        public int EnemyKindId { get; set; }
    }
}
