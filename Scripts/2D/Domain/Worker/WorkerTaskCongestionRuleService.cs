namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for worker task congestion monitoring.
    /// </summary>
    public sealed class WorkerTaskCongestionRuleService
    {
        public float ClampRefreshInterval(float interval)
        {
            return interval < 0.1f ? 0.1f : interval;
        }
    }
}
