namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 所有领域RuleService的共享数学工具类。
    /// 消除了10余个文件中重复的RoundToInt、Clamp01、CeilToInt、ClampMin、ClampMax方法。
    /// </summary>
    public static class MathHelper
    {
        /// <summary>将float四舍五入到最接近的整数（中间值规则：+0.5向上取整，-0.5向下取整）。</summary>
        public static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }

        /// <summary>将float限制在 [0.0, 1.0] 范围内。</summary>
        public static float Clamp01(float value)
        {
            if (value < 0.0f) return 0.0f;
            if (value > 1.0f) return 1.0f;
            return value;
        }

        /// <summary>将float向上取整到最接近的整数。</summary>
        public static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
        }

        /// <summary>将int值限制为不小于最小值。</summary>
        public static int ClampMin(int value, int min)
        {
            return value < min ? min : value;
        }

        /// <summary>将float值限制为不小于最小值。</summary>
        public static float ClampMin(float value, float min)
        {
            return value < min ? min : value;
        }

        /// <summary>将int值限制为不大于最大值。</summary>
        public static int ClampMax(int value, int max)
        {
            return value > max ? max : value;
        }

        /// <summary>将float值限制为不大于最大值。</summary>
        public static float ClampMax(float value, float max)
        {
            return value > max ? max : value;
        }

        /// <summary>安全比率 = current / max，限制在 [0.0, 1.0] 范围内；若max无效则返回0。</summary>
        public static float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f) return 0.0f;
            return Clamp01(current / max);
        }

        /// <summary>将 [0,1] 比率转换为百分比整数（0-100）。</summary>
        public static int ToPercentInt(float ratio)
        {
            return RoundToInt(Clamp01(ratio) * 100.0f);
        }

        /// <summary>将刷新间隔限制为至少0.1秒。</summary>
        public static float ClampRefreshInterval(float interval)
        {
            return interval < 0.1f ? 0.1f : interval;
        }
    }
}
