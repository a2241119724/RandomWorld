namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 季节类型。
    /// </summary>
    public enum SeasonType
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    /// <summary>
    /// 温度规则服务 — 自包含所有温度常量和计算逻辑。
    /// 纯 C# 领域服务，无 Unity 依赖。
    /// 温度来源 = 季节基础 + 天气偏移 + 昼夜波动；房间温度 = 室外 + 保温 + 供暖功率。
    /// </summary>
    public sealed class TemperatureRuleService
    {
        // 季节长度（游戏天），可调：1 游戏天 = GlobalData.GameDayTime(1800) 真实秒
        public const int SeasonLengthDays = 5;

        // 季节基础温度（℃）
        public const float SpringBaseTemp = 18f;
        public const float SummerBaseTemp = 30f;
        public const float AutumnBaseTemp = 18f;
        public const float WinterBaseTemp = 2f;

        // 天气温度偏移（℃）
        public const float ClearOffset = 0f;
        public const float RainOffset = -6f;
        public const float SnowOffset = -12f;

        // 昼夜波动幅度（℃）
        public const float DayNightAmplitude = 4f;
        // 昼夜相位与 GameTimeUI 光照一致：sin((t/gameDaySeconds)*6.2624 - 1.55)
        public const float DayNightPhase = 6.2624f;
        public const float DayNightPhaseOffset = 1.55f;

        // 房间保温（℃）
        public const float RoomInsulationBonus = 6f;

        // 移动速度倍率：15~30℃ 舒适区间 1.0，每偏离 1℃ 惩罚 2%，clamp [0.5, 1.0]
        public const float MoveComfortMin = 15f;
        public const float MoveComfortMax = 30f;
        public const float MovePerDegreePenalty = 0.02f;
        public const float MoveMultiplierMin = 0.5f;
        public const float MoveMultiplierMax = 1.0f;

        // 疲劳消耗倍率：10~30℃ 舒适区间 1.0，每偏离 1℃ 加速 3%，clamp [1.0, 1.6]
        public const float FatigueComfortMin = 10f;
        public const float FatigueComfortMax = 30f;
        public const float FatiguePerDegreePenalty = 0.03f;
        public const float FatigueMultiplierMin = 1.0f;
        public const float FatigueMultiplierMax = 1.6f;

        /// <summary>
        /// 根据游戏天推算季节（0=春 1=夏 2=秋 3=冬，循环）。
        /// </summary>
        public SeasonType GetSeasonByGameDay(int gameDay)
        {
            int index = (gameDay / SeasonLengthDays) % 4;
            if (index < 0)
            {
                index += 4;
            }

            return (SeasonType)index;
        }

        /// <summary>
        /// 季节基础温度。
        /// </summary>
        public float GetBaseTemperature(SeasonType season)
        {
            switch (season)
            {
                case SeasonType.Summer:
                    return SummerBaseTemp;
                case SeasonType.Autumn:
                    return AutumnBaseTemp;
                case SeasonType.Winter:
                    return WinterBaseTemp;
                default:
                    return SpringBaseTemp;
            }
        }

        /// <summary>
        /// 天气温度偏移（雨雪天降温）。
        /// </summary>
        public float GetWeatherOffset(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return RainOffset;
                case WeatherType.Snow:
                    return SnowOffset;
                default:
                    return ClearOffset;
            }
        }

        /// <summary>
        /// 室外温度 = 季节基础 + 天气偏移 + 昼夜波动(±4℃)。
        /// 昼夜相位与 GameTimeUI 光照公式一致（sin((t/gameDaySeconds)*6.2624 - 1.55)）。
        /// </summary>
        /// <param name="curGameTime">累计真实游戏秒数（GameTimeManager.CurGameTime）。</param>
        /// <param name="gameDaySeconds">每游戏天的真实秒数（GlobalData.GameDayTime，默认 1800）。</param>
        /// <param name="weather">当前天气。</param>
        /// <returns>室外温度（℃）。</returns>
        public float GetOutdoorTemperature(double curGameTime, double gameDaySeconds, WeatherType weather)
        {
            double gameDay = curGameTime / gameDaySeconds;
            int gameDayInt = (int)gameDay;
            float baseTemp = this.GetBaseTemperature(this.GetSeasonByGameDay(gameDayInt));
            float weatherOffset = this.GetWeatherOffset(weather);
            float dayNight = (float)(System.Math.Sin((gameDay * DayNightPhase) - DayNightPhaseOffset) * DayNightAmplitude);
            return baseTemp + weatherOffset + dayNight;
        }

        /// <summary>
        /// 房间温度 = 室外 + 保温 + 供暖功率之和。
        /// </summary>
        public float GetRoomTemperature(float outdoorTemp, float heatPowerSum)
        {
            return outdoorTemp + RoomInsulationBonus + heatPowerSum;
        }

        /// <summary>
        /// 温度 → 移动速度倍率。15~30℃ 舒适 1.0；>30℃ 每高 1℃ 减 2%；&lt;15℃ 每低 1℃ 减 2%。clamp [0.5, 1.0]。
        /// </summary>
        public float GetMoveSpeedMultiplier(float temp)
        {
            if (temp >= MoveComfortMin && temp <= MoveComfortMax)
            {
                return 1.0f;
            }

            float penalty = temp > MoveComfortMax
                ? (temp - MoveComfortMax) * MovePerDegreePenalty
                : (MoveComfortMin - temp) * MovePerDegreePenalty;
            return this.Clamp(1.0f - penalty, MoveMultiplierMin, MoveMultiplierMax);
        }

        /// <summary>
        /// 温度 → 疲劳消耗倍率。10~30℃ 舒适 1.0；>30℃ 每高 1℃ 加速 3%；&lt;10℃ 每低 1℃ 加速 3%。clamp [1.0, 1.6]。
        /// </summary>
        public float GetFatigueDecayMultiplier(float temp)
        {
            if (temp >= FatigueComfortMin && temp <= FatigueComfortMax)
            {
                return 1.0f;
            }

            float penalty = temp > FatigueComfortMax
                ? (temp - FatigueComfortMax) * FatiguePerDegreePenalty
                : (FatigueComfortMin - temp) * FatiguePerDegreePenalty;
            return this.Clamp(1.0f + penalty, FatigueMultiplierMin, FatigueMultiplierMax);
        }

        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
