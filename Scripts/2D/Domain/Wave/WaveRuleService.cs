namespace LAB2D.Domain.Wave
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 纯波次推进和生成数量规则。
    /// </summary>
    public sealed class WaveRuleService
    {
        public float GetDifficultyScale(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return 1.0f + (MathHelper.ClampMin(totalWavesCompleted, 0) * safeConfig.DifficultyScalePerWave);
        }

        public bool AreAllWavesCleared(int totalWavesCompleted, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            return safeConfig.TotalWaves > 0 && totalWavesCompleted >= safeConfig.TotalWaves;
        }

        public int GetEnemyCountForWave(int waveIndex, WaveConfigModel config)
        {
            WaveConfigModel safeConfig = config ?? new WaveConfigModel();
            int normalizedWaveIndex = MathHelper.ClampMin(waveIndex, 1);
            int count = safeConfig.BaseEnemyCount + ((normalizedWaveIndex - 1) * safeConfig.EnemiesPerWaveIncrease);
            return MathHelper.ClampMin(count, 1);
        }

        public int GetEffectiveMaxAliveEnemies(int configMaxAliveEnemies, int runtimeMaxEnemyCount)
        {
            int maxAliveEnemies = configMaxAliveEnemies;
            if (runtimeMaxEnemyCount > 0)
            {
                maxAliveEnemies = MathHelper.ClampMax(maxAliveEnemies, runtimeMaxEnemyCount);
            }

            return MathHelper.ClampMin(maxAliveEnemies, 1);
        }

        public bool IsWaveCleared(int enemiesSpawnedThisWave, int currentAliveEnemies, int aliveEnemiesBeforeWave)
        {
            return enemiesSpawnedThisWave > 0 && currentAliveEnemies <= aliveEnemiesBeforeWave;
        }

        public float GetRemainingRestTime(float restDuration, float elapsed)
        {
            float remaining = restDuration - elapsed;
            return remaining < 0.0f ? 0.0f : remaining;
        }

    }
}
