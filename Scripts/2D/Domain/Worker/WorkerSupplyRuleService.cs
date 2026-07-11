namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for worker supply needs.
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
            return interval < 0.1f ? 0.1f : interval;
        }

        public int ToRecoverNeedCount(float recoverNeed)
        {
            return CeilToInt(recoverNeed);
        }

        public int GetVisibleIssueCount(int issueCount, int maxIssueCount)
        {
            int safeIssueCount = issueCount < 0 ? 0 : issueCount;
            int safeMaxIssueCount = maxIssueCount < 0 ? 0 : maxIssueCount;
            return safeIssueCount < safeMaxIssueCount ? safeIssueCount : safeMaxIssueCount;
        }

        private static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
        }
    }
}
