namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Manager;

    /// <summary>
    /// 天气玩法工具类。
    /// 负责适配层映射（Manager → Domain 类型转换）和展示文本构建。
    /// 所有游戏规则逻辑委托给 WeatherGameplayRuleService。
    /// </summary>
    public static class WeatherGameplayTool
    {
        private static readonly WeatherGameplayRuleService RuleService = new WeatherGameplayRuleService();

        /// <summary>
        /// 将 Manager 层天气类型映射到领域层天气类型。
        /// </summary>
        private static WeatherType MapToDomain(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return WeatherType.Rain;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return WeatherType.Snow;
                default:
                    return WeatherType.Clear;
            }
        }

        /// <summary>
        /// 获取玩家移动速度倍率。
        /// </summary>
        public static float GetPlayerMoveSpeedMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            return RuleService.GetPlayerMoveSpeedMultiplier(MapToDomain(weather));
        }

        /// <summary>
        /// 获取工人移动速度倍率。
        /// </summary>
        public static float GetWorkerMoveSpeedMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            return RuleService.GetWorkerMoveSpeedMultiplier(MapToDomain(weather));
        }

        /// <summary>
        /// 获取工人工作进度倍率。
        /// </summary>
        public static float GetWorkerTaskProgressMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            return RuleService.GetWorkerTaskProgressMultiplier(MapToDomain(weather));
        }

        /// <summary>
        /// 获取环境灵气恢复倍率。
        /// </summary>
        public static float GetEnergyRecoveryMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            return RuleService.GetEnergyRecoveryMultiplier(MapToDomain(weather));
        }

        /// <summary>
        /// 按倍率计算数值，并保证结果不低于最小值。
        /// </summary>
        /// <param name="baseValue">基础值。</param>
        /// <param name="multiplier">倍率。</param>
        /// <param name="minValue">允许的最小值。</param>
        /// <returns>套用倍率后的安全值。</returns>
        public static float ApplyMultiplier(float baseValue, float multiplier, float minValue = 0.0f)
        {
            return RuleService.ApplyMultiplier(baseValue, multiplier, minValue);
        }

        /// <summary>
        /// 获取天气中文名称。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>用于 UI 和日志展示的中文名称。</returns>
        public static string GetWeatherName(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return "雨天";
                case WeatherManager.WeatherTypeEnum.Snow:
                    return "雪天";
                default:
                    return "晴天";
            }
        }

        /// <summary>
        /// 构建天气效果摘要。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>适合 Tip、HUD 和 Editor 菜单展示的摘要文本。</returns>
        public static string BuildEffectSummary(WeatherManager.WeatherTypeEnum weather)
        {
            return $"{GetWeatherName(weather)}效果\n" +
                $"玩家移动: {GetPlayerMoveSpeedMultiplier(weather):0.00}x\n" +
                $"工人移动: {GetWorkerMoveSpeedMultiplier(weather):0.00}x\n" +
                $"工人工作: {GetWorkerTaskProgressMultiplier(weather):0.00}x\n" +
                $"灵气恢复: {GetEnergyRecoveryMultiplier(weather):0.00}x";
        }
    }
}
