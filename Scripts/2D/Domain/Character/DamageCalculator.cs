namespace LAB2D
{
    /// <summary>
    /// 角色适配器共享的纯伤害与生命值算术规则。
    /// </summary>
    public sealed class DamageCalculator
    {
        private const float MinAppliedDamage = 0.1f;

        public float ApplyDefense(float incomingDamage, float defense)
        {
            if (incomingDamage <= 0.0f)
            {
                return 0.0f;
            }

            float reducedDamage = incomingDamage - (incomingDamage * defense / 10.0f);
            return reducedDamage < MinAppliedDamage ? MinAppliedDamage : reducedDamage;
        }

        public float GetOutgoingDamage(float attack, float criticalDamage, bool isCritical)
        {
            return isCritical ? attack * criticalDamage : attack;
        }

        public CharacterHealthResult ApplyDamageToHealth(float currentHp, float damage)
        {
            float safeDamage = damage < 0.0f ? 0.0f : damage;
            float remainingHp = currentHp - safeDamage;
            bool isDead = remainingHp <= 0.0f;
            return new CharacterHealthResult(isDead ? 0.0f : remainingHp, isDead);
        }

        public float ApplyHealingToHealth(float currentHp, float maxHp, float healing)
        {
            float safeHealing = healing < 0.0f ? 0.0f : healing;
            float nextHp = currentHp + safeHealing;
            return nextHp > maxHp ? maxHp : nextHp;
        }
    }

    public readonly struct CharacterHealthResult
    {
        public CharacterHealthResult(float remainingHp, bool isDead)
        {
            this.RemainingHp = remainingHp;
            this.IsDead = isDead;
        }

        public float RemainingHp { get; }

        public bool IsDead { get; }
    }
}
