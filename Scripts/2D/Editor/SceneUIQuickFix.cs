namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 当前场景 UI 快速修复工具。
    /// 仅修改"明确需要修改"的组件，跳过已对齐的。支持 Undo。
    ///
    /// 菜单: 工具/界面/快速修复当前场景
    /// 适合在打开 Game.unity 等大型场景后使用。
    /// </summary>
    public static class SceneUIQuickFix
    {
        private const string MenuPath = "工具/界面/快速修复当前场景";

        private static readonly ColorBlock ThemeButtonColors = new ColorBlock
        {
            normalColor      = new Color32(242, 160, 175, 255),
            highlightedColor = new Color32(252, 200, 213, 255),
            pressedColor     = new Color32(249, 213, 110, 255),
            selectedColor    = new Color32(126, 203, 154, 255),
            disabledColor    = new Color(0, 0, 0, 0),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };

        private static readonly ColorBlock ThemeInputColors = new ColorBlock
        {
            normalColor      = Color.white,
            highlightedColor = new Color32(252, 200, 213, 255),
            pressedColor     = new Color32(249, 213, 110, 255),
            selectedColor    = new Color32(252, 200, 213, 255),
            disabledColor    = new Color32(200, 200, 200, 128),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };

        [MenuItem(MenuPath, false, 5)]
        private static void QuickFix()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!EditorUtility.DisplayDialog(
                "快速修复 UI",
                $"将修复场景 \"{scene.name}\" 中的 UI 组件:\n\n" +
                "• 按钮颜色 → PixelUITheme\n" +
                "• 输入框颜色 → 主题色\n" +
                "• 开关颜色 → 主题色\n" +
                "• 文本字体 → ark-pixel (如已配置)\n\n" +
                "仅修改未对齐的组件，已正确配置的跳过。\n" +
                "支持 Ctrl+Z 撤销。",
                "执行",
                "取消"))
            {
                return;
            }

            Undo.SetCurrentGroupName("Scene UI Quick Fix");
            int group = Undo.GetCurrentGroup();

            int btnFixed = FixButtons();
            int inputFixed = FixInputFields();
            int toggleFixed = FixToggles();
            int fontFixed = FixFonts();

            Undo.CollapseUndoOperations(group);

            string msg = $"按钮: {btnFixed} | 输入框: {inputFixed} | 开关: {toggleFixed} | 字体: {fontFixed}";
            Debug.Log($"<color=cyan>[SceneUIQuickFix]</color> {scene.name} — {msg}");
            EditorUtility.DisplayDialog("快速修复完成", msg, "确定");
        }

        private static int FixButtons()
        {
            int count = 0;
            foreach (Button btn in UnityEngine.Object.FindObjectsOfType<Button>(true))
            {
                if (btn == null) continue;
                if (ColorsMatch(btn.colors.normalColor, ThemeButtonColors.normalColor) &&
                    ColorsMatch(btn.colors.highlightedColor, ThemeButtonColors.highlightedColor))
                    continue;

                Undo.RecordObject(btn, "Fix Button Colors");
                btn.colors = ThemeButtonColors;
                EditorUtility.SetDirty(btn);
                count++;
            }
            return count;
        }

        private static int FixInputFields()
        {
            int count = 0;
            foreach (InputField inf in UnityEngine.Object.FindObjectsOfType<InputField>(true))
            {
                if (inf == null) continue;
                if (ColorsMatch(inf.colors.highlightedColor, ThemeInputColors.highlightedColor))
                    continue;

                Undo.RecordObject(inf, "Fix Input Colors");
                inf.colors = ThemeInputColors;
                EditorUtility.SetDirty(inf);
                count++;
            }
            return count;
        }

        private static int FixToggles()
        {
            var colors = new ColorBlock
            {
                normalColor      = Color.white,
                highlightedColor = new Color32(252, 200, 213, 255),
                pressedColor     = new Color32(249, 213, 110, 255),
                selectedColor    = new Color32(126, 203, 154, 255),
                disabledColor    = new Color32(200, 200, 200, 128),
                colorMultiplier  = 1f,
                fadeDuration     = 0.1f,
            };

            int count = 0;
            foreach (Toggle tog in UnityEngine.Object.FindObjectsOfType<Toggle>(true))
            {
                if (tog == null) continue;
                if (ColorsMatch(tog.colors.selectedColor, colors.selectedColor))
                    continue;

                Undo.RecordObject(tog, "Fix Toggle Colors");
                tog.colors = colors;
                EditorUtility.SetDirty(tog);
                count++;
            }
            return count;
        }

        private static int FixFonts()
        {
            var config = AI.Dialogue.LLM.UIFontConfig.Instance;
            if (config == null || config.font == null) return 0;

            Font target = config.font;
            int count = 0;
            foreach (Text text in UnityEngine.Object.FindObjectsOfType<Text>(true))
            {
                if (text == null || text.font == target) continue;
                Undo.RecordObject(text, "Fix Font");
                text.font = target;
                EditorUtility.SetDirty(text);
                count++;
            }
            return count;
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            const float t = 0.005f;
            return Mathf.Abs(a.r - b.r) < t && Mathf.Abs(a.g - b.g) < t && Mathf.Abs(a.b - b.b) < t;
        }
    }
}
