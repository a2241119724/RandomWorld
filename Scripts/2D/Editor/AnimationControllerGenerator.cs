namespace LAB2D.Editor
{
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    /// <summary>
    /// 为选中的 AnimationClip 生成单状态 AnimatorController（controller 资产名 = clip 名，
    /// 与 ItemData.Name 对应；状态名 = clip 名，供运行时 animator.Play(prefix) 随机相位）。
    /// 产物放在 clip 同目录，覆盖同名旧资产。
    /// </summary>
    public static class AnimationControllerGenerator
    {
        /// <summary>
        /// 右键菜单校验：选中内容含 AnimationClip 时可用。
        /// </summary>
        [MenuItem("Assets/为选中动画生成 Controller", true)]
        private static bool ValidateGenerate()
        {
            return Selection.objects.OfType<AnimationClip>().Any();
        }

        /// <summary>
        /// 为每个选中的 AnimationClip 生成同名 .controller（单状态、默认状态自动播放）。
        /// </summary>
        [MenuItem("Assets/为选中动画生成 Controller")]
        private static void Generate()
        {
            int created = 0;
            foreach (AnimationClip clip in Selection.objects.OfType<AnimationClip>())
            {
                string clipPath = AssetDatabase.GetAssetPath(clip);
                if (string.IsNullOrEmpty(clipPath))
                {
                    continue;
                }

                string dir = Path.GetDirectoryName(clipPath)?.Replace('\\', '/');
                string controllerPath = $"{dir}/{clip.name}.controller";

                // 覆盖已存在的同名 controller：先删旧资产再创建，保证状态机干净（与序列帧动画生成器同一套路）
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null
                    && !AssetDatabase.DeleteAsset(controllerPath))
                {
                    Debug.LogWarning($"[AnimCtrlGen] 删除旧 controller 失败，已跳过：{controllerPath}");
                    continue;
                }

                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                controller.AddMotion(clip);
                created++;
                Debug.Log($"[AnimCtrlGen] 生成：{controllerPath}（状态 {clip.name}，循环跟随 clip 的 Loop Time）");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[AnimCtrlGen] 完成：{created} 个 controller");
        }
    }
}
