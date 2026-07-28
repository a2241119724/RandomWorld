namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 天气玩法影响 Editor 菜单。
    /// 提供运行时状态查看和天气模拟。
    /// </summary>
    public static class WeatherGameplayEffectMenu
    {
        private const string MenuRoot = "工具/天气玩法影响/";
        private const string HudRootName = "WeatherGameplayHUDRoot";
        private const string HudTextName = "WeatherText";

        /// <summary>
        /// 查看当前天气玩法影响状态。
        /// </summary>
        [MenuItem(MenuRoot + "查看当前效果", false, 460)]
        private static void ShowCurrentEffect()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "请在 Play Mode 中查看运行时天气效果。", "确定");
                return;
            }

            WeatherGameplayEffect.Instance.Refresh();
            string summary = WeatherGameplayEffect.Instance.CurrentState.ToSummaryText();
            Debug.Log("<color=cyan>[天气玩法影响]</color>\n" + summary);
            EditorUtility.DisplayDialog("天气玩法影响", summary, "确定");
        }

        /// <summary>
        /// 启用天气玩法影响。
        /// </summary>
        [MenuItem(MenuRoot + "启用玩法影响", false, 461)]
        private static void EnableEffect()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "请在 Play Mode 中启用运行时效果。", "确定");
                return;
            }

            WeatherGameplayEffect.Instance.Enable();
            EditorUtility.DisplayDialog("天气玩法影响", "天气玩法影响已启用。", "确定");
        }

        /// <summary>
        /// 禁用天气玩法影响。
        /// </summary>
        [MenuItem(MenuRoot + "禁用玩法影响", false, 462)]
        private static void DisableEffect()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "请在 Play Mode 中禁用运行时效果。", "确定");
                return;
            }

            WeatherGameplayEffect.Instance.Disable();
            EditorUtility.DisplayDialog("天气玩法影响", "天气玩法影响已禁用，倍率已回到 1。", "确定");
        }

        /// <summary>
        /// 模拟晴天。
        /// </summary>
        [MenuItem(MenuRoot + "模拟天气/晴天", false, 463)]
        private static void SimulateSunny()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Sunny);
        }

        /// <summary>
        /// 模拟雨天。
        /// </summary>
        [MenuItem(MenuRoot + "模拟天气/雨天", false, 464)]
        private static void SimulateRain()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Rain);
        }

        /// <summary>
        /// 模拟雪天。
        /// </summary>
        [MenuItem(MenuRoot + "模拟天气/雪天", false, 465)]
        private static void SimulateSnow()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Snow);
        }

        /// <summary>
        /// 模拟指定天气。
        /// </summary>
        /// <param name="weather">目标天气。</param>
        private static void SimulateWeather(WeatherManager.WeatherTypeEnum weather)
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "请在 Play Mode 中模拟天气。", "确定");
                return;
            }

            WeatherManager manager = WeatherManager.Instance;
            if (manager == null)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "场景中没有 WeatherManager 实例。", "确定");
                return;
            }

            manager.SetWeather(weather);
            WeatherGameplayEffect.Instance.Refresh();
            EditorUtility.DisplayDialog("天气玩法影响", "已切换为" + WeatherGameplayTool.GetWeatherName(weather) + "。", "确定");
        }
    }
}
