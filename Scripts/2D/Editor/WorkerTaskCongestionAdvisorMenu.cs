namespace LAB2D.Editor
{
    using LAB2D.Domain.Worker;
    using LAB2D.Tool;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 工人任务队列拥堵提示 Editor 菜单。
    /// 提供运行时拥堵建议查看、监控开关和手动 Tip 触发入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class WorkerTaskCongestionAdvisorMenu
    {
        /// <summary>
        /// 查看当前任务队列拥堵建议。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "查看拥堵建议", false, 1)]
        private static void ShowCongestionAdvice()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("任务队列拥堵提示", "请在 Play Mode 中查看运行时任务队列拥堵建议。", "确定");
                return;
            }

            WorkerTaskCongestionReport report = WorkerTaskCongestionAdvisor.Instance.Refresh(false);
            string summary = report.ToSummaryText();
            Debug.Log("<color=cyan>[任务队列拥堵提示]</color>\n" + summary);
            EditorUtility.DisplayDialog("任务队列拥堵提示", summary, "确定");
        }

        /// <summary>
        /// 启用任务拥堵监控。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "启用拥堵监控", false, 10)]
        private static void EnableMonitor()
        {
            WorkerTaskCongestionAdvisor.Instance.Enable();
            EditorUtility.DisplayDialog("任务队列拥堵提示", "任务队列拥堵监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用任务拥堵监控。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "禁用拥堵监控", false, 11)]
        private static void DisableMonitor()
        {
            WorkerTaskCongestionAdvisor.Instance.Disable();
            EditorUtility.DisplayDialog("任务队列拥堵提示", "任务队列拥堵监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用任务拥堵 Tip。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "启用拥堵 Tip", false, 20)]
        private static void EnableTip()
        {
            WorkerTaskCongestionAdvisor.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("任务队列拥堵提示", "任务队列拥堵 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用任务拥堵 Tip。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "禁用拥堵 Tip", false, 21)]
        private static void DisableTip()
        {
            WorkerTaskCongestionAdvisor.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("任务队列拥堵提示", "任务队列拥堵 Tip 已禁用。", "确定");
        }

        /// <summary>
        /// 手动触发一次当前拥堵 Tip。
        /// </summary>
        [MenuItem(WorkerTaskCongestionConstant.MenuRoot + "立即触发一次拥堵 Tip", false, 30)]
        private static void ShowCurrentTip()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("任务队列拥堵提示", "请在 Play Mode 中触发运行时 Tip。", "确定");
                return;
            }

            bool shown = WorkerTaskCongestionAdvisor.Instance.TryShowCurrentTip();
            EditorUtility.DisplayDialog(
                "任务队列拥堵提示",
                shown ? "已请求显示一次任务队列拥堵 Tip。" : "当前任务队列未达到拥堵 Tip 条件。",
                "确定");
        }
    }
}
