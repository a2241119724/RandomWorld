namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 主动技能数值与冷却显示的纯算术规则。
    /// </summary>
    public sealed class SkillRuleService
    {
        /// <summary>技能最大等级。</summary>
        public const int MaxSkillLevel = 5;

        public float CalculateSkillDamage(float baseAtn, float damageMultiplier, int skillLevel, float upgradeEffectIncrease)
        {
            float levelBonus = 1.0f + ((skillLevel - 1) * upgradeEffectIncrease);
            float damage = baseAtn * damageMultiplier * levelBonus;
            return damage < 1.0f ? 1.0f : damage;
        }

        public float CalculateSkillCooldown(float baseCooldown, int skillLevel, float upgradeCooldownReduction)
        {
            float reduction = (skillLevel - 1) * upgradeCooldownReduction;
            float cooldown = baseCooldown * (1.0f - reduction);
            return cooldown < 0.5f ? 0.5f : cooldown;
        }

        public int ToCooldownDisplaySeconds(float remainingSeconds)
        {
            return MathHelper.CeilToInt(remainingSeconds);
        }

        public float GetCooldownProgress(float remainingSeconds, float totalCooldown)
        {
            if (totalCooldown <= 0.0f || remainingSeconds <= 0.0f)
            {
                return 0.0f;
            }

            return MathHelper.Clamp01(remainingSeconds / totalCooldown);
        }

        /// <summary>
        /// 获取技能升级所需经验点数。
        /// 满级（MaxSkillLevel）或无效等级返回 -1。
        /// </summary>
        /// <param name="currentLevel">当前技能等级（1-4），5级已达到上限。</param>
        /// <returns>升级所需经验点数；已满级或无效等级返回 -1。</returns>
        public int GetUpgradeCost(int currentLevel)
        {
            return currentLevel switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 5,
                _ => -1,
            };
        }

        /// <summary>
        /// 计算技能 Buff 的实际倍率（基于技能等级）。
        /// 用于 SelfBuff 类型技能的效果计算。
        /// </summary>
        /// <param name="baseMultiplier">技能基础倍率。</param>
        /// <param name="skillLevel">技能等级（1-MaxSkillLevel）。</param>
        /// <param name="upgradeEffectIncrease">每级提升比例。</param>
        /// <returns>等级加成后的倍率。</returns>
        public float CalculateBuffMultiplier(float baseMultiplier, int skillLevel, float upgradeEffectIncrease)
        {
            return baseMultiplier + ((skillLevel - 1) * upgradeEffectIncrease * 0.5f);
        }

        /// <summary>
        /// 计算技能治疗量（基于技能等级）。
        /// 用于 SelfHeal 类型技能。
        /// </summary>
        /// <param name="baseHealAmount">基础治疗量。</param>
        /// <param name="skillLevel">技能等级（1-MaxSkillLevel）。</param>
        /// <param name="upgradeEffectIncrease">每级提升比例。</param>
        /// <returns>等级加成后的治疗量。</returns>
        public float CalculateHealAmount(float baseHealAmount, int skillLevel, float upgradeEffectIncrease)
        {
            float levelBonus = 1.0f + ((skillLevel - 1) * upgradeEffectIncrease);
            return baseHealAmount * levelBonus;
        }

        /// <summary>
        /// 检查当前法力是否足够施放技能。
        /// </summary>
        /// <param name="currentMp">当前法力值。</param>
        /// <param name="manaCost">技能法力消耗。</param>
        /// <returns>法力充足返回 true。</returns>
        public bool HasEnoughMana(float currentMp, float manaCost)
        {
            return currentMp >= manaCost;
        }
    }
}
