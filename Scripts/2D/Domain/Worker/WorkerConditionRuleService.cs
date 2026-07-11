namespace LAB2D
{
    /// <summary>
    /// 工人状态比率和显示值的纯算术规则。
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
