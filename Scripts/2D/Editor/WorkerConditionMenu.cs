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
    /// 工人状态 Editor 菜单。
    /// 提供运行时状态查看、效果开关和 Game.unity HUD 创建入口。
    /// </summary>
    public static class WorkerConditionMenu
    {
        /// <summary>
        /// 查看当前工人状态汇总。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "查看状态汇总", false, 1)]
        private static void ShowConditionSummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人状态", "请在 Play Mode 中查看运行时工人状态。", "确定");
                return;
            }

            string summary = WorkerConditionManager.Instance.BuildSummaryText();
            Debug.Log("<color=cyan>[工人状态]</color>\n" + summary);
            EditorUtility.DisplayDialog("工人状态", summary, "确定");
        }

        /// <summary>
        /// 启用状态效率影响。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "启用状态效果", false, 10)]
        private static void EnableConditionEffect()
        {
            WorkerConditionManager.Instance.Enable();
            EditorUtility.DisplayDialog("工人状态", "工人状态效果已启用。", "确定");
        }

        /// <summary>
        /// 禁用状态效率影响。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "禁用状态效果", false, 11)]
        private static void DisableConditionEffect()
        {
            WorkerConditionManager.Instance.Disable();
            EditorUtility.DisplayDialog("工人状态", "工人状态效果已禁用，移动与工作倍率回到 1。", "确定");
        }

        /// <summary>
        /// 启用状态提示。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "启用状态提示", false, 20)]
        private static void EnableConditionTip()
        {
            WorkerConditionManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("工人状态", "工人状态 Tip 提示已启用。", "确定");
        }

        /// <summary>
        /// 禁用状态提示。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "禁用状态提示", false, 21)]
        private static void DisableConditionTip()
        {
            WorkerConditionManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("工人状态", "工人状态 Tip 提示已禁用。", "确定");
        }

        /// <summary>
        /// 在 Game.unity 中创建独立工人状态 HUD。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "创建工人状态 HUD 到 Game 场景", false, 60)]
        private static void CreateHudInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("工人状态", "未找到 Game.unity，无法创建工人状态 HUD。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            GameObject root = GameObject.Find(WorkerConditionConstant.HudRootName);
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
                "工人状态",
                created ? "已在 Game.unity 中创建工人状态 HUD。" : "工人状态 HUD 已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 从当前场景移除工人状态 HUD。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "从当前场景移除工人状态 HUD", false, 61)]
        private static void RemoveHudFromCurrentScene()
        {
            GameObject root = GameObject.Find(WorkerConditionConstant.HudRootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("工人状态", "当前场景没有工人状态 HUD。", "确定");
                return;
            }

            Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("工人状态", "已移除工人状态 HUD。", "确定");
        }

        /// <summary>
        /// 创建独立 Canvas。
        /// </summary>
        /// <returns>新建 Canvas。</returns>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(WorkerConditionConstant.HudCanvasName);
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
            GameObject root = new GameObject(
                WorkerConditionConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(WorkerConditionHUD));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(20.0f, -190.0f);
            rootRect.sizeDelta = new Vector2(520.0f, 150.0f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = PixelUITheme.DialogBoxBg;

            GameObject textObject = new GameObject(
                WorkerConditionConstant.HudTextName,
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12.0f, 8.0f);
            textRect.offsetMax = new Vector2(-12.0f, -8.0f);

            Text text = textObject.GetComponent<Text>();
            Font font = Resources.Load<Font>(WorkerConditionConstant.FontResourcePath);
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = WorkerConditionConstant.EmptyHudText;

            WorkerConditionHUD hud = root.GetComponent<WorkerConditionHUD>();
            hud.conditionText = text;
            hud.SetVisible(true);
            return root;
        }

        /// <summary>
        /// 查找 Game.unity 的真实路径。
        /// </summary>
        /// <returns>Game.unity 路径，找不到时返回空字符串。</returns>
        private static string FindGameScenePath()
        {
            string[] guids = AssetDatabase.FindAssets(WorkerConditionConstant.GameSceneName + " t:Scene", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == WorkerConditionConstant.GameSceneName)
                {
                    return path;
                }
            }

            return string.Empty;
        }
    }
}
