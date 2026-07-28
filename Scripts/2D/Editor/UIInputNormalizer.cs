namespace LAB2D.Editor
{
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 将当前场景中所有 InputField 和 Toggle 的颜色对齐 PixelUITheme。
    /// 不修改业务逻辑、事件绑定、文本内容或父子关系。
    ///
    /// 使用方式: 打开目标场景 → 菜单 "工具/界面/对齐输入组件颜色"
    /// 支持 Undo (Ctrl+Z)。
    /// </summary>
    public static class UIInputNormalizer
    {
        private const string MenuRoot = "工具/界面/";
        private const string RestoreInput = MenuRoot + "还原输入组件默认颜色";

        /// <summary>
        /// 默认 InputField/Toggle 颜色方案
        /// </summary>
        private static readonly ColorBlock DefaultInputColors = new ColorBlock
        {
            normalColor      = Color.white,
            highlightedColor = new Color32(245, 245, 245, 255),
            pressedColor     = new Color32(200, 200, 200, 255),
            selectedColor    = new Color32(245, 245, 245, 255),
            disabledColor    = new Color32(200, 200, 200, 128),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };

        private const string ExcludePattern = @"^$"; // 排除名称正则（默认不排除）

        [MenuItem(RestoreInput, false, 22)]
        private static void RestoreDefaults()
        {
            ApplyColors(DefaultInputColors, DefaultInputColors);
        }

        private static void ApplyColors(ColorBlock inputColors, ColorBlock toggleColors)
        {
            int inputCount = 0;
            int toggleCount = 0;

            // === InputField ===
            Selectable[] allSelectables = UnityEngine.Object.FindObjectsOfType<Selectable>(true);
            foreach (Selectable sel in allSelectables)
            {
                if (sel == null)
                {
                    continue;
                }

                if (Regex.IsMatch(sel.name, ExcludePattern) && ExcludePattern != @"^$")
                {
                    Debug.Log("[UIInputNormalizer] 排除: " + sel.name);
                    continue;
                }

                if (sel.GetComponent<ExcludeEditor>() != null)
                {
                    continue;
                }

                Undo.RecordObject(sel, "Normalize InputField/Toggle Colors");

                if (sel is InputField inputField)
                {
                    inputField.colors = inputColors;
                    EditorUtility.SetDirty(inputField);
                    inputCount++;
                }
                else if (sel is Toggle toggle)
                {
                    toggle.colors = toggleColors;
                    EditorUtility.SetDirty(toggle);
                    toggleCount++;
                }
            }

            Debug.Log($"[UIInputNormalizer] InputField: {inputCount} 个, Toggle: {toggleCount} 个 已处理。");
            EditorUtility.DisplayDialog(
                "完成",
                $"InputField: {inputCount} 个\nToggle: {toggleCount} 个\n\n颜色已更新。可通过 Ctrl+Z 撤销。",
                "确定");
        }
    }
}
