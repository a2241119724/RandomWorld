namespace LAB2D.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 当前场景 UI 组件审计报告工具。
    /// 扫描所有 UGUI 组件，检查颜色/字体/状态与 PixelUITheme 的一致性。
    ///
    /// 使用方式: 菜单 "工具/界面/生成 UI 审计报告"
    /// 报告输出到 Console 窗口 (带颜色标记)。
    /// </summary>
    public static class UIAuditReporter
    {
        private const string MenuRoot = "工具/界面/";
        private const string AuditCommand = MenuRoot + "生成 UI 审计报告";

        // === 主题参考值 ===
        private static readonly Color ThemeNormal     = new Color32(242, 160, 175, 255);  // #F2A0AF
        private static readonly Color ThemeHighlighted = new Color32(252, 200, 213, 255);  // #FCC8D5
        private static readonly Color ThemePressed     = new Color32(249, 213, 110, 255);  // #F9D56E
        private static readonly Color ThemeSelected    = new Color32(126, 203, 154, 255);  // #7ECB9A
        private static readonly Color ThemeTextPrimary = new Color32(74, 55, 40, 255);     // #4A3728
        private const float ColorTolerance = 0.005f;

        // Unity 默认 InputField 颜色
        private static readonly Color DefaultHighlighted = new Color32(245, 245, 245, 255);
        private static readonly Color DefaultPressed     = new Color32(200, 200, 200, 255);

        // 字体 GUID
        private const string ArkPixelGuid = "994464cadda06394eb1598617cdd2c57";
        private const string BuiltinGuid  = "0000000000000000e000000000000000";

        // === 数据结构 ===
        private sealed class AuditCounts
        {
            public int TotalText;
            public int TotalButton;
            public int TotalInputField;
            public int TotalToggle;
            public int TotalSlider;
            public int TotalScrollRect;
            public int TotalDropdown;

            // 颜色问题
            public int ButtonsWithDefaultColors;
            public int InputFieldsWithDefaultColors;
            public int TogglesWithDefaultColors;

            // 字体问题
            public int TextsWithArialFont;
            public int TextsWithPureBlack;

            // 良好状态
            public int TextsWithThemeFont;
            public int ButtonsWithThemeColors;
        }

        [MenuItem(AuditCommand, false, 55)]
        private static void GenerateReport()
        {
            var counts = new AuditCounts();
            var issueDetails = new List<string>();

            CollectTexts(counts, issueDetails);
            CollectSelectables(counts, issueDetails);

            // 构建报告
            var sb = new StringBuilder(2048);
            sb.AppendLine("<color=cyan>══════════════════════════════════════</color>");
            sb.AppendLine("<color=cyan>  UI 审计报告 — " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "</color>");
            sb.AppendLine("<color=cyan>══════════════════════════════════════</color>");
            sb.AppendLine();

            // 汇总
            sb.AppendLine("<color=yellow>【组件统计】</color>");
            sb.AppendLine($"  Text (Legacy):       {counts.TotalText}");
            sb.AppendLine($"  Button:              {counts.TotalButton}");
            sb.AppendLine($"  InputField:          {counts.TotalInputField}");
            sb.AppendLine($"  Toggle:              {counts.TotalToggle}");
            sb.AppendLine($"  Slider:              {counts.TotalSlider}");
            sb.AppendLine($"  ScrollRect:          {counts.TotalScrollRect}");
            sb.AppendLine($"  Dropdown:            {counts.TotalDropdown}");
            sb.AppendLine();

            // 字体
            sb.AppendLine("<color=yellow>【字体状态】</color>");
            sb.Append(counts.TextsWithThemeFont > 0
                ? $"  ✅ ark-pixel 字体: {counts.TextsWithThemeFont} 个\n"
                : "  ⚠️ 未检测到 ark-pixel 字体\n");
            sb.Append(counts.TextsWithArialFont > 0
                ? $"  ❌ Arial (内置):    {counts.TextsWithArialFont} 个 — 建议运行 工具/界面/修改文本\n"
                : "  ✅ 未检测到 Arial 字体\n");
            sb.Append(counts.TextsWithPureBlack > 0
                ? $"  ⚠️ 纯黑文本 (#000): {counts.TextsWithPureBlack} 个 — 建议改为 #4A3728\n"
                : "  ✅ 无纯黑文本\n");
            sb.AppendLine();

            // 颜色
            sb.AppendLine("<color=yellow>【颜色状态】</color>");
            sb.Append(counts.ButtonsWithThemeColors > 0
                ? $"  ✅ 主题色按钮: {counts.ButtonsWithThemeColors} 个\n"
                : "");
            sb.Append(counts.ButtonsWithDefaultColors > 0
                ? $"  ❌ 默认色按钮: {counts.ButtonsWithDefaultColors} 个 — 运行 工具/界面/修改按钮\n"
                : "  ✅ 按钮颜色已全部对齐\n");
            sb.Append(counts.InputFieldsWithDefaultColors > 0
                ? $"  ❌ 默认色输入框: {counts.InputFieldsWithDefaultColors} 个 — 运行 工具/界面/对齐输入组件颜色\n"
                : "  ✅ 输入框颜色已全部对齐\n");
            sb.Append(counts.TogglesWithDefaultColors > 0
                ? $"  ❌ 默认色开关: {counts.TogglesWithDefaultColors} 个 — 运行 工具/界面/对齐输入组件颜色\n"
                : "  ✅ 开关颜色已全部对齐\n");
            sb.AppendLine();

            // 问题明细 (最多 20 条)
            if (issueDetails.Count > 0)
            {
                sb.AppendLine("<color=yellow>【问题明细】</color>");
                int limit = Math.Min(issueDetails.Count, 20);
                for (int i = 0; i < limit; i++)
                {
                    sb.AppendLine($"  {issueDetails[i]}");
                }

                if (issueDetails.Count > 20)
                {
                    sb.AppendLine($"  ... 及其他 {issueDetails.Count - 20} 项 (完整列表见上方统计)");
                }
            }

            sb.AppendLine();
            sb.AppendLine("<color=cyan>══════════════════════════════════════</color>");
            sb.AppendLine("修复命令:");
            sb.AppendLine("  字体 → 工具/界面/修改文本 (或 LLM/应用全局字体到所有 UI)");
            sb.AppendLine("  按钮 → 工具/界面/修改按钮");
            sb.AppendLine("  输入 → 工具/界面/对齐输入组件颜色");
            sb.AppendLine("<color=cyan>══════════════════════════════════════</color>");

            Debug.Log(sb.ToString());
        }

        private static void CollectTexts(AuditCounts counts, List<string> details)
        {
            foreach (Text text in UnityEngine.Object.FindObjectsOfType<Text>(true))
            {
                if (text == null) continue;
                counts.TotalText++;

                string fontGuid = GetFontGuid(text);
                if (fontGuid == ArkPixelGuid)
                {
                    counts.TextsWithThemeFont++;
                }
                else if (fontGuid == BuiltinGuid || string.IsNullOrEmpty(fontGuid))
                {
                    counts.TextsWithArialFont++;
                    if (details.Count < 50)
                    {
                        details.Add($"<color=red>[字体]</color> {GetPath(text)} → Arial");
                    }
                }

                if (IsPureBlack(text.color))
                {
                    counts.TextsWithPureBlack++;
                    if (details.Count < 50)
                    {
                        details.Add($"<color=orange>[纯黑]</color> {GetPath(text)} → #000000");
                    }
                }
            }
        }

        private static void CollectSelectables(AuditCounts counts, List<string> details)
        {
            foreach (Selectable sel in UnityEngine.Object.FindObjectsOfType<Selectable>(true))
            {
                if (sel == null) continue;

                switch (sel)
                {
                    case Button btn:
                        counts.TotalButton++;
                        if (HasThemeButtonColors(btn.colors))
                            counts.ButtonsWithThemeColors++;
                        else
                        {
                            counts.ButtonsWithDefaultColors++;
                            if (details.Count < 50)
                                details.Add($"<color=red>[按钮]</color> {GetPath(btn)} → 默认色");
                        }
                        break;

                    case InputField inf:
                        counts.TotalInputField++;
                        if (HasDefaultSelectableColors(inf.colors))
                        {
                            counts.InputFieldsWithDefaultColors++;
                            if (details.Count < 50)
                                details.Add($"<color=red>[输入]</color> {GetPath(inf)} → 默认色");
                        }
                        break;

                    case Toggle tog:
                        counts.TotalToggle++;
                        if (HasDefaultSelectableColors(tog.colors))
                        {
                            counts.TogglesWithDefaultColors++;
                            if (details.Count < 50)
                                details.Add($"<color=red>[开关]</color> {GetPath(tog)} → 默认色");
                        }
                        break;

                    case Slider _:
                        counts.TotalSlider++;
                        break;

                    case Scrollbar _:
                        // 计入 ScrollRect 统计
                        break;

                    case Dropdown _:
                        counts.TotalDropdown++;
                        break;
                }
            }

            // ScrollRect 单独统计（不是 Selectable 子类）
            counts.TotalScrollRect = UnityEngine.Object.FindObjectsOfType<ScrollRect>(true).Length;
        }

        // === 辅助方法 ===

        private static string GetFontGuid(Text text)
        {
            if (text.font == null) return string.Empty;
            // Unity 的 Font 对象的 GUID 需要通过 AssetDatabase 获取
            string path = AssetDatabase.GetAssetPath(text.font);
            if (string.IsNullOrEmpty(path)) return string.Empty;
            return AssetDatabase.AssetPathToGUID(path);
        }

        private static string GetPath(Component comp)
        {
            if (comp == null) return "(null)";
            Transform t = comp.transform;
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool IsPureBlack(Color c)
        {
            return Math.Abs(c.r) < ColorTolerance
                && Math.Abs(c.g) < ColorTolerance
                && Math.Abs(c.b) < ColorTolerance
                && Math.Abs(c.a - 1f) < ColorTolerance;
        }

        private static bool HasThemeButtonColors(ColorBlock cb)
        {
            return ColorsMatch(cb.normalColor, ThemeNormal)
                && ColorsMatch(cb.highlightedColor, ThemeHighlighted)
                && ColorsMatch(cb.pressedColor, ThemePressed);
        }

        private static bool HasDefaultSelectableColors(ColorBlock cb)
        {
            return ColorsMatch(cb.highlightedColor, DefaultHighlighted)
                && ColorsMatch(cb.pressedColor, DefaultPressed);
        }

        private static bool ColorsMatch(Color a, Color b)
        {
            return Math.Abs(a.r - b.r) < ColorTolerance
                && Math.Abs(a.g - b.g) < ColorTolerance
                && Math.Abs(a.b - b.b) < ColorTolerance;
        }
    }
}
