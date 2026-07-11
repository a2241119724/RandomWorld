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

        public float GetBossHealthMultiplier(float normalHealthMultiplier, float bossHealthMultiplier)
        {
            return normalHealthMultiplier * bossHealthMultiplier;
        }

        public float GetBossAttackMultiplier(float normalAttackMultiplier, float bossAttackMultiplier)
        {
            return normalAttackMultiplier * bossAttackMultiplier;
        }

        public float GetBossDefenseMultiplier(float normalDefenseMultiplier, float bossDefenseMultiplier)
        {
            return normalDefenseMultiplier * bossDefenseMultiplier;
        }

        public float GetRewardValue(
            WaveRewardType rewardType,
            bool isBossReward,
            int waveIndex,
            float normalHealPercent,
            float bossHealPercent,
            int normalExperienceBase,
            int bossExperienceBase,
            float normalDamageBoost,
            float bossDamageBoost,
            float normalDefenseBoost,
            float bossDefenseBoost,
            float normalMoveSpeedBoost,
            float bossMoveSpeedBoost)
        {
            switch (rewardType)
            {
                case WaveRewardType.Heal:
                    return isBossReward ? bossHealPercent : normalHealPercent;
                case WaveRewardType.Experience:
                    return (isBossReward ? bossExperienceBase : normalExperienceBase) +
                        ClampMin(0, waveIndex * 2);
                case WaveRewardType.DamageBoost:
                    return isBossReward ? bossDamageBoost : normalDamageBoost;
                case WaveRewardType.DefenseBoost:
                    return isBossReward ? bossDefenseBoost : normalDefenseBoost;
                case WaveRewardType.MoveSpeedBoost:
                    return isBossReward ? bossMoveSpeedBoost : normalMoveSpeedBoost;
                default:
                    return 0.0f;
            }
        }

        public int ToPercentInt(float value)
        {
            float safeValue = value < 0.0f ? 0.0f : value;
            return RoundToInt(safeValue * 100.0f);
        }

        private static float ClampMin(float min, float value)
        {
            return value < min ? min : value;
        }

        private static int ClampMin(int min, int value)
        {
            return value < min ? min : value;
        }

        private static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }
    }
}
