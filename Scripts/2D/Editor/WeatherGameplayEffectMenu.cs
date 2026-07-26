namespace LAB2D.Editor
{
    using LAB2D;
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 天气玩法影响 Editor 菜单。
    /// 提供运行时状态查看、天气模拟和 Game.unity HUD 创建入口。
    /// </summary>
    public static class WeatherGameplayEffectMenu
    {
        private const string MenuRoot = "工具/天气玩法影响/";
        private const string HudRootName = "WeatherGameplayHUDRoot";
        private const string HudTextName = "WeatherText";

        /// <summary>
        /// 查看当前天气玩法影响状态。
        /// </summary>
        [MenuItem(MenuRoot + "查看当前效果", false, 1)]
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
        [MenuItem(MenuRoot + "启用玩法影响", false, 10)]
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
        [MenuItem(MenuRoot + "禁用玩法影响", false, 11)]
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
        [MenuItem(MenuRoot + "模拟天气/晴天", false, 30)]
        private static void SimulateSunny()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Sunny);
        }

        /// <summary>
        /// 模拟雨天。
        /// </summary>
        [MenuItem(MenuRoot + "模拟天气/雨天", false, 31)]
        private static void SimulateRain()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Rain);
        }

        /// <summary>
        /// 模拟雪天。
        /// </summary>
        [MenuItem(MenuRoot + "模拟天气/雪天", false, 32)]
        private static void SimulateSnow()
        {
            SimulateWeather(WeatherManager.WeatherTypeEnum.Snow);
        }

        /// <summary>
        /// 在 Game.unity 中创建独立天气 HUD。
        /// </summary>
        [MenuItem(MenuRoot + "创建天气 HUD 到 Game 场景", false, 60)]
        private static void CreateHudInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("天气玩法影响", "未找到 Game.unity，无法创建天气 HUD。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            GameObject root = GameObject.Find(HudRootName);
            bool created = false;
            if (root == null)
            {
                root = CreateHudRoot(canvas.transform);
                created = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog(
                "天气玩法影响",
                created ? "已在 Game.unity 中创建天气 HUD。" : "天气 HUD 已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 从当前场景移除天气 HUD。
        /// </summary>
        [MenuItem(MenuRoot + "从当前场景移除天气 HUD", false, 61)]
        private static void RemoveHudFromCurrentScene()
        {
            GameObject root = GameObject.Find(HudRootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("天气玩法影响", "当前场景没有天气 HUD。", "确定");
                return;
            }

            Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("天气玩法影响", "已移除天气 HUD。", "确定");
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

        /// <summary>
        /// 创建独立 Canvas。
        /// </summary>
        /// <returns>新建 Canvas。</returns>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("WeatherGameplayHUDCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// 创建 HUD 根节点。
        /// </summary>
        /// <param name="parent">Canvas 根节点。</param>
        /// <returns>HUD 根对象。</returns>
        private static GameObject CreateHudRoot(Transform parent)
        {
            GameObject root = new GameObject(HudRootName, typeof(RectTransform), typeof(CanvasGroup), typeof(WeatherGameplayHUD));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(20.0f, -96.0f);
            rootRect.sizeDelta = new Vector2(360.0f, 86.0f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = PixelUITheme.DialogBoxBg;

            GameObject textObject = new GameObject(HudTextName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12.0f, 8.0f);
            textRect.offsetMax = new Vector2(-12.0f, -8.0f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = "天气: 等待运行时数据";

            WeatherGameplayHUD hud = root.GetComponent<WeatherGameplayHUD>();
            hud.effectText = text;
            hud.SetVisible(true);
            return root;
        }

        /// <summary>
        /// 查找 Game.unity 的真实路径。
        /// </summary>
        /// <returns>Game.unity 路径，找不到时返回空字符串。</returns>
        private static string FindGameScenePath()
        {
            string[] guids = AssetDatabase.FindAssets("Game t:Scene", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "Game")
                {
                    return path;
                }
            }

            return string.Empty;
        }
    }
}
