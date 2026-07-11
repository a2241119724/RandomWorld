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

            return Clamp01(current / max);
        }

        public int ToPercentInt(float ratio)
        {
            return RoundToInt(Clamp01(ratio) * 100.0f);
        }

        private static float Clamp01(float value)
        {
            if (value < 0.0f)
            {
                return 0.0f;
            }

            return value > 1.0f ? 1.0f : value;
        }

        private static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }
    }
}
