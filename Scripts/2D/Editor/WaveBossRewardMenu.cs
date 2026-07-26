namespace LAB2D.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// A004 波次 Boss 与奖励 Editor 菜单。
    /// 提供运行时调试、奖励面板安装和场景 UI 回滚入口。
    /// </summary>
    public static class WaveBossRewardMenu
    {
        /// <summary>
        /// 查看运行时状态。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "查看当前状态", false, 1)]
        private static void ShowCurrentState()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中查看运行时状态。", "确定");
                return;
            }

            string summary = WaveBossRewardManager.Instance.CurrentState.ToSummaryText();
            Debug.Log("<color=cyan>[A004 波次Boss奖励]</color>\n" + summary);
            EditorUtility.DisplayDialog("波次Boss奖励", summary, "确定");
        }

        /// <summary>
        /// 启用系统。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "启用系统", false, 10)]
        private static void EnableSystem()
        {
            WaveBossRewardManager.Instance.Enable();
            EditorUtility.DisplayDialog("波次Boss奖励", "A004 系统已启用。", "确定");
        }

        /// <summary>
        /// 禁用系统。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "禁用系统", false, 11)]
        private static void DisableSystem()
        {
            WaveBossRewardManager.Instance.Disable();
            EditorUtility.DisplayDialog("波次Boss奖励", "A004 系统已禁用，奖励 Buff 已清空。", "确定");
        }

        /// <summary>
        /// 启用提示。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "启用提示", false, 20)]
        private static void EnableTip()
        {
            WaveBossRewardManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("波次Boss奖励", "Tip 提示已启用。", "确定");
        }

        /// <summary>
        /// 禁用提示。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "禁用提示", false, 21)]
        private static void DisableTip()
        {
            WaveBossRewardManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("波次Boss奖励", "Tip 提示已禁用。", "确定");
        }

        /// <summary>
        /// 模拟普通波奖励。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "调试/生成普通波奖励", false, 30)]
        private static void DebugCreateNormalReward()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中生成调试奖励。", "确定");
                return;
            }

            WaveBossRewardPanel.EnsureRuntimePanel();
            WaveBossRewardManager.Instance.CreateDebugRewardOptions(false);
        }

        /// <summary>
        /// 模拟 Boss 波奖励。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "调试/生成Boss波奖励", false, 31)]
        private static void DebugCreateBossReward()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "请在 Play Mode 中生成调试奖励。", "确定");
                return;
            }

            WaveBossRewardPanel.EnsureRuntimePanel();
            WaveBossRewardManager.Instance.CreateDebugRewardOptions(true);
        }

        /// <summary>
        /// 在 Game.unity 中创建奖励面板。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "创建奖励面板到 Game 场景", false, 60)]
        private static void CreatePanelInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("波次Boss奖励", "未找到 Game.unity，无法创建奖励面板。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            GameObject root = GameObject.Find(WaveBossRewardConstant.PanelRootName);
            bool created = false;
            if (root == null)
            {
                root = WaveBossRewardPanel.CreatePanelRoot(canvas.transform);
                created = true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog(
                "波次Boss奖励",
                created ? "已在 Game.unity 中创建 A004 奖励面板。" : "A004 奖励面板已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 从当前场景移除奖励面板。
        /// </summary>
        [MenuItem(WaveBossRewardConstant.MenuRoot + "从当前场景移除奖励面板", false, 61)]
        private static void RemovePanelFromCurrentScene()
        {
            int removed = 0;
            removed += RemoveObjectByName(WaveBossRewardConstant.PanelRootName);
            removed += RemoveObjectByName(WaveBossRewardConstant.CanvasName);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog(
                "波次Boss奖励",
                removed > 0 ? $"已移除 {removed} 个 A004 UI 对象。" : "当前场景没有 A004 奖励面板。",
                "确定");
        }

        /// <summary>
        /// 创建独立 Canvas。
        /// </summary>
        /// <returns>Canvas 组件。</returns>
        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                WaveBossRewardConstant.CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            return canvas;
        }

        /// <summary>
        /// 查找 Game.unity 的真实路径。
        /// </summary>
        /// <returns>场景路径，找不到时为空字符串。</returns>
        private static string FindGameScenePath()
        {
            string[] guids = AssetDatabase.FindAssets(WaveBossRewardConstant.GameSceneName + " t:Scene", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == WaveBossRewardConstant.GameSceneName)
                {
                    return path;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 按名称移除场景对象。
        /// </summary>
        /// <param name="objectName">对象名称。</param>
        /// <returns>移除数量。</returns>
        private static int RemoveObjectByName(string objectName)
        {
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject == null)
            {
                return 0;
            }

            Object.DestroyImmediate(gameObject);
            return 1;
        }
    }
}
