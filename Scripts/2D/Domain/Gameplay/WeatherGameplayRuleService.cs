namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 天气玩法规则服务 — 自包含所有天气倍率常量和计算逻辑。
    /// 纯 C# 领域服务，无 Unity 依赖。
    /// </summary>
    public sealed class WeatherGameplayRuleService
    {
        // 玩家移动速度倍率常量
        private const float PlayerMoveRain = 0.92f;
        private const float PlayerMoveSnow = 0.84f;
        private const float PlayerMoveDefault = 1.0f;

        // 工人移动速度倍率常量
        private const float WorkerMoveRain = 0.9f;
        private const float WorkerMoveSnow = 0.78f;
        private const float WorkerMoveDefault = 1.0f;

        // 工人工作进度倍率常量
        private const float WorkerTaskRain = 0.94f;
        private const float WorkerTaskSnow = 0.82f;
        private const float WorkerTaskDefault = 1.0f;

        // 环境灵气恢复倍率常量
        private const float EnergyRecoveryRain = 1.12f;
        private const float EnergyRecoverySnow = 0.86f;
        private const float EnergyRecoveryDefault = 1.05f;

        // 工人疲劳衰减倍率常量（雨雪天加速疲劳，倾向于回家休息）
        private const float FatigueDecayRain = 1.2f;
        private const float FatigueDecaySnow = 1.5f;
        private const float FatigueDecayDefault = 1.0f;

        /// <summary>
        /// 获取玩家移动速度倍率。
        /// </summary>
        public float GetPlayerMoveSpeedMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return PlayerMoveRain;
                case WeatherType.Snow:
                    return PlayerMoveSnow;
                default:
                    return PlayerMoveDefault;
            }
        }

        /// <summary>
        /// 获取工人移动速度倍率。
        /// </summary>
        public float GetWorkerMoveSpeedMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return WorkerMoveRain;
                case WeatherType.Snow:
                    return WorkerMoveSnow;
                default:
                    return WorkerMoveDefault;
            }
        }

        /// <summary>
        /// 获取工人工作进度倍率。
        /// </summary>
        public float GetWorkerTaskProgressMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return WorkerTaskRain;
                case WeatherType.Snow:
                    return WorkerTaskSnow;
                default:
                    return WorkerTaskDefault;
            }
        }

        /// <summary>
        /// 获取环境灵气恢复倍率。
        /// </summary>
        public float GetEnergyRecoveryMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return EnergyRecoveryRain;
                case WeatherType.Snow:
                    return EnergyRecoverySnow;
                default:
                    return EnergyRecoveryDefault;
            }
        }

        /// <summary>
        /// 获取工人疲劳衰减倍率（雨雪天加速疲劳）。
        /// </summary>
        public float GetFatigueDecayMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    return FatigueDecayRain;
                case WeatherType.Snow:
                    return FatigueDecaySnow;
                default:
                    return FatigueDecayDefault;
            }
        }

        /// <summary>
        /// 按倍率计算数值，并保证结果不低于最小值。
        /// </summary>
        public float ApplyMultiplier(float baseValue, float multiplier, float minValue)
        {
            float safeMultiplier = multiplier < 0.0f ? 0.0f : multiplier;
            float value = baseValue * safeMultiplier;
            return value < minValue ? minValue : value;
        }
    }
}
