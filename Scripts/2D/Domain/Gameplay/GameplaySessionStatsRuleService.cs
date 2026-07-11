namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for runtime gameplay session statistics.
    /// </summary>
    public sealed class GameplaySessionStatsRuleService
    {
        public float ClampComboTimeout(float timeout)
        {
            return timeout < 0.1f ? 0.1f : timeout;
        }

        public int ToRecordedDamage(float damage)
        {
            int rounded = RoundToInt(damage);
            return rounded < 0 ? 0 : rounded;
        }

        public float GetSessionDuration(float currentRealtime, float sessionStartRealtime)
        {
            float duration = currentRealtime - sessionStartRealtime;
            return duration < 0.0f ? 0.0f : duration;
        }

        public int GetNextCombo(float currentRealtime, float lastDefeatRealtime, float comboTimeout, int currentCombo)
        {
            return currentRealtime - lastDefeatRealtime <= comboTimeout
                ? currentCombo + 1
                : 1;
        }

        public int GetMaxCombo(int currentMaxCombo, int currentCombo)
        {
            return currentCombo > currentMaxCombo ? currentCombo : currentMaxCombo;
        }

        public int ToClampedScore(float score, int minScore, int maxScore)
        {
            int rounded = RoundToInt(score);
            if (rounded < minScore)
            {
                return minScore;
            }

            return rounded > maxScore ? maxScore : rounded;
        }

        private static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }
    }
}
