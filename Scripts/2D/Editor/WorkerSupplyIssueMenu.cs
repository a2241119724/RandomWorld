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
    /// 工人补给缺口 Editor 菜单。
    /// 提供运行时补给报告查看、提示开关和 Game.unity HUD 创建入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class WorkerSupplyIssueMenu
    {
        /// <summary>
        /// 查看当前补给缺口汇总。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "查看补给缺口汇总", false, 1)]
        private static void ShowSupplySummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人补给提示", "请在 Play Mode 中查看运行时补给缺口。", "确定");
                return;
            }

            WorkerSupplyReport report = WorkerSupplyIssueManager.Instance.Refresh(false);
            string summary = report.ToSummaryText();
            Debug.Log("<color=cyan>[工人补给提示]</color>\n" + summary);
            EditorUtility.DisplayDialog("工人补给提示", summary, "确定");
        }

        /// <summary>
        /// 启用补给缺口监控。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "启用补给缺口监控", false, 10)]
        private static void EnableMonitor()
        {
            WorkerSupplyIssueManager.Instance.Enable();
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用补给缺口监控。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "禁用补给缺口监控", false, 11)]
        private static void DisableMonitor()
        {
            WorkerSupplyIssueManager.Instance.Disable();
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用补给缺口 Tip。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "启用补给缺口 Tip", false, 20)]
        private static void EnableTip()
        {
            WorkerSupplyIssueManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用补给缺口 Tip。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "禁用补给缺口 Tip", false, 21)]
        private static void DisableTip()
        {
            WorkerSupplyIssueManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口 Tip 已禁用。", "确定");
        }

        /// <summary>
        /// 在 Game.unity 中创建独立补给缺口 HUD。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "创建补给缺口 HUD 到 Game 场景", false, 60)]
        private static void CreateHudInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("工人补给提示", "未找到 Game.unity，无法创建补给缺口 HUD。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            GameObject root = GameObject.Find(WorkerSupplyConstant.HudRootName);
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
                "工人补给提示",
                created ? "已在 Game.unity 中创建补给缺口 HUD。" : "补给缺口 HUD 已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 从当前场景移除补给缺口 HUD。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "从当前场景移除补给缺口 HUD", false, 61)]
        private static void RemoveHudFromCurrentScene()
        {
            GameObject root = GameObject.Find(WorkerSupplyConstant.HudRootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("工人补给提示", "当前场景没有补给缺口 HUD。", "确定");
                return;
            }

            Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("工人补给提示", "已移除补给缺口 HUD。", "确定");
        }

        /// <summary>
        /// 创建独立 Canvas。
        /// </summary>
        /// <returns>新建 Canvas。</returns>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(WorkerSupplyConstant.HudCanvasName);
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
                WorkerSupplyConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(WorkerSupplyHUD));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(20.0f, -360.0f);
            rootRect.sizeDelta = new Vector2(580.0f, 190.0f);

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
                WorkerSupplyConstant.HudTextName,
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

            text.fontSize = 15;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = WorkerSupplyConstant.EmptyHudText;

            WorkerSupplyHUD hud = root.GetComponent<WorkerSupplyHUD>();
            hud.supplyText = text;
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
