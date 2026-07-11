namespace LAB2D
{
    /// <summary>
    /// 成就进度的纯算术规则。
    /// </summary>
    public sealed class AchievementRuleService
    {
        public int GetElapsedMinutes(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0.0f)
            {
                return 0;
            }

            return (int)(elapsedSeconds / 60.0f);
        }

        public int ClampProgressToTarget(int progress, int target)
        {
            return progress < target ? progress : target;
        }

        public float GetProgressRatio(int current, int target)
        {
            if (target <= 0)
            {
                return 1.0f;
            }

            return MathHelper.Clamp01((float)current / target);
        }

    }
}
