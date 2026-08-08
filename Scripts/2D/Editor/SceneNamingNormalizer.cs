namespace LAB2D.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 场景对象命名规范化工具。
    /// 将当前场景中所有空名和重复通用名的 GameObject 重命名为 PascalCase 功能描述格式。
    ///
    /// 使用方式: 菜单 "工具/界面/规范化场景对象命名"
    /// 支持 Ctrl+Z 撤销。
    /// </summary>
    public static class SceneNamingNormalizer
    {
        private const string MenuPath = "工具/界面/规范化场景对象命名";

        /// <summary>
        /// 需要添加父节点前缀的通用名称集合
        /// </summary>
        private static readonly HashSet<string> GenericNames = new HashSet<string>
        {
            "Text",
            "Text (Legacy)",
            "Content",
            "Background",
            "Image",
            "Title",
            "Equipment",
            "Border",
            "Viewport",
            "Label",
            "TitleItem",
            "Handle",
            "Fill Area",
            "Fill",
            "Placeholder",
            "Bar",
            "Handle Slide Area",
            "Checkmark",
            "Cancel",
            "Tip",
            "Confirm",
            "Sliding Area",
            "SkillNameText",
            "SkillLevelText",
            "SkillHotkeyText",
            "Dropdown",
            "Arrow",
            "Scrollbar",
            "Item",
            "Item Background",
            "Item Label",
            "Item Checkmark",
            "Template",
            "Scroll View",
            "ScrollRect",
            "Scrollbar Horizontal",
            "Scrollbar Vertical",
            "Button",
            "Panel",
            "Toggle",
            "InputField",
            "Slider",
        };

        [MenuItem(MenuPath, false, 56)]
        private static void NormalizeSceneNaming()
        {
            Scene scene = SceneManager.GetActiveScene();
            string sceneName = scene.name;

            if (string.IsNullOrEmpty(sceneName) || !scene.IsValid())
            {
                Debug.LogError("[SceneNamingNormalizer] 当前没有有效场景，请先打开场景。");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "规范化场景对象命名",
                $"将为场景 \"{sceneName}\" 中所有对象规范化命名:\n\n" +
                "• 空名对象 → 根据父节点+组件类型自动生成名称\n" +
                "• 通用名对象 → 添加父节点前缀，消除歧义\n\n" +
                "⚠️ 建议先保存场景。可通过 Ctrl+Z 撤销全部操作。\n\n" +
                "是否继续?",
                "继续",
                "取消"))
            {
                return;
            }

            Undo.SetCurrentGroupName("规范化场景对象命名");
            int undoGroup = Undo.GetCurrentGroup();

            var log = new StringBuilder();
            log.AppendLine($"<color=cyan>═══ 场景命名规范化: {sceneName} ═══</color>");

            try
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();
                var stats = new RenameStats();

                foreach (GameObject root in rootObjects)
                {
                    ProcessTransform(root.transform, null, stats, log);
                }

                // 汇总
                log.AppendLine();
                log.AppendLine($"<color=yellow>─── 命名规范化完成 ───</color>");
                log.AppendLine($"  空名对象已命名: {stats.EmptyNamedCount}");
                log.AppendLine($"  通用名对象已重命名: {stats.GenericRenamedCount}");
                log.AppendLine($"  总计处理: {stats.EmptyNamedCount + stats.GenericRenamedCount} 个对象");
                log.AppendLine($"<color=cyan>══════════════════════════</color>");

                Debug.Log(log.ToString());

                Undo.CollapseUndoOperations(undoGroup);

                EditorUtility.DisplayDialog(
                    "规范化完成",
                    $"场景 \"{sceneName}\" 命名规范化完成:\n\n" +
                    $"• {stats.EmptyNamedCount} 个空名对象已命名\n" +
                    $"• {stats.GenericRenamedCount} 个通用名对象已重命名\n\n" +
                    $"详情请查看 Console 窗口。",
                    "确定");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneNamingNormalizer] 规范化失败: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("规范化失败", $"发生错误:\n{ex.Message}", "确定");
            }
        }

        private static void ProcessTransform(Transform t, Transform parent, RenameStats stats, StringBuilder log)
        {
            string currentName = t.gameObject.name;
            string newName = null;

            // 情况1: 空名对象
            if (string.IsNullOrEmpty(currentName))
            {
                newName = GenerateNameFromContext(t);
                if (!string.IsNullOrEmpty(newName))
                {
                    Undo.RecordObject(t.gameObject, "重命名空名对象");
                    t.gameObject.name = newName;
                    stats.EmptyNamedCount++;
                    log.AppendLine($"  <color=green>✔ 空名→</color> \"{newName}\"");
                }
            }
            // 情况2: 通用名对象 — 添加父节点前缀
            else if (IsGenericName(currentName) && t.parent != null)
            {
                string parentName = SanitizeName(t.parent.gameObject.name);
                if (!string.IsNullOrEmpty(parentName))
                {
                    string sanitized = SanitizeName(currentName);
                    newName = $"{parentName}_{sanitized}";

                    // 避免与兄弟节点重名
                    newName = EnsureUniqueName(t.parent, newName);

                    Undo.RecordObject(t.gameObject, "重命名通用名对象");
                    t.gameObject.name = newName;
                    stats.GenericRenamedCount++;
                    log.AppendLine($"  <color=yellow>✎ 通用名→</color> \"{currentName}\" → \"{newName}\"");
                }
            }

            // 递归处理子节点
            for (int i = 0; i < t.childCount; i++)
            {
                ProcessTransform(t.GetChild(i), t, stats, log);
            }
        }

        /// <summary>
        /// 为无名称对象根据上下文生成名称
        /// </summary>
        private static string GenerateNameFromContext(Transform t)
        {
            string prefix = string.Empty;

            // 优先使用父节点名称
            if (t.parent != null && !string.IsNullOrEmpty(t.parent.gameObject.name))
            {
                prefix = SanitizeName(t.parent.gameObject.name);
            }

            // 检测组件类型作为后缀
            string componentSuffix = GetPrimaryComponentName(t);

            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(componentSuffix))
            {
                return $"GameObject_{t.GetSiblingIndex()}";
            }

            if (string.IsNullOrEmpty(prefix))
            {
                return $"{componentSuffix}_{t.GetSiblingIndex()}";
            }

            if (string.IsNullOrEmpty(componentSuffix))
            {
                return $"{prefix}_Child_{t.GetSiblingIndex()}";
            }

            return $"{prefix}_{componentSuffix}";
        }

        /// <summary>
        /// 获取对象上最主要的 UI 组件类型名称
        /// </summary>
        private static string GetPrimaryComponentName(Transform t)
        {
            // 按优先级检查组件
            if (t.GetComponent<Button>() != null) return "Button";
            if (t.GetComponent<Toggle>() != null) return "Toggle";
            if (t.GetComponent<InputField>() != null) return "InputField";
            if (t.GetComponent<ScrollRect>() != null) return "ScrollRect";
            if (t.GetComponent<Slider>() != null) return "Slider";
            if (t.GetComponent<Dropdown>() != null) return "Dropdown";
            if (t.GetComponent<Scrollbar>() != null) return "Scrollbar";
            if (t.GetComponent<TMPro.TextMeshProUGUI>() != null) return "TMPText";
            if (t.GetComponent<Text>() != null) return "Text";
            if (t.GetComponent<RawImage>() != null) return "RawImage";
            if (t.GetComponent<Image>() != null) return "Image";
            if (t.GetComponent<RectTransform>() != null) return "Rect";
            if (t.GetComponent<CanvasRenderer>() != null) return "CanvasRender";

            return string.Empty;
        }

        /// <summary>
        /// 判断是否为需要处理的通用名称
        /// </summary>
        private static bool IsGenericName(string name)
        {
            return GenericNames.Contains(name);
        }

        /// <summary>
        /// 清理名称：去除空格和特殊字符，转为 PascalCase
        /// </summary>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            // 去除括号内容 "(Legacy)" → ""
            string cleaned = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(.*?\)", string.Empty);

            // 按空格/连字符/下划线拆分，每段首字母大写
            string[] parts = cleaned.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                    {
                        sb.Append(part.Substring(1));
                    }
                }
            }

            string result = sb.ToString();
            return string.IsNullOrEmpty(result) ? name : result;
        }

        /// <summary>
        /// 确保在父节点下名称唯一
        /// </summary>
        private static string EnsureUniqueName(Transform parent, string baseName)
        {
            string candidate = baseName;
            int suffix = 1;

            while (HasChildWithName(parent, candidate))
            {
                candidate = $"{baseName}_{suffix}";
                suffix++;
            }

            return candidate;
        }

        private static bool HasChildWithName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).gameObject.name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class RenameStats
        {
            public int EmptyNamedCount;
            public int GenericRenamedCount;
        }
    }
}
