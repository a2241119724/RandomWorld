namespace LAB2D.Domain.Wave
{
    /// <summary>
    /// Engine-independent runtime state for the wave loop.
    /// Unity-side managers can mirror this state without owning the wave rules.
    /// </summary>
    public sealed class WaveRuntimeState
    {
        public int CurrentWaveIndex { get; private set; }

        public int EnemiesAliveInWave { get; private set; }

        public int EnemiesDefeatedInWave { get; private set; }

        public int TotalWavesCompleted { get; private set; }

        public int EnemiesAliveBeforeWave { get; private set; }

        public int EnemiesSpawnedThisWave { get; private set; }

        public bool IsWaveActive { get; private set; }

        public bool IsResting { get; private set; }

        public void Reset()
        {
            this.CurrentWaveIndex = 0;
            this.EnemiesAliveInWave = 0;
            this.EnemiesDefeatedInWave = 0;
            this.TotalWavesCompleted = 0;
            this.EnemiesAliveBeforeWave = 0;
            this.EnemiesSpawnedThisWave = 0;
            this.IsWaveActive = false;
            this.IsResting = false;
        }

        public void BeginRest()
        {
            this.IsResting = true;
        }

        public void EndRest()
        {
            this.IsResting = false;
        }

        public void BeginNextWave(int aliveEnemiesBeforeWave)
        {
            this.CurrentWaveIndex++;
            this.EnemiesDefeatedInWave = 0;
            this.EnemiesAliveInWave = 0;
            this.EnemiesSpawnedThisWave = 0;
            this.EnemiesAliveBeforeWave = aliveEnemiesBeforeWave < 0 ? 0 : aliveEnemiesBeforeWave;
            this.IsWaveActive = true;
            this.IsResting = false;
        }

        public void RegisterSpawnSuccess()
        {
            this.EnemiesSpawnedThisWave++;
        }

        public void SyncWaveAliveCountToSpawned()
        {
            this.EnemiesAliveInWave = this.EnemiesSpawnedThisWave;
        }

        public void CompleteCurrentWave()
        {
            this.IsWaveActive = false;
            this.IsResting = false;
            this.EnemiesAliveInWave = 0;
            this.EnemiesDefeatedInWave = this.EnemiesSpawnedThisWave;
            this.TotalWavesCompleted++;
        }

        public void Stop()
        {
            this.IsWaveActive = false;
            this.IsResting = false;
        }

        /// <summary>
        /// 从存档恢复运行时状态（仅 WaveManager 调用）。
        /// </summary>
        public void RestoreFrom(
            int currentWaveIndex,
            int totalWavesCompleted,
            int enemiesAliveBeforeWave,
            int enemiesSpawnedThisWave,
            bool isWaveActive,
            bool isResting)
        {
            this.CurrentWaveIndex = currentWaveIndex;
            this.TotalWavesCompleted = totalWavesCompleted;
            this.EnemiesAliveBeforeWave = enemiesAliveBeforeWave;
            this.EnemiesSpawnedThisWave = enemiesSpawnedThisWave;
            this.IsWaveActive = isWaveActive;
            this.IsResting = isResting;
            this.EnemiesAliveInWave = 0;
            this.EnemiesDefeatedInWave = 0;
        }
    }
}
