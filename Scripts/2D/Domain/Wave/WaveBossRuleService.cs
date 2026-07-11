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
    }
}
