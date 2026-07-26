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
    /// 工人任务队列 HUD Editor 菜单。
    /// 提供运行时摘要查看和 Game.unity HUD 创建入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class WorkerTaskQueueHUDMenu
    {
        /// <summary>
        /// 查看当前任务队列摘要。
        /// </summary>
        [MenuItem(WorkerTaskHudConstant.MenuRoot + "查看任务队列摘要", false, 1)]
        private static void ShowTaskQueueSummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("任务队列 HUD", "请在 Play Mode 中查看运行时任务队列。", "确定");
                return;
            }

            WorkerTaskManager manager = WorkerTaskManager.Instance;
            string summary = manager == null
                ? WorkerTaskHudConstant.ManagerUnavailableText
                : WorkerTaskSummaryTool.BuildPlainText(manager.CreateTaskQueueSnapshot());
            Debug.Log("<color=cyan>[任务队列 HUD]</color>\n" + summary);
            EditorUtility.DisplayDialog("任务队列 HUD", summary, "确定");
        }

        /// <summary>
        /// 在 Game.unity 中创建独立任务队列 HUD。
        /// </summary>
        [MenuItem(WorkerTaskHudConstant.MenuRoot + "创建任务队列 HUD 到 Game 场景", false, 60)]
        private static void CreateHudInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("任务队列 HUD", "未找到 Game.unity，无法创建任务队列 HUD。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            GameObject root = GameObject.Find(WorkerTaskHudConstant.HudRootName);
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
                "任务队列 HUD",
                created ? "已在 Game.unity 中创建任务队列 HUD。" : "任务队列 HUD 已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 从当前场景移除任务队列 HUD。
        /// </summary>
        [MenuItem(WorkerTaskHudConstant.MenuRoot + "从当前场景移除任务队列 HUD", false, 61)]
        private static void RemoveHudFromCurrentScene()
        {
            GameObject root = GameObject.Find(WorkerTaskHudConstant.HudRootName);
            if (root == null)
            {
                EditorUtility.DisplayDialog("任务队列 HUD", "当前场景没有任务队列 HUD。", "确定");
                return;
            }

            Object.DestroyImmediate(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("任务队列 HUD", "已移除任务队列 HUD。", "确定");
        }

        /// <summary>
        /// 创建独立 Canvas。
        /// </summary>
        /// <returns>新建 Canvas。</returns>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(WorkerTaskHudConstant.HudCanvasName);
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
                WorkerTaskHudConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(WorkerTaskQueueHUD));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(
                WorkerTaskHudConstant.HudAnchoredX,
                WorkerTaskHudConstant.HudAnchoredY);
            rootRect.sizeDelta = new Vector2(
                WorkerTaskHudConstant.HudWidth,
                WorkerTaskHudConstant.HudHeight);

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
                WorkerTaskHudConstant.HudTextName,
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(
                WorkerTaskHudConstant.HudPaddingX,
                WorkerTaskHudConstant.HudPaddingY);
            textRect.offsetMax = new Vector2(
                -WorkerTaskHudConstant.HudPaddingX,
                -WorkerTaskHudConstant.HudPaddingY);

            Text text = textObject.GetComponent<Text>();
            Font font = Resources.Load<Font>(WorkerConditionConstant.FontResourcePath);
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = WorkerTaskHudConstant.HudFontSize;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = WorkerTaskHudConstant.NoTaskText;

            WorkerTaskQueueHUD hud = root.GetComponent<WorkerTaskQueueHUD>();
            hud.queueText = text;
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
