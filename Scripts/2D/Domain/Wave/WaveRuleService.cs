namespace LAB2D
{
    /// <summary>
    /// Pure wave progression and spawn count rules.
    /// </summary>
    public sealed class WaveRuleService
    {
        public float GetDifficultyScale(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return 1.0f + (ClampMin(totalWavesCompleted, 0) * safeConfig.DifficultyScalePerWave);
        }

        public bool AreAllWavesCleared(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return safeConfig.TotalWaves > 0 && totalWavesCompleted >= safeConfig.TotalWaves;
        }

        public int GetEnemyCountForWave(int waveIndex, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            int normalizedWaveIndex = ClampMin(waveIndex, 1);
            int count = safeConfig.BaseEnemyCount + ((normalizedWaveIndex - 1) * safeConfig.EnemiesPerWaveIncrease);
            return ClampMin(count, 1);
        }

        public int GetEffectiveMaxAliveEnemies(int configMaxAliveEnemies, int runtimeMaxEnemyCount)
        {
            int maxAliveEnemies = configMaxAliveEnemies;
            if (runtimeMaxEnemyCount > 0)
            {
                maxAliveEnemies = ClampMax(maxAliveEnemies, runtimeMaxEnemyCount);
            }

            return ClampMin(maxAliveEnemies, 1);
        }

        public bool IsWaveCleared(int enemiesSpawnedThisWave, int currentAliveEnemies, int aliveEnemiesBeforeWave)
        {
            return enemiesSpawnedThisWave > 0 && currentAliveEnemies <= aliveEnemiesBeforeWave;
        }

        private static int ClampMin(int value, int min)
        {
            return value < min ? min : value;
        }

        private static int ClampMax(int value, int max)
        {
            return value > max ? max : value;
        }
    }
}
