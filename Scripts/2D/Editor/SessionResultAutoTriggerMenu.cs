namespace LAB2D
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// SessionResultAutoTrigger 的 Editor 调试菜单。
    /// 提供状态查看、启用/禁用开关、手动触发模拟等功能。
    /// 所有功能在非 Play Mode 时自动降级提示。
    /// </summary>
    public static class SessionResultAutoTriggerMenu
    {
        private const string MenuRoot = "工具/结算自动触发";

        /// <summary>
        /// 获取当前活动的 SessionResultAutoTrigger 实例（可能为 null）
        /// </summary>
        private static SessionResultAutoTrigger GetInstance()
        {
            // 优先从场景中查找
            SessionResultAutoTrigger trigger = Object.FindObjectOfType<SessionResultAutoTrigger>();
            if (trigger == null)
            {
                trigger = SessionResultAutoTrigger.Instance;
            }

            return trigger;
        }

        [MenuItem(MenuRoot + "/查看状态", false, 1)]
        private static void ShowStatus()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger — 状态",
                    "当前未处于 Play Mode。\n\n" +
                    "SessionResultAutoTrigger 仅在运行时激活。\n" +
                    "请进入 Play Mode 后再次查看状态。\n\n" +
                    "提示：需将 SessionResultAutoTrigger 组件挂载到场景中某个 GameObject 上。\n" +
                    "如未挂载，Player.Death() 仍会直接调用 SessionResultManager.CaptureResult()。",
                    "确认");
                return;
            }

            SessionResultAutoTrigger trigger = GetInstance();
            string statusText;
            if (trigger != null)
            {
                statusText = trigger.GetStatusText();
            }
            else
            {
                statusText = "SessionResultAutoTrigger 实例不存在。\n\n" +
                             "当前模式：降级直连模式\n" +
                             "Player.Death() 会自动调用 SessionResultManager.CaptureResult() 但无事件分发。\n\n" +
                             "如需完整功能（波次事件订阅 + 事件分发 + Tip 反馈），请将 SessionResultAutoTrigger 组件挂载到场景 GameObject 上。";
            }

            EditorUtility.DisplayDialog("Session Auto Trigger — 状态", statusText, "确认");
        }

        [MenuItem(MenuRoot + "/模拟玩家死亡采集", false, 2)]
        private static void SimulatePlayerDeathCapture()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "请在 Play Mode 中使用此功能。",
                    "确认");
                return;
            }

            SessionResultAutoTrigger.NotifyPlayerDeath();
            EditorUtility.DisplayDialog(
                "Session Auto Trigger",
                "已触发 NotifyPlayerDeath()。\n请查看控制台输出和 Tip 提示。",
                "确认");
        }

        [MenuItem(MenuRoot + "/模拟波次通关采集", false, 3)]
        private static void SimulateWaveClearCapture()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "请在 Play Mode 中使用此功能。",
                    "确认");
                return;
            }

            SessionResultAutoTrigger trigger = GetInstance();
            if (trigger == null)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "SessionResultAutoTrigger 实例不存在，无法模拟波次通关采集。\n\n" +
                    "SessionResultAutoTrigger 需挂载到场景 GameObject 上才能订阅 WaveManager 事件。\n" +
                    "如仅需测试结算采集，请使用 '工具 > 结算结果 > 立即采集' 菜单。",
                    "确认");
                return;
            }

            // 直接调用 SessionResultManager.CaptureResult()
            SessionResultData result = SessionResultManager.Instance.CaptureResult();
            if (result != null)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger — 波次通关模拟",
                    $"已模拟波次通关采集：\n\n" +
                    $"评分: {result.CombatScore}\n" +
                    $"星级: {new string('★', result.StarRating)}{new string('☆', 5 - result.StarRating)}\n" +
                    $"等级: {result.GradeText}\n" +
                    $"击杀: {result.TotalDefeatedEnemyCount}\n" +
                    $"存活: {result.HasSurvived}\n\n" +
                    $"详细报告请查看控制台输出。",
                    "确认");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "采集失败。请确保 GameplaySessionStats 中有可用数据。\n" +
                    "提示：进入 Play Mode 后先击杀敌人，再触发采集。",
                    "确认");
            }
        }

        [MenuItem(MenuRoot + "/查看最新结果报告", false, 4)]
        private static void ShowLatestResultReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "请在 Play Mode 中使用此功能。",
                    "确认");
                return;
            }

            SessionResultData latest = SessionResultManager.Instance.LatestResult;
            if (latest == null)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "暂无结算记录。\n请先触发一次结算采集。",
                    "确认");
                return;
            }

            EditorUtility.DisplayDialog(
                "Session Auto Trigger — 最新结算报告",
                latest.GetReportText(),
                "确认");
        }

        [MenuItem(MenuRoot + "/清空全部结果", false, 20)]
        private static void ClearAllResults()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Session Auto Trigger",
                    "请在 Play Mode 中使用此功能。",
                    "确认");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Session Auto Trigger — 清空结算历史",
                    $"当前有 {SessionResultManager.Instance.HistoryCount} 条结算记录。\n确定清空所有历史记录？",
                    "确认清空",
                    "取消"))
            {
                return;
            }

            SessionResultManager.Instance.ClearHistory();
            EditorUtility.DisplayDialog(
                "Session Auto Trigger",
                "已清空所有结算历史记录。",
                "确认");
        }
    }
}
