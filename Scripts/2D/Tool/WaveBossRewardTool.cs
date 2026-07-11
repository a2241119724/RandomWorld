namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 波次 Boss 与奖励通用工具。
    /// 只负责纯计算和展示文案，不访问场景对象、Prefab、存档、Photon 或 AssetBundle。
    /// </summary>
    public static class WaveBossRewardTool
    {
        private static readonly WaveBossRuleService BossRuleService = new WaveBossRuleService();

        /// <summary>
        /// 判断指定波次是否为 Boss 波。
        /// </summary>
        /// <param name="waveIndex">从 1 开始的波次索引。</param>
        /// <returns>当前波次是否应生成 Boss。</returns>
        public static bool IsBossWave(int waveIndex)
        {
            return BossRuleService.IsBossWave(waveIndex, WaveBossRewardConstant.BossWaveInterval);
        }

        /// <summary>
        /// 根据 Boss 波规则修正敌人数量。
        /// </summary>
        /// <param name="baseEnemyCount">WaveManager 原始敌人数。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <returns>最终生成敌人数。</returns>
        public static int GetEnemyCountForWave(int baseEnemyCount, int waveIndex)
        {
            return BossRuleService.GetEnemyCountForWave(
                baseEnemyCount,
                waveIndex,
                WaveBossRewardConstant.BossWaveInterval,
                WaveBossRewardConstant.BossGuardianExtraEnemyCount);
        }

        /// <summary>
        /// 获取普通敌人的生命倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>生命倍率。</returns>
        public static float GetNormalEnemyHealthMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetNormalEnemyHealthMultiplier(
                waveIndex,
                difficultyScale,
                WaveBossRewardConstant.NormalEnemyHealthScalePerWave);
        }

        /// <summary>
        /// 获取普通敌人的攻击倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>攻击倍率。</returns>
        public static float GetNormalEnemyAttackMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetNormalEnemyAttackMultiplier(
                waveIndex,
                difficultyScale,
                WaveBossRewardConstant.NormalEnemyAttackScalePerWave);
        }

        /// <summary>
        /// 获取普通敌人的防御倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>防御倍率。</returns>
        public static float GetNormalEnemyDefenseMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetNormalEnemyDefenseMultiplier(
                waveIndex,
                WaveBossRewardConstant.NormalEnemyDefenseScalePerWave);
        }

        /// <summary>
        /// 获取 Boss 生命倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>Boss 生命倍率。</returns>
        public static float GetBossHealthMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetBossHealthMultiplier(
                GetNormalEnemyHealthMultiplier(waveIndex, difficultyScale),
                WaveBossRewardConstant.BossHealthMultiplier);
        }

        /// <summary>
        /// 获取 Boss 攻击倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>Boss 攻击倍率。</returns>
        public static float GetBossAttackMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetBossAttackMultiplier(
                GetNormalEnemyAttackMultiplier(waveIndex, difficultyScale),
                WaveBossRewardConstant.BossAttackMultiplier);
        }

        /// <summary>
        /// 获取 Boss 防御倍率。
        /// </summary>
        /// <param name="waveIndex">当前波次。</param>
        /// <param name="difficultyScale">WaveManager 当前难度倍率。</param>
        /// <returns>Boss 防御倍率。</returns>
        public static float GetBossDefenseMultiplier(int waveIndex, float difficultyScale)
        {
            return BossRuleService.GetBossDefenseMultiplier(
                GetNormalEnemyDefenseMultiplier(waveIndex, difficultyScale),
                WaveBossRewardConstant.BossDefenseMultiplier);
        }

        /// <summary>
        /// 获取奖励类型名称。
        /// </summary>
        /// <param name="rewardType">奖励类型。</param>
        /// <returns>用于 UI 展示的中文名称。</returns>
        public static string GetRewardName(WaveRewardType rewardType)
        {
            switch (rewardType)
            {
                case WaveRewardType.Heal:
                    return "生命补给";
                case WaveRewardType.Experience:
                    return "经验结晶";
                case WaveRewardType.DamageBoost:
                    return "锋刃祝福";
                case WaveRewardType.DefenseBoost:
                    return "护盾祝福";
                case WaveRewardType.MoveSpeedBoost:
                    return "迅捷祝福";
                default:
                    return "未知奖励";
            }
        }

        /// <summary>
        /// 根据奖励类型和波次构建奖励数值。
        /// </summary>
        /// <param name="rewardType">奖励类型。</param>
        /// <param name="isBossReward">是否来自 Boss 波。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <returns>奖励数值。百分比类奖励使用 0.1 表示 10%。</returns>
        public static float GetRewardValue(WaveRewardType rewardType, bool isBossReward, int waveIndex)
        {
            return BossRuleService.GetRewardValue(
                rewardType,
                isBossReward,
                waveIndex,
                WaveBossRewardConstant.NormalHealPercent,
                WaveBossRewardConstant.BossHealPercent,
                WaveBossRewardConstant.NormalExperienceBase,
                WaveBossRewardConstant.BossExperienceBase,
                WaveBossRewardConstant.NormalDamageBoost,
                WaveBossRewardConstant.BossDamageBoost,
                WaveBossRewardConstant.NormalDefenseBoost,
                WaveBossRewardConstant.BossDefenseBoost,
                WaveBossRewardConstant.NormalMoveSpeedBoost,
                WaveBossRewardConstant.BossMoveSpeedBoost);
        }

        /// <summary>
        /// 构建奖励描述。
        /// </summary>
        /// <param name="rewardType">奖励类型。</param>
        /// <param name="value">奖励数值。</param>
        /// <returns>用于按钮和提示的说明文本。</returns>
        public static string BuildRewardDescription(WaveRewardType rewardType, float value)
        {
            switch (rewardType)
            {
                case WaveRewardType.Heal:
                    return $"恢复最大生命的 {FormatPercent(value)}";
                case WaveRewardType.Experience:
                    return $"获得 {Mathf.RoundToInt(value)} 点经验";
                case WaveRewardType.DamageBoost:
                    return $"本局伤害 +{FormatPercent(value)}";
                case WaveRewardType.DefenseBoost:
                    return $"本局受到伤害 -{FormatPercent(value)}";
                case WaveRewardType.MoveSpeedBoost:
                    return $"本局移动速度 +{FormatPercent(value)}";
                default:
                    return "奖励效果未知";
            }
        }

        /// <summary>
        /// 构建 Boss 名称。
        /// </summary>
        /// <param name="baseName">敌人原始名称。</param>
        /// <param name="waveIndex">当前波次。</param>
        /// <returns>Boss 展示名称。</returns>
        public static string BuildBossName(string baseName, int waveIndex)
        {
            string safeName = string.IsNullOrEmpty(baseName) ? "Enemy" : baseName;
            return $"Boss_Wave{waveIndex}_{safeName}";
        }

        /// <summary>
        /// 构建当前 Buff 摘要。
        /// </summary>
        /// <param name="damageBonus">伤害加成。</param>
        /// <param name="damageReduction">减伤加成。</param>
        /// <param name="moveSpeedBonus">移动加成。</param>
        /// <returns>适合 HUD 和 Editor 展示的文本。</returns>
        public static string BuildBuffSummary(float damageBonus, float damageReduction, float moveSpeedBonus)
        {
            return $"伤害 +{FormatPercent(damageBonus)} | 减伤 {FormatPercent(damageReduction)} | 移动 +{FormatPercent(moveSpeedBonus)}";
        }

        /// <summary>
        /// 格式化百分比。
        /// </summary>
        /// <param name="value">小数形式百分比。</param>
        /// <returns>百分比文本。</returns>
        public static string FormatPercent(float value)
        {
            return $"{BossRuleService.ToPercentInt(value)}%";
        }

        /// <summary>
        /// 把增量累加到当前值，并限制最大值。
        /// </summary>
        /// <param name="current">当前累计值。</param>
        /// <param name="add">新增值。</param>
        /// <param name="max">上限。</param>
        /// <returns>限制后的累计值。</returns>
        public static float AddWithCap(float current, float add, float max)
        {
            return BossRuleService.AddWithCap(current, add, max);
        }
    }
}
