namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 天气玩法工具类。
    /// 只负责根据天气类型计算通用倍率和展示文本，不持有运行时状态。
    /// 使用边界：不得在这里访问场景对象、Prefab、存档、Photon 或 AssetBundle。
    /// </summary>
    public static class WeatherGameplayTool
    {
        /// <summary>
        /// 获取玩家移动速度倍率。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>玩家移动速度倍率，1 表示不变化。</returns>
        public static float GetPlayerMoveSpeedMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return 0.92f;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return 0.84f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 获取工人移动速度倍率。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>工人移动速度倍率，1 表示不变化。</returns>
        public static float GetWorkerMoveSpeedMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return 0.9f;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return 0.78f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 获取工人工作进度倍率。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>工人任务进度倍率，数值越低表示工作越慢。</returns>
        public static float GetWorkerTaskProgressMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return 0.94f;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return 0.82f;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 获取环境灵气恢复倍率。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <returns>灵气恢复倍率，1 表示不变化。</returns>
        public static float GetEnergyRecoveryMultiplier(WeatherManager.WeatherTypeEnum weather)
        {
            switch (weather)
            {
                case WeatherManager.WeatherTypeEnum.Rain:
                    return 1.12f;
                case WeatherManager.WeatherTypeEnum.Snow:
                    return 0.86f;
                default:
                    return 1.05f;
            }
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
            return Mathf.Max(minValue, baseValue * Mathf.Max(0.0f, multiplier));
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
