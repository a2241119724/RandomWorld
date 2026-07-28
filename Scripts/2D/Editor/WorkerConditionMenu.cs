namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 工人状态 Editor 菜单。
    /// 提供运行时状态查看和效果开关。
    /// </summary>
    public static class WorkerConditionMenu
    {
        /// <summary>
        /// 查看当前工人状态汇总。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "查看状态汇总", false, 410)]
        private static void ShowConditionSummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人状态", "请在 Play Mode 中查看运行时工人状态。", "确定");
                return;
            }

            string summary = WorkerConditionManager.Instance.BuildSummaryText();
            Debug.Log("<color=cyan>[工人状态]</color>\n" + summary);
            EditorUtility.DisplayDialog("工人状态", summary, "确定");
        }

        /// <summary>
        /// 启用状态效率影响。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "启用状态效果", false, 411)]
        private static void EnableConditionEffect()
        {
            WorkerConditionManager.Instance.Enable();
            EditorUtility.DisplayDialog("工人状态", "工人状态效果已启用。", "确定");
        }

        /// <summary>
        /// 禁用状态效率影响。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "禁用状态效果", false, 412)]
        private static void DisableConditionEffect()
        {
            WorkerConditionManager.Instance.Disable();
            EditorUtility.DisplayDialog("工人状态", "工人状态效果已禁用，移动与工作倍率回到 1。", "确定");
        }

        /// <summary>
        /// 启用状态提示。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "启用状态提示", false, 413)]
        private static void EnableConditionTip()
        {
            WorkerConditionManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("工人状态", "工人状态 Tip 提示已启用。", "确定");
        }

        /// <summary>
        /// 禁用状态提示。
        /// </summary>
        [MenuItem(WorkerConditionConstant.MenuRoot + "禁用状态提示", false, 414)]
        private static void DisableConditionTip()
        {
            WorkerConditionManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("工人状态", "工人状态 Tip 提示已禁用。", "确定");
        }

    }
}
