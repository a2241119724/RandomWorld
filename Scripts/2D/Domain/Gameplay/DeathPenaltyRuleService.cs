namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Domain.Common;
    /// <summary>
    /// 玩家死亡惩罚与复活计时的纯算术规则。
    /// </summary>
    public sealed class DeathPenaltyRuleService
    {
        public bool IsRespawning(float respawnDeadline, float currentRealtime)
        {
            return respawnDeadline > 0.0f && currentRealtime < respawnDeadline;
        }

        public float GetRespawnRemaining(float respawnDeadline, float currentRealtime)
        {
            float remaining = respawnDeadline - currentRealtime;
            return remaining < 0.0f ? 0.0f : remaining;
        }

        public float GetRespawnDeadline(float currentRealtime, float respawnDelaySeconds)
        {
            return currentRealtime + respawnDelaySeconds;
        }

        public int GetExperienceLoss(int currentExperience, float lossPercent)
        {
            return MathHelper.RoundToInt(currentExperience * lossPercent);
        }

        public int ApplyExperienceLoss(int currentExperience, int experienceLoss)
        {
            int nextExperience = currentExperience - experienceLoss;
            return nextExperience < 0 ? 0 : nextExperience;
        }

        public int ToCountdownSeconds(float seconds)
        {
            return MathHelper.CeilToInt(seconds);
        }

        public float GetRestoredHp(float maxHp, float hpRestorePercent)
        {
            return maxHp * hpRestorePercent;
        }

    }
}
