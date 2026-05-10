namespace LAB2D
{
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 装备掉落系统 Editor 菜单。
    /// 提供安装/卸载 UI 到 Game 场景、测试稀有度分布等功能。
    /// 所有操作都是安全的，不会覆盖已有对象或破坏场景。
    /// </summary>
    public static class EquipmentLootMenu
    {
        /// <summary>
        /// 安装装备掉落 UI 到 Game 场景。
        /// 创建独立 Canvas 节点用于装备面板和对比弹窗。
        /// 不修改已有场景对象，仅新增独立节点。
        /// </summary>
        [MenuItem(EquipmentLootConstant.EditorMenuInstall, false, 0)]
        private static void InstallUI()
        {
            // 检查 Game 场景是否已加载
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
            {
                EditorUtility.DisplayDialog(
                    "安装失败",
                    "请先打开 Game 场景，再执行安装。\n当前场景: " +
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    "确定");
                return;
            }

            // 创建装备面板 Canvas
            GameObject existingPanel = GameObject.Find(EquipmentLootConstant.EquipmentPanelCanvasName);
            if (existingPanel != null)
            {
                Debug.LogWarning("[EquipmentLootMenu] 装备面板 Canvas 已存在，跳过创建。");
            }
            else
            {
                GameObject panelCanvas = new GameObject(EquipmentLootConstant.EquipmentPanelCanvasName);
                Canvas canvas = panelCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = EquipmentLootConstant.EquipmentPanelSortingOrder;
                panelCanvas.AddComponent<CanvasScaler>();
                panelCanvas.AddComponent<GraphicRaycaster>();
                Debug.Log("[EquipmentLootMenu] 已创建装备面板 Canvas: " + EquipmentLootConstant.EquipmentPanelCanvasName);
            }

            // 创建对比弹窗 Canvas
            GameObject existingPopup = GameObject.Find(EquipmentLootConstant.ComparePopupCanvasName);
            if (existingPopup != null)
            {
                Debug.LogWarning("[EquipmentLootMenu] 对比弹窗 Canvas 已存在，跳过创建。");
            }
            else
            {
                GameObject popupCanvas = new GameObject(EquipmentLootConstant.ComparePopupCanvasName);
                Canvas canvas = popupCanvas.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = EquipmentLootConstant.ComparePopupSortingOrder;
                popupCanvas.AddComponent<CanvasScaler>();
                popupCanvas.AddComponent<GraphicRaycaster>();
                Debug.Log("[EquipmentLootMenu] 已创建对比弹窗 Canvas: " + EquipmentLootConstant.ComparePopupCanvasName);
            }

            EditorUtility.DisplayDialog(
                "安装完成",
                "装备掉落 UI 已安装到 Game 场景：\n" +
                "  - " + EquipmentLootConstant.EquipmentPanelCanvasName + " (排序层级 " + EquipmentLootConstant.EquipmentPanelSortingOrder + ")\n" +
                "  - " + EquipmentLootConstant.ComparePopupCanvasName + " (排序层级 " + EquipmentLootConstant.ComparePopupSortingOrder + ")\n\n" +
                "运行时自动创建 UI 内容。F9 打开装备面板。",
                "确定");
        }

        /// <summary>
        /// 从 Game 场景移除装备掉落 UI。
        /// </summary>
        [MenuItem(EquipmentLootConstant.EditorMenuUninstall, false, 1)]
        private static void UninstallUI()
        {
            int removed = 0;

            GameObject panelCanvas = GameObject.Find(EquipmentLootConstant.EquipmentPanelCanvasName);
            if (panelCanvas != null)
            {
                Object.DestroyImmediate(panelCanvas);
                removed++;
            }

            GameObject popupCanvas = GameObject.Find(EquipmentLootConstant.ComparePopupCanvasName);
            if (popupCanvas != null)
            {
                Object.DestroyImmediate(popupCanvas);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log("[EquipmentLootMenu] 已移除 " + removed + " 个装备掉落 UI 节点。");
                EditorUtility.DisplayDialog("卸载完成", "已从 Game 场景移除 " + removed + " 个装备掉落 UI 节点。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("卸载完成", "未找到装备掉落 UI 节点，无需移除。", "确定");
            }
        }

        /// <summary>
        /// 测试稀有度分布并打印到控制台。
        /// </summary>
        [MenuItem(EquipmentLootConstant.EditorMenuTestDrop, false, 2)]
        private static void TestRarityDistribution()
        {
            // 确保管理器已初始化（Singleton<T>.Instance 自动创建实例）
            if (!EquipmentLootManager.Instance.IsInitialized)
            {
                EquipmentLootManager.Instance.Initialize();
            }

            string report = "=== 装备稀有度掉落分布测试 ===\n\n";
            for (int wave = 0; wave <= 12; wave += 3)
            {
                report += EquipmentLootManager.Instance.TestRarityDistribution(wave, 1000);
                report += "\n\n";
            }

            report += "测试完成。数值受 EquipmentLootConstant 中权重配置影响。";
            Debug.Log(report);

            EditorUtility.DisplayDialog(
                "测试完成",
                "稀有度分布测试已完成，请查看 Unity Console 窗口输出。\n测试波次: 0, 3, 6, 9, 12 (每个 1000 次采样)",
                "确定");
        }
    }
    #endif
}
