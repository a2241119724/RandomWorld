namespace LAB2D
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// 成就系统 Editor 菜单工具
    /// 用途：在 Unity Editor 中提供一键安装/移除成就系统 UI 的菜单项。
    /// 仅在 Editor 环境下编译，不会进入运行时构建。
    ///
    /// 菜单路径：工具/智能体/成就系统/
    /// </summary>
    public static class AchievementMenu
    {
        /// <summary>
        /// 安装成就系统 UI 到当前打开的 Game 场景
        /// 在场景中创建独立的成就 Canvas（弹窗 + 面板），不修改已有场景对象。
        /// 重复执行安全：已存在时跳过创建。
        /// </summary>
        [MenuItem(AchievementConstant.EditorMenuInstallToGame)]
        private static void InstallAchievementSystemToGame()
        {
            Scene gameScene = SceneManager.GetActiveScene();
            if (!gameScene.IsValid() || !gameScene.name.Contains("Game"))
            {
                Debug.LogWarning("[成就系统] 当前场景不是 Game 场景，请在 Game 场景中执行安装。"
                    + $"当前场景：{gameScene.name}");
                return;
            }

            // 创建弹窗 Canvas
            GameObject popupCanvas = GameObject.Find(AchievementConstant.PopupCanvasName);
            if (popupCanvas == null)
            {
                popupCanvas = AchievementTool.EnsureCanvas(AchievementConstant.PopupCanvasName, 200);
                Debug.Log($"[成就系统] 已创建弹窗 Canvas: {AchievementConstant.PopupCanvasName}");
            }
            else
            {
                Debug.Log($"[成就系统] 弹窗 Canvas 已存在，跳过创建: {AchievementConstant.PopupCanvasName}");
            }

            // 创建弹窗对象（若不存在）
            if (popupCanvas != null)
            {
                Transform existingPopup = popupCanvas.transform.Find(AchievementConstant.PopupRootName);
                if (existingPopup == null)
                {
                    GameObject popupObj = new GameObject(AchievementConstant.PopupRootName);
                    popupObj.transform.SetParent(popupCanvas.transform, false);
                    popupObj.AddComponent<AchievementPopup>();
                    Debug.Log($"[成就系统] 已创建弹窗对象: {AchievementConstant.PopupRootName}");
                }
            }

            // 创建面板 Canvas
            GameObject panelCanvas = GameObject.Find(AchievementConstant.PanelCanvasName);
            if (panelCanvas == null)
            {
                panelCanvas = AchievementTool.EnsureCanvas(AchievementConstant.PanelCanvasName, 150);
                Debug.Log($"[成就系统] 已创建面板 Canvas: {AchievementConstant.PanelCanvasName}");
            }
            else
            {
                Debug.Log($"[成就系统] 面板 Canvas 已存在，跳过创建: {AchievementConstant.PanelCanvasName}");
            }

            // 创建面板对象（若不存在）
            if (panelCanvas != null)
            {
                Transform existingPanel = panelCanvas.transform.Find(AchievementConstant.PanelRootName);
                if (existingPanel == null)
                {
                    GameObject panelObj = new GameObject(AchievementConstant.PanelRootName);
                    panelObj.transform.SetParent(panelCanvas.transform, false);
                    panelObj.AddComponent<AchievementPanel>();
                    Debug.Log($"[成就系统] 已创建面板对象: {AchievementConstant.PanelRootName}");
                }
            }

            Debug.Log("[成就系统] 安装完成！成就弹窗和面板已就绪。");
            Debug.Log("[成就系统] 提示：按 F7 键可打开/关闭成就面板。");
        }

        /// <summary>
        /// 从当前 Game 场景移除成就系统 UI
        /// 安全删除：只删除独立 Canvas，不触碰其他场景对象。
        /// </summary>
        [MenuItem(AchievementConstant.EditorMenuRemoveFromGame)]
        private static void RemoveAchievementSystemFromGame()
        {
            GameObject popupCanvas = GameObject.Find(AchievementConstant.PopupCanvasName);
            if (popupCanvas != null)
            {
                Object.DestroyImmediate(popupCanvas);
                Debug.Log($"[成就系统] 已移除弹窗 Canvas: {AchievementConstant.PopupCanvasName}");
            }

            GameObject panelCanvas = GameObject.Find(AchievementConstant.PanelCanvasName);
            if (panelCanvas != null)
            {
                Object.DestroyImmediate(panelCanvas);
                Debug.Log($"[成就系统] 已移除面板 Canvas: {AchievementConstant.PanelCanvasName}");
            }

            Debug.Log("[成就系统] 移除完成。");
        }

        /// <summary>
        /// 验证成就系统安装状态
        /// </summary>
        [MenuItem(AchievementConstant.EditorMenuRoot + "验证成就系统安装状态")]
        private static void ValidateInstallation()
        {
            bool popupOk = GameObject.Find(AchievementConstant.PopupCanvasName) != null;
            bool panelOk = GameObject.Find(AchievementConstant.PanelCanvasName) != null;
            bool mgrOk = AchievementManager.Instance != null;

            Debug.Log("[成就系统] 安装状态验证：");
            Debug.Log($"  弹窗 Canvas: {(popupOk ? "已安装" : "未安装")}");
            Debug.Log($"  面板 Canvas: {(panelOk ? "已安装" : "未安装")}");
            Debug.Log($"  管理器实例: {(mgrOk ? "已初始化" : "未初始化")}");

            EditorUtility.DisplayDialog(
                "成就系统安装状态",
                $"弹窗 Canvas: {(popupOk ? "[V] 已安装" : "[ ] 未安装")}\n"
                + $"面板 Canvas: {(panelOk ? "[V] 已安装" : "[ ] 未安装")}\n"
                + $"管理器实例: {(mgrOk ? "[V] 已初始化" : "[ ] 未初始化")}",
                "确定");
        }
    }
}
