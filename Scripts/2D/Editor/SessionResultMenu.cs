namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单工具：会话结算数据的采集、查看和管理。
    /// 菜单路径：工具 > 结算
    ///
    /// 功能：
    ///   - Capture Now：从当前 GameplaySessionStats 采集一次结算数据
    ///   - Show Latest：显示最近一次结算的详细报告
    ///   - Show History：显示所有历史结算的汇总列表
    ///   - Clear History：清空历史记录
    /// </summary>
    public static class SessionResultMenu
    {
        private const string MenuRoot = "工具/结算/";

        [MenuItem(MenuRoot + "立即采集", false, 300)]
        private static void CaptureNow()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "请在 Play Mode 中使用此功能。\n结算数据仅在运行时生成。",
                    "OK");
                return;
            }

            SessionResultData result = SessionResultManager.Instance.CaptureResult();
            if (result == null)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "采集失败。请确认 GameplaySessionStats 实例已初始化。",
                    "OK");
                return;
            }

            Debug.Log("<color=cyan>[SessionResult]</color> 结算数据已采集。\n" + result.GetReportText());
            EditorUtility.DisplayDialog(
                "Session Result — 采集成功",
                $"评分：{result.CombatScore} / 10000\n" +
                $"星级：{new string('★', result.StarRating)}{new string('☆', 5 - result.StarRating)}\n" +
                $"评级：{result.GradeText}\n" +
                $"击杀：{result.TotalDefeatedEnemyCount} | 连击：{result.MaxCombo}\n\n" +
                $"详细报告已输出到 Console 窗口。",
                "OK");
        }

        [MenuItem(MenuRoot + "查看最新", false, 301)]
        private static void ShowLatest()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "请在 Play Mode 中使用此功能。\n结算数据仅在运行时存在。",
                    "OK");
                return;
            }

            SessionResultData latest = SessionResultManager.Instance.LatestResult;
            if (latest == null)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "暂无结算数据。\n请先使用 工具 > 结算结果 > 立即采集 采集数据。",
                    "OK");
                return;
            }

            Debug.Log("<color=cyan>[SessionResult]</color>\n" + latest.GetReportText());
            EditorUtility.DisplayDialog(
                "Session Result — Latest",
                latest.GetReportText(),
                "OK");
        }

        [MenuItem(MenuRoot + "查看历史", false, 302)]
        private static void ShowHistory()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "请在 Play Mode 中使用此功能。\n结算历史仅在运行时存在。",
                    "OK");
                return;
            }

            string summary = SessionResultManager.Instance.GetHistorySummaryText();
            Debug.Log("<color=cyan>[SessionResult History]</color>\n" + summary);
            EditorUtility.DisplayDialog(
                "Session Result — History",
                summary,
                "OK");
        }

        [MenuItem(MenuRoot + "清空历史", false, 303)]
        private static void ClearHistory()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Result",
                    "请在 Play Mode 中使用此功能。",
                    "OK");
                return;
            }

            SessionResultManager.Instance.ClearHistory();
            Debug.Log("<color=cyan>[SessionResult]</color> 结算历史已清空。");
            EditorUtility.DisplayDialog(
                "Session Result",
                "结算历史已清空。",
                "OK");
        }
    }
}
