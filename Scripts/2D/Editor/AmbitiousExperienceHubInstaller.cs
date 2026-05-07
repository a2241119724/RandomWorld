namespace LAB2D
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// A001 体验中枢安装工具。
    /// 使用 Unity Editor 官方 API 创建场景节点和 Prefab，避免手写 Scene/Prefab YAML 破坏已有引用。
    /// </summary>
    public static class AmbitiousExperienceHubInstaller
    {
        private const string MenuRoot = "Tools/Agent/Ambitious/Experience Hub/";
        private const string SceneRootName = "Ambitious_A001_ExperienceHub_Root";
        private const string RuntimeRootName = "Ambitious_A001_ExperienceHub_Runtime";
        private const string PrefabFolder = "Assets/ResourcesLocal/Prefabs/UI/AmbitiousExperienceHub";
        private const string PrefabPath = PrefabFolder + "/Ambitious_A001_ExperienceHub.prefab";

        /// <summary>
        /// 在 Game.unity 中安装独立根节点。
        /// </summary>
        [MenuItem(MenuRoot + "Install Runtime Root In Game Scene", false, 1)]
        private static void InstallRuntimeRootInGameScene()
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
                root = CreateRoot(SceneRootName, true);
                created = true;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            string message = created
                ? "已在 Game.unity 中创建 Ambitious_A001_ExperienceHub_Root。"
                : "Game.unity 中已存在 Ambitious_A001_ExperienceHub_Root，未重复创建。";
            EditorUtility.DisplayDialog("Ambitious Experience Hub", message, "确定");
        }

        /// <summary>
        /// 在 ResourcesLocal 下生成完整 UI Prefab。
        /// </summary>
        [MenuItem(MenuRoot + "Create Prefab In ResourcesLocal", false, 2)]
        private static void CreatePrefabInResourcesLocal()
        {
            EnsureFolder(PrefabFolder);
            GameObject root = CreateRoot(SceneRootName, true);
            AmbitiousExperienceHub hub = root.GetComponent<AmbitiousExperienceHub>();
            hub.BuildPreviewForEditor();

            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, uniquePath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(uniquePath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Ambitious Experience Hub", $"已生成 Prefab：\n{uniquePath}", "确定");
        }

        /// <summary>
        /// 在当前场景创建可预览的根节点，不自动保存场景。
        /// </summary>
        [MenuItem(MenuRoot + "Preview Runtime Root In Current Scene", false, 3)]
        private static void PreviewRuntimeRootInCurrentScene()
        {
            GameObject root = GameObject.Find(SceneRootName);
            if (root == null)
            {
                root = CreateRoot(SceneRootName, true);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            AmbitiousExperienceHub hub = root.GetComponent<AmbitiousExperienceHub>();
            hub.BuildPreviewForEditor();
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorUtility.DisplayDialog("Ambitious Experience Hub", "已在当前场景创建或刷新预览根节点。", "确定");
        }

        /// <summary>
        /// 从当前场景移除 A001 根节点，用于回滚。
        /// </summary>
        [MenuItem(MenuRoot + "Remove Runtime Root From Current Scene", false, 20)]
        private static void RemoveRuntimeRootFromCurrentScene()
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
        private static GameObject CreateRoot(string objectName, bool buildPreview)
        {
            GameObject root = new GameObject(objectName);
            AmbitiousExperienceHub hub = root.AddComponent<AmbitiousExperienceHub>();
            hub.autoBuildOnAwake = true;
            hub.showHudOnStart = true;
            hub.showResultOnCapture = true;

            if (buildPreview)
            {
                hub.BuildPreviewForEditor();
            }

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
        /// 递归确保目标目录存在。
        /// </summary>
        private static void EnsureFolder(string folderPath)
        {
            string normalized = folderPath.Replace("\\", "/");
            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
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
