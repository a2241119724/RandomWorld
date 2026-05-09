namespace LAB2D
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// A006 殖民地运营指挥中心 Editor 菜单。
    /// 提供运行时报告查看、监控开关、Tip 开关、Game.unity UI 安装、ResourcesLocal Prefab 生成和场景 UI 回滚入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class ColonyCommandCenterMenu
    {
        /// <summary>
        /// 查看当前指挥中心报告。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "查看当前指挥报告", false, 1)]
        private static void ShowCurrentReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("殖民地指挥中心", "请在 Play Mode 中查看运行时指挥报告。", "确定");
                return;
            }

            ColonyCommandCenterReport report = ColonyCommandCenterManager.Instance.Refresh(false);
            string summary = ColonyCommandCenterTool.BuildPlainText(report);
            Debug.Log("<color=cyan>[A006 殖民地指挥中心]</color>\n" + summary);
            EditorUtility.DisplayDialog("殖民地指挥中心", summary, "确定");
        }

        /// <summary>
        /// 启用指挥中心监控。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "启用指挥中心监控", false, 10)]
        private static void EnableMonitor()
        {
            ColonyCommandCenterManager.Instance.Enable();
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用指挥中心监控。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "禁用指挥中心监控", false, 11)]
        private static void DisableMonitor()
        {
            ColonyCommandCenterManager.Instance.Disable();
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "启用指挥中心 Tip", false, 20)]
        private static void EnableTip()
        {
            ColonyCommandCenterManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "禁用指挥中心 Tip", false, 21)]
        private static void DisableTip()
        {
            ColonyCommandCenterManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心 Tip 已禁用。", "确定");
        }

        /// <summary>
        /// 手动显示当前指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "调试/显示当前 Tip", false, 30)]
        private static void ShowCurrentTip()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("殖民地指挥中心", "请在 Play Mode 中触发 Tip。", "确定");
                return;
            }

            bool shown = ColonyCommandCenterManager.Instance.TryShowCurrentTip();
            EditorUtility.DisplayDialog(
                "殖民地指挥中心",
                shown ? "已请求显示当前指挥中心 Tip。" : "当前报告未达到提示等级。",
                "确定");
        }

        /// <summary>
        /// 在 Game.unity 中创建独立指挥中心 HUD。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "创建指挥中心 HUD 到 Game 场景", false, 60)]
        private static void CreateHudInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("殖民地指挥中心", "未找到 Game.unity，无法创建指挥中心 HUD。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject canvasObject = GameObject.Find(ColonyCommandCenterConstant.CanvasName);
            if (canvasObject == null)
            {
                canvasObject = ColonyCommandCenterHUD.CreateCanvasObject();
            }

            GameObject root = GameObject.Find(ColonyCommandCenterConstant.HudRootName);
            bool created = false;
            if (root == null)
            {
                root = ColonyCommandCenterHUD.CreatePanelRoot(canvasObject.transform);
                created = true;
            }

            EnsureGraphicRaycaster(canvasObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog(
                "殖民地指挥中心",
                created ? "已在 Game.unity 中创建 A006 指挥中心 HUD。" : "A006 指挥中心 HUD 已存在，未重复创建。",
                "确定");
        }

        /// <summary>
        /// 创建 ResourcesLocal 指挥中心 HUD Prefab。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "创建 ResourcesLocal HUD Prefab", false, 61)]
        private static void CreateResourcesLocalPrefab()
        {
            Directory.CreateDirectory(ColonyCommandCenterConstant.PrefabFolderPath);
            GameObject canvasObject = ColonyCommandCenterHUD.CreateCanvasObject();
            ColonyCommandCenterHUD.CreatePanelRoot(canvasObject.transform);
            EnsureGraphicRaycaster(canvasObject);

            PrefabUtility.SaveAsPrefabAsset(canvasObject, ColonyCommandCenterConstant.PrefabAssetPath);
            Object.DestroyImmediate(canvasObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ColonyCommandCenterConstant.PrefabAssetPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EditorUtility.DisplayDialog(
                "殖民地指挥中心",
                "已生成 ResourcesLocal 指挥中心 HUD Prefab。\n" + ColonyCommandCenterConstant.PrefabAssetPath,
                "确定");
        }

        /// <summary>
        /// 从当前场景移除指挥中心 UI。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "从当前场景移除指挥中心 UI", false, 70)]
        private static void RemoveHudFromCurrentScene()
        {
            int removed = 0;
            removed += RemoveObjectByName(ColonyCommandCenterConstant.HudRootName);
            removed += RemoveObjectByName(ColonyCommandCenterConstant.CanvasName);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog(
                "殖民地指挥中心",
                removed > 0 ? $"已移除 {removed} 个 A006 UI 对象。" : "当前场景没有 A006 指挥中心 UI。",
                "确定");
        }

        /// <summary>
        /// 确保 Canvas 具备 GraphicRaycaster。
        /// </summary>
        /// <param name="canvasObject">Canvas 对象。</param>
        private static void EnsureGraphicRaycaster(GameObject canvasObject)
        {
            if (canvasObject == null)
            {
                return;
            }

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }
        }

        /// <summary>
        /// 查找 Game.unity 的真实路径。
        /// </summary>
        /// <returns>场景路径，找不到时为空字符串。</returns>
        private static string FindGameScenePath()
        {
            string[] guids = AssetDatabase.FindAssets(ColonyCommandCenterConstant.GameSceneName + " t:Scene", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == ColonyCommandCenterConstant.GameSceneName)
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
