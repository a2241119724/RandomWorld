namespace LAB2D.Editor
{
    using LAB2D;
    using LAB2D.Domain.Worker;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 工人补给缺口 Editor 菜单。
    /// 提供运行时补给报告查看和提示开关；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class WorkerSupplyIssueMenu
    {
        /// <summary>
        /// 查看当前补给缺口汇总。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "查看补给缺口汇总", false, 420)]
        private static void ShowSupplySummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人补给提示", "请在 Play Mode 中查看运行时补给缺口。", "确定");
                return;
            }

            WorkerSupplyReport report = WorkerSupplyIssueManager.Instance.Refresh(false);
            string summary = report.ToSummaryText();
            Debug.Log("<color=cyan>[工人补给提示]</color>\n" + summary);
            EditorUtility.DisplayDialog("工人补给提示", summary, "确定");
        }

        /// <summary>
        /// 启用补给缺口监控。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "启用补给缺口监控", false, 421)]
        private static void EnableMonitor()
        {
            WorkerSupplyIssueManager.Instance.Enable();
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用补给缺口监控。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "禁用补给缺口监控", false, 422)]
        private static void DisableMonitor()
        {
            WorkerSupplyIssueManager.Instance.Disable();
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用补给缺口 Tip。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "启用补给缺口 Tip", false, 423)]
        private static void EnableTip()
        {
            WorkerSupplyIssueManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用补给缺口 Tip。
        /// </summary>
        [MenuItem(WorkerSupplyConstant.MenuRoot + "禁用补给缺口 Tip", false, 424)]
        private static void DisableTip()
        {
            WorkerSupplyIssueManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("工人补给提示", "工人补给缺口 Tip 已禁用。", "确定");
        }

    }
}
