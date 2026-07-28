namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 成就系统 Editor 菜单工具 — 验证 Game 场景中成就系统 UI 是否就绪。
    /// 仅在 Editor 环境下编译，不会进入运行时构建。
    ///
    /// 菜单路径：工具/成就/
    /// </summary>
    public static class AchievementMenu
    {
        /// <summary>
        /// 验证成就系统安装状态
        /// </summary>
        [MenuItem(AchievementConstant.EditorMenuRoot + "验证成就系统安装状态")]
        private static void ValidateInstallation()
        {
            bool popupOk = GameObject.Find(AchievementConstant.PopupRootName) != null;
            bool panelOk = GameObject.Find(AchievementConstant.PanelRootName) != null;
            bool mgrOk = AchievementManager.Instance != null;

            Debug.Log("[成就系统] 安装状态验证：");
            Debug.Log($"  弹窗: {(popupOk ? "已安装" : "未安装")}");
            Debug.Log($"  面板: {(panelOk ? "已安装" : "未安装")}");
            Debug.Log($"  管理器实例: {(mgrOk ? "已初始化" : "未初始化")}");

            EditorUtility.DisplayDialog(
                "成就系统安装状态",
                $"弹窗: {(popupOk ? "[V] 已安装" : "[ ] 未安装")}\n"
                + $"面板: {(panelOk ? "[V] 已安装" : "[ ] 未安装")}\n"
                + $"管理器实例: {(mgrOk ? "[V] 已初始化" : "[ ] 未初始化")}",
                "确定");
        }
    }
}
