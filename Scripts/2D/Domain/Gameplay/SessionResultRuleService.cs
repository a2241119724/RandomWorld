using System;

namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 会话结果评分、评级与衍生统计数据计算的纯规则。
    /// </summary>
    public sealed class SessionResultRuleService
    {
        private const int KillScorePerEnemy = 100;
        private const int MaxKillScore = 3500;
        private const int ComboScorePerCombo = 50;
        private const int MaxComboScore = 2500;
        private const int MaxSurvivalScore = 2000;
        private const int SurvivalDeathPenalty = 500;
        private const float EfficiencyScoreMultiplier = 300.0f;
 private const int MaxEfficiencyScore = 1500;
        private const int CollectionScorePerItem = 5;
        private const int MaxCollectionScore = 500;
        private const int MaxCombatScore = 10000;
        private const int MinCombatScore = 0;

        private const int Star5Threshold = 8000;
        private const int Star4Threshold = 6000;
        private const int Star3Threshold = 4000;
        private const int Star2Threshold = 2000;

        private const int GradeSThreshold = 8000;
        private const int GradeAThreshold = 6000;
        private const int GradeBThreshold = 4000;
        private const int GradeCThreshold = 2000;

        /// <summary>
        /// 计算暴击率百分比（0-100）。
        /// 根据总伤害量估算命中次数。
        /// </summary>
        public float CalculateCriticalHitRate(int criticalHitCount, int totalDamageDealt)
        {
            int estimatedHitCount = totalDamageDealt > 0
                ? Math.Max(1, totalDamageDealt / 10)
                : 0;
            if (estimatedHitCount <= 0)
            {
                return 0.0f;
            }

            return Math.Min(100.0f, (float)criticalHitCount / estimatedHitCount * 100.0f);
        }

        /// <summary>
        /// 计算伤害效率（输出/承受比率）。
        /// </summary>
        public float CalculateDamageEfficiency(int totalDamageDealt, int totalDamageTaken)
        {
            if (totalDamageTaken > 0)
            {
                return (float)totalDamageDealt / totalDamageTaken;
            }

            return totalDamageDealt;
        }

        /// <summary>
        /// 计算击杀分数组件（上限3500）。
        /// </summary>
        public float CalculateKillScore(int totalDefeatedEnemyCount)
        {
            return Math.Min(MaxKillScore, MathHelper.ClampMin(totalDefeatedEnemyCount, 0) * KillScorePerEnemy);
        }

        /// <summary>
        /// 计算连击分数组件（上限2500）。
        /// </summary>
        public float CalculateComboScore(int maxCombo)
        {
            return Math.Min(MaxComboScore, MathHelper.ClampMin(maxCombo, 0) * ComboScorePerCombo);
        }

        /// <summary>
        /// 计算生存分数组件（上限2000）。
        /// </summary>
        public float CalculateSurvivalScore(bool hasSurvived, int playerDeathCount)
        {
            if (hasSurvived)
            {
                return MaxSurvivalScore;
            }

            float penalty = MathHelper.ClampMin(playerDeathCount, 0) * SurvivalDeathPenalty;
            float score = MaxSurvivalScore - penalty;
            return score < 0.0f ? 0.0f : score;
        }

        /// <summary>
        /// 计算效率分数组件（上限1500）。
        /// </summary>
        public float CalculateEfficiencyScore(float damageEfficiency)
        {
            return Math.Min(MaxEfficiencyScore, damageEfficiency * EfficiencyScoreMultiplier);
        }

        /// <summary>
        /// 计算收集分数组件（上限500）。
        /// </summary>
        public float CalculateCollectionScore(int totalCollectedItemCount)
        {
            return Math.Min(MaxCollectionScore, MathHelper.ClampMin(totalCollectedItemCount, 0) * CollectionScorePerItem);
        }

        /// <summary>
        /// 从所有组件计算总战斗分数。
        /// </summary>
        public int CalculateCombatScore(
            int totalDefeatedEnemyCount,
            int maxCombo,
            bool hasSurvived,
            int playerDeathCount,
            float damageEfficiency,
            int totalCollectedItemCount)
        {
            float score = CalculateKillScore(totalDefeatedEnemyCount)
                + CalculateComboScore(maxCombo)
                + CalculateSurvivalScore(hasSurvived, playerDeathCount)
                + CalculateEfficiencyScore(damageEfficiency)
                + CalculateCollectionScore(totalCollectedItemCount);

            int rounded = MathHelper.RoundToInt(score);
            return ClampScore(rounded);
        }

        /// <summary>
        /// 根据战斗分数获取星级评价（1-5星）。
        /// </summary>
        public int GetStarRating(int combatScore)
        {
            if (combatScore >= Star5Threshold) return 5;
            if (combatScore >= Star4Threshold) return 4;
            if (combatScore >= Star3Threshold) return 3;
            if (combatScore >= Star2Threshold) return 2;
            return 1;
        }

        /// <summary>
        /// 根据战斗分数获取等级文本（S/A/B/C/D）。
        /// </summary>
        public string GetGradeText(int combatScore)
        {
            if (combatScore >= GradeSThreshold) return "S";
            if (combatScore >= GradeAThreshold) return "A";
            if (combatScore >= GradeBThreshold) return "B";
            if (combatScore >= GradeCThreshold) return "C";
            return "D";
        }

        private static int ClampScore(int score)
        {
            if (score < MinCombatScore) return MinCombatScore;
            if (score > MaxCombatScore) return MaxCombatScore;
            return score;
        }

    }
}
