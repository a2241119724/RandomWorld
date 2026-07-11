namespace LAB2D
{
    /// <summary>
    /// Pure rules for boss-wave cadence and enemy count adjustment.
    /// </summary>
    public sealed class WaveBossRuleService
    {
        public bool IsBossWave(int waveIndex, int bossWaveInterval)
        {
            return waveIndex > 0 && bossWaveInterval > 0 && waveIndex % bossWaveInterval == 0;
        }

        public int GetEnemyCountForWave(
            int baseEnemyCount,
            int waveIndex,
            int bossWaveInterval,
            int bossGuardianExtraEnemyCount)
        {
            int count = baseEnemyCount < 1 ? 1 : baseEnemyCount;
            if (this.IsBossWave(waveIndex, bossWaveInterval))
            {
                count += bossGuardianExtraEnemyCount < 0 ? 0 : bossGuardianExtraEnemyCount;
            }

            return count;
        }

        public float GetNormalEnemyHealthMultiplier(
            int waveIndex,
            float difficultyScale,
            float healthScalePerWave)
        {
            return ClampMin(1.0f, difficultyScale + ((waveIndex - 1) * healthScalePerWave));
        }

        public float GetNormalEnemyAttackMultiplier(
            int waveIndex,
            float difficultyScale,
            float attackScalePerWave)
        {
            return ClampMin(1.0f, difficultyScale + ((waveIndex - 1) * attackScalePerWave));
        }

        public float GetNormalEnemyDefenseMultiplier(int waveIndex, float defenseScalePerWave)
        {
            return ClampMin(1.0f, 1.0f + ((waveIndex - 1) * defenseScalePerWave));
        }

        private static float ClampMin(float min, float value)
        {
            return value < min ? min : value;
        }
    }
}
