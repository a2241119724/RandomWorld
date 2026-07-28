namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 工人任务队列 HUD Editor 菜单。
    /// 提供运行时摘要查看入口；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class WorkerTaskQueueHUDMenu
    {
        /// <summary>
        /// 查看当前任务队列摘要。
        /// </summary>
        [MenuItem(WorkerTaskHudConstant.MenuRoot + "查看任务队列摘要", false, 430)]
        private static void ShowTaskQueueSummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("任务队列 HUD", "请在 Play Mode 中查看运行时任务队列。", "确定");
                return;
            }

            WorkerTaskManager manager = WorkerTaskManager.Instance;
            string summary = manager == null
                ? WorkerTaskHudConstant.ManagerUnavailableText
                : WorkerTaskSummaryTool.BuildPlainText(manager.CreateTaskQueueSnapshot());
            Debug.Log("<color=cyan>[任务队列 HUD]</color>\n" + summary);
            EditorUtility.DisplayDialog("任务队列 HUD", summary, "确定");
        }

    }
}
