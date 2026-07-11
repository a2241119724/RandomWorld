namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for worker condition ratios and display values.
    /// </summary>
    public sealed class WorkerConditionRuleService
    {
        public float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f)
            {
                return 0.0f;
            }

            return MathHelper.Clamp01(current / max);
        }

        public int ToPercentInt(float ratio)
        {
            return MathHelper.ToPercentInt(ratio);
        }

    }
}
