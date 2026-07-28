namespace LAB2D.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// A006 殖民地运营指挥中心 Editor 菜单。
    /// 提供运行时报告查看、监控开关和 Tip 开关；Editor 专用逻辑不会进入运行时构建。
    /// </summary>
    public static class ColonyCommandCenterMenu
    {
        /// <summary>
        /// 查看当前指挥中心报告。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "查看当前指挥报告", false, 1)]
        private static void ShowCurrentReport()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("殖民地指挥中心", "请在 Play Mode 中查看运行时指挥报告。", "确定");
                return;
            }

            ColonyCommandCenterReport report = ColonyCommandCenterManager.Instance.Refresh(false);
            string summary = ColonyCommandCenterTool.BuildPlainText(report);
            Debug.Log("<color=cyan>[A006 殖民地指挥中心]</color>\n" + summary);
            EditorUtility.DisplayDialog("殖民地指挥中心", summary, "确定");
        }

        /// <summary>
        /// 启用指挥中心监控。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "启用指挥中心监控", false, 10)]
        private static void EnableMonitor()
        {
            ColonyCommandCenterManager.Instance.Enable();
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心监控已启用。", "确定");
        }

        /// <summary>
        /// 禁用指挥中心监控。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "禁用指挥中心监控", false, 11)]
        private static void DisableMonitor()
        {
            ColonyCommandCenterManager.Instance.Disable();
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心监控已禁用。", "确定");
        }

        /// <summary>
        /// 启用指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "启用指挥中心 Tip", false, 20)]
        private static void EnableTip()
        {
            ColonyCommandCenterManager.Instance.SetTipEnabled(true);
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心 Tip 已启用。", "确定");
        }

        /// <summary>
        /// 禁用指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "禁用指挥中心 Tip", false, 21)]
        private static void DisableTip()
        {
            ColonyCommandCenterManager.Instance.SetTipEnabled(false);
            EditorUtility.DisplayDialog("殖民地指挥中心", "A006 指挥中心 Tip 已禁用。", "确定");
        }

        /// <summary>
        /// 手动显示当前指挥中心 Tip。
        /// </summary>
        [MenuItem(ColonyCommandCenterConstant.MenuRoot + "调试/显示当前 Tip", false, 30)]
        private static void ShowCurrentTip()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("殖民地指挥中心", "请在 Play Mode 中触发 Tip。", "确定");
                return;
            }

            bool shown = ColonyCommandCenterManager.Instance.TryShowCurrentTip();
            EditorUtility.DisplayDialog(
                "殖民地指挥中心",
                shown ? "已请求显示当前指挥中心 Tip。" : "当前报告未达到提示等级。",
                "确定");
        }
    }
}
