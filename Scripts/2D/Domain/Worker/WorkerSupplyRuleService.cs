namespace LAB2D
{
    /// <summary>
    /// 工人补给需求的纯算术规则。
    /// </summary>
    public sealed class WorkerSupplyRuleService
    {
        public float GetRecoverNeed(float current, float max)
        {
            float need = max - current;
            return need < 0.0f ? 0.0f : need;
        }

        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }

        public int ToRecoverNeedCount(float recoverNeed)
        {
            return MathHelper.CeilToInt(recoverNeed);
        }

        public int GetVisibleIssueCount(int issueCount, int maxIssueCount)
        {
            int safeIssueCount = issueCount < 0 ? 0 : issueCount;
            int safeMaxIssueCount = maxIssueCount < 0 ? 0 : maxIssueCount;
            return safeIssueCount < safeMaxIssueCount ? safeIssueCount : safeMaxIssueCount;
        }

    }
}
