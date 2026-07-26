namespace LAB2D.Domain.Player
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Enum;
    /// <summary>
    /// 玩家生命警报比率和显示值的纯算术规则。
    /// </summary>
    public sealed class PlayerVitalAlertRuleService
    {
        /// <summary>血量低于该比例时进入受伤提示。</summary>
        public const float WarningRatio = 0.35f;

        /// <summary>血量低于该比例时进入濒危提示。</summary>
        public const float CriticalRatio = 0.18f;

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

        /// <summary>
        /// 根据血量比例和复活状态计算生命提示等级。
        /// 使用本服务的 WarningRatio 和 CriticalRatio 常量进行阈值判定。
        /// </summary>
        /// <param name="hpRatio">血量比例（0 到 1）。</param>
        /// <param name="isRespawning">是否处于复活等待。</param>
        /// <returns>玩家生命提示等级。</returns>
        public PlayerVitalAlertLevel GetLevel(float hpRatio, bool isRespawning)
        {
            if (isRespawning)
            {
                return PlayerVitalAlertLevel.Respawning;
            }

            float ratio = MathHelper.Clamp01(hpRatio);
            if (ratio <= CriticalRatio)
            {
                return PlayerVitalAlertLevel.Critical;
            }

            if (ratio <= WarningRatio)
            {
                return PlayerVitalAlertLevel.Wounded;
            }

            return PlayerVitalAlertLevel.Safe;
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
