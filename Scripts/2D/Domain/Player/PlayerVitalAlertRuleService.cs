namespace LAB2D
{
    /// <summary>
    /// 玩家生命警报比率和显示值的纯算术规则。
    /// </summary>
    public sealed class PlayerVitalAlertRuleService
    {
        public float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f)
            {
                return 0.0f;
            }

            return MathHelper.Clamp01(current / max);
        }

        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }

        public int ToPercentInt(float ratio)
        {
            return MathHelper.ToPercentInt(ratio);
        }

        public int ToDisplayHealth(float value)
        {
            float safeValue = value < 0.0f ? 0.0f : value;
            return MathHelper.CeilToInt(safeValue);
        }

        public bool IsDangerLevel(PlayerVitalAlertLevel level)
        {
            return level == PlayerVitalAlertLevel.Wounded ||
                level == PlayerVitalAlertLevel.Critical ||
                level == PlayerVitalAlertLevel.Respawning;
        }

        public bool IsMoreSevere(PlayerVitalAlertLevel next, PlayerVitalAlertLevel previous)
        {
            return GetSeverity(next) > GetSeverity(previous);
        }

        public int GetSeverity(PlayerVitalAlertLevel level)
        {
            switch (level)
            {
                case PlayerVitalAlertLevel.Wounded:
                    return 1;
                case PlayerVitalAlertLevel.Critical:
                    return 2;
                case PlayerVitalAlertLevel.Respawning:
                    return 3;
                default:
                    return 0;
            }
        }
    }
}
