namespace LAB2D.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// A001 体验中枢安装工具。
    /// 使用 Unity Editor 官方 API 创建脚本根节点；UI 层级由 Game.unity 预置，不再由运行时代码生成。
    /// </summary>
    public static class AmbitiousExperienceHubInstaller
    {
        private const string MenuRoot = "工具/智能体/体验中枢/";
        private const string SceneRootName = "ExperienceHub_Root";
        private const string RuntimeRootName = "ExperienceHub_Runtime";

        /// <summary>
        /// 在 Game.unity 中安装独立根节点。
        /// </summary>
        [MenuItem(MenuRoot + "安装场景根节点到游戏场景", false, 1)]
        private static void InstallSceneRootInGameScene()
        {
            string scenePath = FindGameScenePath();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Ambitious Experience Hub", "未找到 Game.unity，无法安装场景根节点。", "确定");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find(SceneRootName);
            bool created = false;
            if (root == null)
            {
                root = CreateRoot(SceneRootName);
                created = true;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            string message = created
                ? "已在 Game.unity 中创建 ExperienceHub_Root。请确保其下已有预置 UI 层级。"
                : "Game.unity 中已存在 ExperienceHub_Root，未重复创建。";
            EditorUtility.DisplayDialog("Ambitious Experience Hub", message, "确定");
        }

        /// <summary>
        /// 从当前场景移除 A001 根节点，用于回滚。
        /// </summary>
        [MenuItem(MenuRoot + "从当前场景移除场景根节点", false, 20)]
        private static void RemoveSceneRootFromCurrentScene()
        {
            int removed = 0;
            removed += RemoveObjectByName(SceneRootName);
            removed += RemoveObjectByName(RuntimeRootName);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog(
                "Ambitious Experience Hub",
                removed > 0 ? $"已移除 {removed} 个 A001 根节点。" : "当前场景没有 A001 根节点。",
                "确定");
        }

        /// <summary>
        /// 创建根节点并挂载体验中枢脚本。
        /// </summary>
        private static GameObject CreateRoot(string objectName)
        {
            GameObject root = new GameObject(objectName);
            AmbitiousExperienceHub hub = root.AddComponent<AmbitiousExperienceHub>();
            hub.showHudOnStart = true;
            hub.showResultOnCapture = true;

            return root;
        }

        /// <summary>
        /// 查找 Game.unity 的真实路径。
        /// </summary>
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

        /// <summary>
        /// 按名称移除场景对象。
        /// </summary>
        private static int RemoveObjectByName(string objectName)
        {
            GameObject go = GameObject.Find(objectName);
            if (go == null)
            {
                return 0;
            }

            Object.DestroyImmediate(go);
            return 1;
        }
    }
}
