namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for active skill values and cooldown display.
    /// </summary>
    public sealed class SkillRuleService
    {
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
            return CeilToInt(remainingSeconds);
        }

        public float GetCooldownProgress(float remainingSeconds, float totalCooldown)
        {
            if (totalCooldown <= 0.0f || remainingSeconds <= 0.0f)
            {
                return 0.0f;
            }

            return Clamp01(remainingSeconds / totalCooldown);
        }

        private static float Clamp01(float value)
        {
            if (value < 0.0f)
            {
                return 0.0f;
            }

            return value > 1.0f ? 1.0f : value;
        }

        private static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
        }
    }
}
