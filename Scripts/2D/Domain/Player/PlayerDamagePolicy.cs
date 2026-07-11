namespace LAB2D
{
    /// <summary>
    /// 判断玩家伤害是否可以生效的纯规则。
    /// </summary>
    public sealed class PlayerDamagePolicy
    {
        public float ClampInvincibilityDuration(float duration)
        {
            return duration < 0.0f ? 0.0f : duration;
        }

        public bool IsInvincible(float currentTime, float lastDamageTime, float invincibilityDuration)
        {
            float safeDuration = this.ClampInvincibilityDuration(invincibilityDuration);
            return safeDuration > 0.0f && currentTime - lastDamageTime < safeDuration;
        }

        public bool ShouldIgnoreDamage(
            float damage,
            bool isRespawning,
            float currentTime,
            float lastDamageTime,
            float invincibilityDuration)
        {
            if (damage <= 0.0f)
            {
                return true;
            }

            if (isRespawning)
            {
                return true;
            }

            return this.IsInvincible(currentTime, lastDamageTime, invincibilityDuration);
        }
    }
}
