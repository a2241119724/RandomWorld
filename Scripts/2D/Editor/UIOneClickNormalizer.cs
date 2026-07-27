namespace LAB2D.Editor
{
    using System;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 一键 UI 全面规范化工具。
    /// 按安全顺序依次执行: 字体 → 按钮 → 输入组件 → 圆角 → 审计报告。
    /// 每步使用 Undo 记录，支持完全撤销 (Ctrl+Z)。
    ///
    /// 使用方式: 菜单 "工具/界面/一键规范化当前场景 UI"
    /// 建议先保存场景 (Ctrl+S)，规范化后检查 Game View 确认无误再保存。
    /// </summary>
    public static class UIOneClickNormalizer
    {
        private const string MenuPath = "工具/界面/一键规范化当前场景 UI";

        [MenuItem(MenuPath, false, 0)]
        private static void NormalizeAll()
        {
            Scene scene = SceneManager.GetActiveScene();
            string sceneName = scene.name;

            if (!EditorUtility.DisplayDialog(
                "一键 UI 规范化",
                $"将规范化场景 \"{sceneName}\" 的所有 UI 组件:\n\n" +
                "1. 替换字体 (Arial → ark-pixel)\n" +
                "2. 对齐按钮颜色 (→ PixelUITheme)\n" +
                "3. 对齐输入组件颜色 (InputField/Toggle)\n" +
                "4. 调整圆角半径\n" +
                "5. 输出审计报告\n\n" +
                "⚠️ 建议先保存场景。可通过 Ctrl+Z 撤销全部操作。\n\n" +
                "是否继续?",
                "继续",
                "取消"))
            {
                return;
            }

            Undo.SetCurrentGroupName("UI 一键规范化");
            int undoGroup = Undo.GetCurrentGroup();

            var log = new StringBuilder();
            log.AppendLine($"<color=cyan>═══ UI 一键规范化: {sceneName} ═══</color>");

            try
            {
                // Step 1: 字体替换
                int fontCount = ApplyFontStep();
                log.AppendLine(fontCount > 0
                    ? $"  ✅ 字体: {fontCount} 个 Text 已更新"
                    : "  ⏭️ 字体: 无需更新 (已使用 ark-pixel)");

                // Step 2: 按钮颜色
                int btnCount = ApplyButtonStep();
                log.AppendLine(btnCount > 0
                    ? $"  ✅ 按钮: {btnCount} 个 Button 已更新"
                    : "  ⏭️ 按钮: 无需更新 (已对齐主题)");

                // Step 3: 输入组件颜色
                int inputCount = ApplyInputStep();
                log.AppendLine(inputCount > 0
                    ? $"  ✅ 输入: {inputCount} 个 InputField/Toggle 已更新"
                    : "  ⏭️ 输入: 无需更新 (已对齐主题)");

                // Step 4: 圆角
                int cornerCount = ApplyRoundCornerStep();
                log.AppendLine(cornerCount > 0
                    ? $"  ✅ 圆角: {cornerCount} 个已调整"
                    : "  ⏭️ 圆角: 无需调整");

                // Step 5: 审计
                log.AppendLine();
                log.AppendLine("<color=yellow>─── 规范化后审计 ───</color>");
                AppendQuickAudit(log);

                log.AppendLine();
                log.AppendLine("<color=cyan>═══ 规范化完成 ═══</color>");
                log.AppendLine("如有问题请 Ctrl+Z 撤销，或逐项运行 工具/界面/ 下的单项工具。");
            }
            catch (Exception ex)
            {
                log.AppendLine($"<color=red>❌ 错误: {ex.Message}</color>");
                log.AppendLine("已执行的操作可通过 Ctrl+Z 撤销。");
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(log.ToString());
        }

        private static int ApplyFontStep()
        {
            var config = AI.Dialogue.LLM.UIFontConfig.Instance;
            if (config == null || config.font == null)
            {
                Debug.LogWarning("[UIOneClick] 未找到 UIFontConfig，跳过字体步骤。请先运行 工具/LLM/创建 UI 字体配置。");
                return 0;
            }

            Font targetFont = config.font;
            int count = 0;
            foreach (Text text in UnityEngine.Object.FindObjectsOfType<Text>(true))
            {
                if (text.font == targetFont) continue;
                Undo.RecordObject(text, "Apply Font");
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                count++;
            }

            return count;
        }

        private static int ApplyButtonStep()
        {
            ColorBlock colors = new ColorBlock
            {
                normalColor      = new Color32(242, 160, 175, 255),
                highlightedColor = new Color32(252, 200, 213, 255),
                pressedColor     = new Color32(249, 213, 110, 255),
                selectedColor    = new Color32(126, 203, 154, 255),
                disabledColor    = new Color(0, 0, 0, 0),
                colorMultiplier  = 1f,
                fadeDuration     = 0.1f,
            };

            int count = 0;
            foreach (Button btn in UnityEngine.Object.FindObjectsOfType<Button>(true))
            {
                if (btn == null) continue;
                ColorBlock current = btn.colors;
                if (ColorsMatch(current.normalColor, colors.normalColor) &&
                    ColorsMatch(current.highlightedColor, colors.highlightedColor) &&
                    ColorsMatch(current.pressedColor, colors.pressedColor))
                {
                    continue; // 已对齐
                }

                Undo.RecordObject(btn, "Apply Button Colors");
                btn.colors = colors;
                EditorUtility.SetDirty(btn);
                count++;
            }

            return count;
        }

        private static int ApplyInputStep()
        {
            var inputColors = new ColorBlock
            {
                normalColor      = Color.white,
                highlightedColor = new Color32(252, 200, 213, 255),
                pressedColor     = new Color32(249, 213, 110, 255),
                selectedColor    = new Color32(252, 200, 213, 255),
                disabledColor    = new Color32(200, 200, 200, 128),
                colorMultiplier  = 1f,
                fadeDuration     = 0.1f,
            };

            var toggleColors = new ColorBlock
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
            foreach (Selectable sel in UnityEngine.Object.FindObjectsOfType<Selectable>(true))
            {
                if (sel == null) continue;

                if (sel is InputField inf)
                {
                    Undo.RecordObject(inf, "Apply Input Colors");
                    inf.colors = inputColors;
                    EditorUtility.SetDirty(inf);
                    count++;
                }
                else if (sel is Toggle tog)
                {
                    Undo.RecordObject(tog, "Apply Toggle Colors");
                    tog.colors = toggleColors;
                    EditorUtility.SetDirty(tog);
                    count++;
                }
            }

            return count;
        }

        private static int ApplyRoundCornerStep()
        {
            // 委托给现有 UITool 的逻辑
            var roundCorners = Resources.FindObjectsOfTypeAll(typeof(RoundCorner));
            int count = 0;
            foreach (var obj in roundCorners)
            {
                var rc = obj as RoundCorner;
                if (rc == null) continue;
                Undo.RecordObject(rc, "Apply Round Corner");
                rc.Radius = rc.name == "Background" ? 0.01f : 0.03f;
                EditorUtility.SetDirty(rc);
                count++;
            }

            return count;
        }

        private static void AppendQuickAudit(StringBuilder sb)
        {
            int textCount = UnityEngine.Object.FindObjectsOfType<Text>(true).Length;
            int btnCount = UnityEngine.Object.FindObjectsOfType<Button>(true).Length;
            int inputCount = UnityEngine.Object.FindObjectsOfType<InputField>(true).Length;
            int toggleCount = UnityEngine.Object.FindObjectsOfType<Toggle>(true).Length;
            sb.AppendLine($"  Text: {textCount} | Button: {btnCount} | InputField: {inputCount} | Toggle: {toggleCount}");
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            const float tol = 0.005f;
            return Math.Abs(a.r - b.r) < tol &&
                   Math.Abs(a.g - b.g) < tol &&
                   Math.Abs(a.b - b.b) < tol;
        }
    }
}
