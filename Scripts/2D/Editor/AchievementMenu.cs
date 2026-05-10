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
        /// 将弹窗和面板挂载到 UI/Foreground 下，复用 UI 的 Canvas。
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

            Transform foreground = AchievementTool.FindForeground();
            if (foreground == null)
            {
                Debug.LogError($"[成就系统] 无法找到 {TagConstant.UI_TAG}/Foreground 节点，安装失败");
                return;
            }

            // 创建弹窗对象（挂载到 UI/Foreground 下，复用 UI 的 Canvas）
            Transform existingPopup = foreground.Find(AchievementConstant.PopupRootName);
            if (existingPopup == null)
            {
                GameObject popupObj = new GameObject(AchievementConstant.PopupRootName);
                popupObj.transform.SetParent(foreground, false);
                popupObj.AddComponent<AchievementPopup>();
                Debug.Log($"[成就系统] 已创建弹窗对象: {AchievementConstant.PopupRootName}");
            }
            else
            {
                Debug.Log($"[成就系统] 弹窗对象已存在，跳过创建: {AchievementConstant.PopupRootName}");
            }

            // 创建面板对象（挂载到 UI/Foreground 下，复用 UI 的 Canvas）
            Transform existingPanel = foreground.Find(AchievementConstant.PanelRootName);
            if (existingPanel == null)
            {
                GameObject panelObj = new GameObject(AchievementConstant.PanelRootName);
                panelObj.transform.SetParent(foreground, false);
                panelObj.AddComponent<AchievementPanel>();
                Debug.Log($"[成就系统] 已创建面板对象: {AchievementConstant.PanelRootName}");
            }
            else
            {
                Debug.Log($"[成就系统] 面板对象已存在，跳过创建: {AchievementConstant.PanelRootName}");
            }

            Debug.Log("[成就系统] 安装完成！成就弹窗和面板已就绪。");
            Debug.Log("[成就系统] 提示：按 F7 键可打开/关闭成就面板。");
        }

        /// <summary>
        /// 从当前 Game 场景移除成就系统 UI
        /// 安全删除：只删除挂载在 UI/Foreground 下的成就节点，不触碰其他场景对象。
        /// </summary>
        [MenuItem(AchievementConstant.EditorMenuRemoveFromGame)]
        private static void RemoveAchievementSystemFromGame()
        {
            GameObject popupRoot = GameObject.Find(AchievementConstant.PopupRootName);
            if (popupRoot != null)
            {
                Object.DestroyImmediate(popupRoot);
                Debug.Log($"[成就系统] 已移除弹窗: {AchievementConstant.PopupRootName}");
            }

            GameObject panelRoot = GameObject.Find(AchievementConstant.PanelRootName);
            if (panelRoot != null)
            {
                Object.DestroyImmediate(panelRoot);
                Debug.Log($"[成就系统] 已移除面板: {AchievementConstant.PanelRootName}");
            }

            Debug.Log("[成就系统] 移除完成。");
        }

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
