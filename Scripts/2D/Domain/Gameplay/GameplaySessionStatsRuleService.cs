namespace LAB2D
{
    /// <summary>
    /// 运行时玩法会话统计的纯算术规则。
    /// </summary>
    public sealed class GameplaySessionStatsRuleService
    {
        public float ClampComboTimeout(float timeout)
        {
            return timeout < 0.1f ? 0.1f : timeout;
        }

        public int ToRecordedDamage(float damage)
        {
            int rounded = MathHelper.RoundToInt(damage);
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
            int rounded = MathHelper.RoundToInt(score);
            if (rounded < minScore)
            {
                return minScore;
            }

            return rounded > maxScore ? maxScore : rounded;
        }

    }
}
