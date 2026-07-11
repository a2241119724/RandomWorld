namespace LAB2D
{
    /// <summary>
    /// Pure rules for session result scoring, rating, and derived stats calculation.
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
        /// Calculate critical hit rate as a percentage (0-100).
        /// Estimates hit count from total damage dealt.
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
        /// Calculate damage efficiency (output / taken ratio).
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
        /// Calculate the kill score component (max 3500).
        /// </summary>
        public float CalculateKillScore(int totalDefeatedEnemyCount)
        {
            return Math.Min(MaxKillScore, MathHelper.ClampMin(totalDefeatedEnemyCount, 0) * KillScorePerEnemy);
        }

        /// <summary>
        /// Calculate the combo score component (max 2500).
        /// </summary>
        public float CalculateComboScore(int maxCombo)
        {
            return Math.Min(MaxComboScore, MathHelper.ClampMin(maxCombo, 0) * ComboScorePerCombo);
        }

        /// <summary>
        /// Calculate the survival score component (max 2000).
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
        /// Calculate the efficiency score component (max 1500).
        /// </summary>
        public float CalculateEfficiencyScore(float damageEfficiency)
        {
            return Math.Min(MaxEfficiencyScore, damageEfficiency * EfficiencyScoreMultiplier);
        }

        /// <summary>
        /// Calculate the collection score component (max 500).
        /// </summary>
        public float CalculateCollectionScore(int totalCollectedItemCount)
        {
            return Math.Min(MaxCollectionScore, MathHelper.ClampMin(totalCollectedItemCount, 0) * CollectionScorePerItem);
        }

        /// <summary>
        /// Calculate the total combat score from all components.
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
        /// Get star rating (1-5) from combat score.
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
        /// Get grade text (S/A/B/C/D) from combat score.
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

        private static float MinFloat(float a, float b)
        {
            return a < b ? a : b;
        }

        private static int MaxInt(int a, int b)
        {
            return a > b ? a : b;
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
