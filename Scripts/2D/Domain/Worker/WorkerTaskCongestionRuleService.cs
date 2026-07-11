namespace LAB2D
{
    /// <summary>
    /// 工人任务拥堵监控的纯算术规则。
    /// </summary>
    public sealed class WorkerTaskCongestionRuleService
    {
        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }
    }
}
