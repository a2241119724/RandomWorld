namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic rules for player vital alert ratios and display values.
    /// </summary>
    public sealed class PlayerVitalAlertRuleService
    {
        public float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f)
            {
                return 0.0f;
            }

            return Clamp01(current / max);
        }

        public float ClampRefreshInterval(float interval)
        {
            return interval < 0.1f ? 0.1f : interval;
        }

        public int ToPercentInt(float ratio)
        {
            return RoundToInt(Clamp01(ratio) * 100.0f);
        }

        public int ToDisplayHealth(float value)
        {
            float safeValue = value < 0.0f ? 0.0f : value;
            return CeilToInt(safeValue);
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

        private static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
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
