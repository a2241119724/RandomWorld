namespace LAB2D
{
    /// <summary>
    /// Shared math utilities for all Domain RuleServices.
    /// Eliminates duplicated RoundToInt, Clamp01, CeilToInt, ClampMin, ClampMax across 10+ files.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>Round float to nearest integer (banker-like midpoint: +0.5 → up, -0.5 → down).</summary>
        public static int RoundToInt(float value)
        {
            return value >= 0.0f ? (int)(value + 0.5f) : (int)(value - 0.5f);
        }

        /// <summary>Clamp float to [0.0, 1.0].</summary>
        public static float Clamp01(float value)
        {
            if (value < 0.0f) return 0.0f;
            if (value > 1.0f) return 1.0f;
            return value;
        }

        /// <summary>Ceil float to nearest integer.</summary>
        public static int CeilToInt(float value)
        {
            int integer = (int)value;
            return value > integer ? integer + 1 : integer;
        }

        /// <summary>Clamp int value to be at least min.</summary>
        public static int ClampMin(int value, int min)
        {
            return value < min ? min : value;
        }

        /// <summary>Clamp float value to be at least min.</summary>
        public static float ClampMin(float value, float min)
        {
            return value < min ? min : value;
        }

        /// <summary>Clamp int value to be at most max.</summary>
        public static int ClampMax(int value, int max)
        {
            return value > max ? max : value;
        }

        /// <summary>Clamp float value to be at most max.</summary>
        public static float ClampMax(float value, float max)
        {
            return value > max ? max : value;
        }

        /// <summary>Safe ratio = current / max, clamped to [0.0, 1.0]; returns 0 if max is invalid.</summary>
        public static float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f) return 0.0f;
            return Clamp01(current / max);
        }

        /// <summary>Convert a [0,1] ratio to a percent integer (0-100).</summary>
        public static int ToPercentInt(float ratio)
        {
            return RoundToInt(Clamp01(ratio) * 100.0f);
        }

        /// <summary>Clamp a refresh interval to at least 0.1 seconds.</summary>
        public static float ClampRefreshInterval(float interval)
        {
            return interval < 0.1f ? 0.1f : interval;
        }
    }
}
