namespace LAB2D.Domain.Time
{
    using System;

    /// <summary>
    /// 昼夜规则 — 天索引/一天进度/相位/光照强度的纯函数计算。
    /// 纯 C# 实现无 UnityEngine 依赖；每游戏天时长由调用方传入
    /// （Gameplay 层传 GlobalData.GameDayTime），本服务不引用外层类型。
    ///
    /// 一天进度约定：0=午夜、0.25=清晨、0.5=正午、0.75=黄昏（与旧版 GameTimeUI 光照
    /// sin 曲线相位一致：sin(progress × 2π - π/2)，谷在 0、峰在 0.5）。
    /// </summary>
    public static class DayNightRuleService
    {
        /// <summary>夜晚区间起点（一天进度，含）：[0.80, 1.20) 跨午夜为夜。</summary>
        public const double NightStart = 0.80;

        /// <summary>夜晚区间终点（一天进度，不含）。</summary>
        public const double NightEnd = 0.20;

        /// <summary>黎明区间终点（一天进度，不含）：[0.20, 0.30) 为晨。</summary>
        public const double DawnEnd = 0.30;

        /// <summary>白天区间终点（一天进度，不含）：[0.30, 0.70) 为昼。</summary>
        public const double DayEnd = 0.70;

        /// <summary>黄昏区间终点（一天进度，不含）：[0.70, 0.80) 为昏。</summary>
        public const double DuskEnd = 0.80;

        /// <summary>光照强度下限（深夜）。</summary>
        public const float LightIntensityMin = 0.2f;

        /// <summary>光照强度上限（正午）。</summary>
        public const float LightIntensityMax = 0.8f;

        /// <summary>全局光强度下限（深夜）— 映射后正午=1.0 与无光照时代视觉一致，深夜保持经营可视性。</summary>
        public const float GlobalLightIntensityMin = 0.35f;

        /// <summary>全局光强度上限（正午）。</summary>
        public const float GlobalLightIntensityMax = 1.0f;

        /// <summary>
        /// 当前天索引（从 0 开始）：累计秒数整除每天时长。
        /// </summary>
        public static int DayIndex(double curGameTime, float dayLengthSeconds)
        {
            if (dayLengthSeconds <= 0f)
            {
                return 0;
            }

            return (int)(curGameTime / dayLengthSeconds);
        }

        /// <summary>
        /// 一天内进度 [0, 1)：0=午夜、0.5=正午。
        /// </summary>
        public static double DayProgress(double curGameTime, float dayLengthSeconds)
        {
            if (dayLengthSeconds <= 0f)
            {
                return 0.0;
            }

            double progress = curGameTime / dayLengthSeconds;
            return progress - Math.Floor(progress);
        }

        /// <summary>
        /// 取当前昼夜相位。
        /// </summary>
        public static GamePhase GetPhase(double curGameTime, float dayLengthSeconds)
        {
            return GetPhaseByProgress(DayProgress(curGameTime, dayLengthSeconds));
        }

        /// <summary>
        /// 按一天进度取相位（边界见各常量注释）。
        /// </summary>
        public static GamePhase GetPhaseByProgress(double progress)
        {
            if (progress >= NightStart || progress < NightEnd)
            {
                return GamePhase.Night;
            }

            if (progress < DawnEnd)
            {
                return GamePhase.Dawn;
            }

            if (progress < DayEnd)
            {
                return GamePhase.Day;
            }

            return GamePhase.Dusk;
        }

        /// <summary>
        /// 光照强度（迁移自旧版 GameTimeUI 的 sin 曲线，数值行为保持不变）：
        /// clamp(sin(progress × 6.2624 - 1.55) + 0.7, 0.2, 0.8)。
        /// </summary>
        public static float GetLightIntensity(double curGameTime, float dayLengthSeconds)
        {
            double progress = DayProgress(curGameTime, dayLengthSeconds);
            float raw = (float)Math.Sin((float)progress * 6.2624f - 1.55f) + 0.7f;
            return Clamp(raw, LightIntensityMin, LightIntensityMax);
        }

        /// <summary>
        /// 全局光强度 — 复用 <see cref="GetLightIntensity"/> 的 sin 曲线形状，归一化后
        /// 重映射到 [GlobalLightIntensityMin, GlobalLightIntensityMax]。URP 2D 下激活任意光后
        /// 进入真光照计算（sprite 颜色 × 光照），正午必须 ≈1.0 才与无光照时代的画面亮度一致。
        /// </summary>
        public static float GetGlobalLightIntensity(double curGameTime, float dayLengthSeconds)
        {
            float raw = GetLightIntensity(curGameTime, dayLengthSeconds);
            float t = (raw - LightIntensityMin) / (LightIntensityMax - LightIntensityMin);
            return GlobalLightIntensityMin + (GlobalLightIntensityMax - GlobalLightIntensityMin) * t;
        }

        /// <summary>
        /// 全局光色温（一天内关键帧线性插值，progress 0 与 1 同色保证周期闭合）：
        /// 午夜暗蓝 → 破晓暖橙 → 正午白 → 黄昏橙红 → 入夜紫蓝。
        /// Domain 层零 UnityEngine 依赖，故用自有 RGB 载体而非 Color。
        /// </summary>
        public static DayLightColor GetGlobalLightColor(double curGameTime, float dayLengthSeconds)
        {
            double progress = DayProgress(curGameTime, dayLengthSeconds);
            return GetGlobalLightColorByProgress(progress);
        }

        /// <summary>按一天进度取全局光色温（关键帧插值的纯实现，便于直接按 progress 单测）。</summary>
        public static DayLightColor GetGlobalLightColorByProgress(double progress)
        {
            // 关键帧与相位常量对齐：0=午夜暗蓝、0.20=破晓（Dawn 始）、0.30=早晨、0.50=正午白、
            // 0.70=午后（Dusk 始）、0.78=黄昏橙红、0.85=入夜、1.00=闭合回午夜暗蓝。
            for (int i = 0; i < ColorKeys.Length - 1; i++)
            {
                float left = ColorKeys[i].Progress;
                float right = ColorKeys[i + 1].Progress;
                if (progress >= left && progress < right)
                {
                    float t = (float)((progress - left) / (right - left));
                    return DayLightColor.Lerp(ColorKeys[i].Color, ColorKeys[i + 1].Color, t);
                }
            }

            // progress 落在 [0.85, 1.0) 之外只剩末段兜底：>= 最后关键帧（含 1.0）取闭合色。
            return ColorKeys[ColorKeys.Length - 1].Color;
        }

        /// <summary>色温关键帧表（Progress 升序，首末同色形成周期闭合）。</summary>
        private static readonly ColorKey[] ColorKeys =
        {
            new ColorKey(0.00f, new DayLightColor(0.55f, 0.65f, 1.00f)), // 午夜暗蓝
            new ColorKey(0.20f, new DayLightColor(1.00f, 0.82f, 0.65f)), // 破晓暖橙
            new ColorKey(0.30f, new DayLightColor(1.00f, 0.96f, 0.88f)), // 早晨
            new ColorKey(0.50f, new DayLightColor(1.00f, 1.00f, 1.00f)), // 正午白
            new ColorKey(0.70f, new DayLightColor(1.00f, 0.95f, 0.85f)), // 午后
            new ColorKey(0.78f, new DayLightColor(1.00f, 0.72f, 0.50f)), // 黄昏橙红
            new ColorKey(0.85f, new DayLightColor(0.62f, 0.70f, 1.00f)), // 入夜紫蓝
            new ColorKey(1.00f, new DayLightColor(0.55f, 0.65f, 1.00f)), // 闭合=午夜
        };

        private readonly struct ColorKey
        {
            public readonly float Progress;
            public readonly DayLightColor Color;

            public ColorKey(float progress, DayLightColor color)
            {
                this.Progress = progress;
                this.Color = color;
            }
        }

        /// <summary>
        /// 距目标相位下一次开始的秒数（永远指向未来的一次开始，含当前恰在目标相位中的场景：
        /// 此时指下一周期）。波次挂日（夜晚开波）、黎明提示等时间联动的统一入口。
        /// </summary>
        /// <example>
        /// 当前进度 0.175（午夜后半夜，属夜区间）：距今晚 0.80 开夜还有 (0.80-0.175)×dayLength；
        /// 当前进度 0.85（昨夜已开过波）：距明夜 (1.80-0.85)×dayLength。
        /// </example>
        public static float SecondsUntilPhaseStart(double curGameTime, float dayLengthSeconds, GamePhase targetPhase)
        {
            if (dayLengthSeconds <= 0f)
            {
                return 0f;
            }

            double start = PhaseStartProgress(targetPhase);
            double progress = DayProgress(curGameTime, dayLengthSeconds);
            double delta = progress < start ? start - progress : start + 1.0 - progress;
            return (float)(delta * dayLengthSeconds);
        }

        /// <summary>各相位的开始进度（与 GetPhaseByProgress 的区间下界一致）。</summary>
        private static double PhaseStartProgress(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Dawn: return NightEnd;
                case GamePhase.Day: return DawnEnd;
                case GamePhase.Dusk: return DayEnd;
                default: return NightStart;
            }
        }

        /// <summary>是否危险相位（夜晚）— 波次联动等消费方的便捷判定。</summary>
        public static bool IsNight(double curGameTime, float dayLengthSeconds)
        {
            return GetPhase(curGameTime, dayLengthSeconds) == GamePhase.Night;
        }

        /// <summary>钳制到 [min, max]。</summary>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }

    /// <summary>
    /// 昼夜光色温的纯数据载体（线性 RGB [0,1]）。Domain 层保持零 UnityEngine 依赖，
    /// 由表现层（DayNightLightManager）负责转换为 UnityEngine.Color。
    /// </summary>
    public readonly struct DayLightColor
    {
        public readonly float R;
        public readonly float G;
        public readonly float B;

        public DayLightColor(float r, float g, float b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        /// <summary>线性插值（t∈[0,1]，出界不钳制由调用方保证）。</summary>
        public static DayLightColor Lerp(DayLightColor a, DayLightColor b, float t)
        {
            return new DayLightColor(
                a.R + (b.R - a.R) * t,
                a.G + (b.G - a.G) * t,
                a.B + (b.B - a.B) * t);
        }
    }
}
