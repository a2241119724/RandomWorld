namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for colony command center monitoring.
    /// </summary>
    public sealed class ColonyCommandCenterRuleService
    {
        public float ClampRefreshInterval(float interval)
        {
            return interval < 0.1f ? 0.1f : interval;
        }
    }
}
