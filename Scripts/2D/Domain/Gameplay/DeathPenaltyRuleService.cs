namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for player death penalty and respawn timing.
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
            return RoundToInt(currentExperience * lossPercent);
        }

        public int ApplyExperienceLoss(int currentExperience, int experienceLoss)
        {
            int nextExperience = currentExperience - experienceLoss;
            return nextExperience < 0 ? 0 : nextExperience;
        }

        public int ToCountdownSeconds(float seconds)
        {
            return CeilToInt(seconds);
        }

        public float GetRestoredHp(float maxHp, float hpRestorePercent)
        {
            return maxHp * hpRestorePercent;
        }

        private static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }

        private static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
        }
    }
}
