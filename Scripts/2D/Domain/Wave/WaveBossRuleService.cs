namespace LAB2D.Domain.Wave
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// Boss波次节奏和敌人数量调整的纯规则。
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

        public bool IsBossEnemySpawn(int waveIndex, int spawnIndex, int totalEnemies, int bossWaveInterval)
        {
            return this.IsBossWave(waveIndex, bossWaveInterval) &&
                spawnIndex == MathHelper.ClampMin(totalEnemies - 1, 0);
        }

        public int ClampWaveIndex(int waveIndex)
        {
            return MathHelper.ClampMin(waveIndex, 1);
        }

        public int GetRewardOptionCount(int configuredOptionCount, int availableRewardTypeCount)
        {
            int safeConfiguredCount = MathHelper.ClampMin(configuredOptionCount, 0);
            int safeAvailableCount = MathHelper.ClampMin(availableRewardTypeCount, 0);
            return safeConfiguredCount < safeAvailableCount ? safeConfiguredCount : safeAvailableCount;
        }

        public float GetNormalEnemyHealthMultiplier(
            int waveIndex,
            float difficultyScale,
            float healthScalePerWave)
        {
            return MathHelper.ClampMin(difficultyScale + ((waveIndex - 1) * healthScalePerWave), 1.0f);
        }

        public float GetNormalEnemyAttackMultiplier(
            int waveIndex,
            float difficultyScale,
            float attackScalePerWave)
        {
            return MathHelper.ClampMin(difficultyScale + ((waveIndex - 1) * attackScalePerWave), 1.0f);
        }

        public float GetNormalEnemyDefenseMultiplier(int waveIndex, float defenseScalePerWave)
        {
            return MathHelper.ClampMin(1.0f + ((waveIndex - 1) * defenseScalePerWave), 1.0f);
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
                        MathHelper.ClampMin(waveIndex * 2, 0);
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
            return MathHelper.RoundToInt(safeValue * 100.0f);
        }

        public int ToRoundedInt(float value)
        {
            return MathHelper.RoundToInt(value);
        }

        public float AddWithCap(float current, float add, float max)
        {
            float safeMax = max < 0.0f ? 0.0f : max;
            float safeCurrent = current < 0.0f ? 0.0f : current;
            float safeAdd = add < 0.0f ? 0.0f : add;
            float value = safeCurrent + safeAdd;
            return value > safeMax ? safeMax : value;
        }

        public float ScaleAttribute(float currentValue, float multiplier, float minValue)
        {
            float scaledValue = currentValue * multiplier;
            return scaledValue < minValue ? minValue : scaledValue;
        }
    }
}
