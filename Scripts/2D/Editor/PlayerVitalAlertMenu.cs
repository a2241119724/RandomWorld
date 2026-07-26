namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 玩家生命危险提示 Editor 菜单。
    /// 提供运行时生命报告查看、监控开关和手动 Tip 触发入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class PlayerVitalAlertMenu
    {
        /// <summary>
        /// 查看当前玩家生命提示报告。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "查看生命提示报告", false, 1)]
        private static void ShowVitalReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("玩家生命提示", "请在 Play Mode 中查看运行时玩家生命提示报告。", "确定");
                return;
            }

            PlayerVitalAlertReport report = PlayerVitalAlertManager.Instance.Refresh(false);
            string summary = report.ToSummaryText();
            Debug.Log("<color=cyan>[玩家生命提示]</color>\n" + summary);
            EditorUtility.DisplayDialog("玩家生命提示", summary, "确定");
        }

        /// <summary>
        /// 启用玩家生命监控。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "启用生命监控", false, 10)]
        private static void EnableMonitor()
        {
            PlayerVitalAlertManager.Instance.Enable();
            EditorUtility.DisplayDialog("玩家生命提示", "玩家生命监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用玩家生命监控。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "禁用生命监控", false, 11)]
        private static void DisableMonitor()
        {
            PlayerVitalAlertManager.Instance.Disable();
            EditorUtility.DisplayDialog("玩家生命提示", "玩家生命监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用玩家生命 Tip。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "启用生命 Tip", false, 20)]
        private static void EnableTip()
        {
            PlayerVitalAlertManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("玩家生命提示", "玩家生命 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用玩家生命 Tip。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "禁用生命 Tip", false, 21)]
        private static void DisableTip()
        {
            PlayerVitalAlertManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("玩家生命提示", "玩家生命 Tip 已禁用。", "确定");
        }

        /// <summary>
        /// 手动触发一次当前生命危险 Tip。
        /// </summary>
        [MenuItem(PlayerVitalAlertConstant.MenuRoot + "立即触发一次生命 Tip", false, 30)]
        private static void ShowCurrentTip()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("玩家生命提示", "请在 Play Mode 中触发运行时 Tip。", "确定");
                return;
            }

            bool shown = PlayerVitalAlertManager.Instance.TryShowCurrentTip();
            EditorUtility.DisplayDialog(
                "玩家生命提示",
                shown ? "已请求显示一次玩家生命危险 Tip。" : "当前玩家生命未达到危险 Tip 条件。",
                "确定");
        }
    }
}
