namespace LAB2D.Domain.Gameplay
{
    /// <summary>
    /// 连击奖励等级查找与倍率计算的纯规则。
    /// </summary>
    public sealed class ComboBonusRuleService
    {
        /// <summary>
        /// 根据给定的连击数查找等级索引。
        /// 从最高等级向最低等级搜索，返回第一个匹配项。
        /// </summary>
        /// <param name="combo">当前连击数。</param>
        /// <param name="tierThresholds">按等级索引的连击最低阈值数组。</param>
        /// <returns>匹配的等级索引（0为基础等级）。</returns>
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
        /// 获取指定索引等级的伤害倍率。
        /// </summary>
        /// <param name="tierIndex">等级索引。</param>
        /// <param name="tierDamageMultipliers">按等级索引的伤害倍率数组。</param>
        /// <returns>该等级的伤害倍率。</returns>
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
        /// 获取指定索引等级的经验倍率。
        /// </summary>
        /// <param name="tierIndex">等级索引。</param>
        /// <param name="tierExpMultipliers">按等级索引的经验倍率数组。</param>
        /// <returns>该等级的经验倍率。</returns>
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
        /// 获取指定索引等级的标签。
        /// </summary>
        /// <param name="tierIndex">等级索引。</param>
        /// <param name="tierLabels">按等级索引的标签数组。</param>
        /// <returns>等级标签字符串，若未设置则返回空。</returns>
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
