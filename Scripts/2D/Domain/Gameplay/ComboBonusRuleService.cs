namespace LAB2D
{
    /// <summary>
    /// Pure rules for combo bonus tier lookup and multiplier calculation.
    /// </summary>
    public sealed class ComboBonusRuleService
    {
        /// <summary>
        /// Find the tier index for a given combo count.
        /// Searches from highest tier to lowest, returning the first match.
        /// </summary>
        /// <param name="combo">Current combo count.</param>
        /// <param name="tierThresholds">Array of tier minimum combo thresholds, indexed by tier.</param>
        /// <returns>Matching tier index (0 is the base tier).</returns>
        public int FindTierIndex(int combo, int[] tierThresholds)
        {
            if (tierThresholds == null || tierThresholds.Length == 0)
            {
                return 0;
            }

            for (int i = tierThresholds.Length - 1; i >= 0; i--)
            {
                if (combo >= tierThresholds[i])
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Get the damage multiplier for the tier at the given index.
        /// </summary>
        /// <param name="tierIndex">Tier index.</param>
        /// <param name="tierDamageMultipliers">Array of damage multipliers, indexed by tier.</param>
        /// <returns>Damage multiplier for the tier.</returns>
        public float GetDamageMultiplier(int tierIndex, float[] tierDamageMultipliers)
        {
            if (tierDamageMultipliers == null || tierDamageMultipliers.Length == 0)
            {
                return 1.0f;
            }

            int safeIndex = ClampIndex(tierIndex, tierDamageMultipliers.Length);
            return tierDamageMultipliers[safeIndex];
        }

        /// <summary>
        /// Get the experience multiplier for the tier at the given index.
        /// </summary>
        /// <param name="tierIndex">Tier index.</param>
        /// <param name="tierExpMultipliers">Array of experience multipliers, indexed by tier.</param>
        /// <returns>Experience multiplier for the tier.</returns>
        public float GetExperienceMultiplier(int tierIndex, float[] tierExpMultipliers)
        {
            if (tierExpMultipliers == null || tierExpMultipliers.Length == 0)
            {
                return 1.0f;
            }

            int safeIndex = ClampIndex(tierIndex, tierExpMultipliers.Length);
            return tierExpMultipliers[safeIndex];
        }

        /// <summary>
        /// Get the tier label for the tier at the given index.
        /// </summary>
        /// <param name="tierIndex">Tier index.</param>
        /// <param name="tierLabels">Array of tier labels, indexed by tier.</param>
        /// <returns>Tier label string, or empty if not set.</returns>
        public string GetTierLabel(int tierIndex, string[] tierLabels)
        {
            if (tierLabels == null || tierLabels.Length == 0)
            {
                return string.Empty;
            }

            int safeIndex = ClampIndex(tierIndex, tierLabels.Length);
            return tierLabels[safeIndex] ?? string.Empty;
        }

        private static int ClampIndex(int index, int length)
        {
            if (index < 0)
            {
                return 0;
            }

            return index >= length ? length - 1 : index;
        }
    }
}
