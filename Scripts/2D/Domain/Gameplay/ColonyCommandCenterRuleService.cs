namespace LAB2D
{
    /// <summary>
    /// 殖民地指挥中心监控的纯算术规则。
    /// </summary>
    public sealed class ColonyCommandCenterRuleService
    {
        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }
    }
}
