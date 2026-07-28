namespace LAB2D.Editor
{
    using LAB2D;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 装备掉落系统 Editor 菜单 — 测试稀有度分布。
    /// </summary>
    public static class EquipmentLootMenu
    {
        /// <summary>
        /// 测试稀有度分布并打印到控制台。
        /// </summary>
        [MenuItem(EquipmentLootConstant.EditorMenuTestDrop, false, 240)]
        private static void TestRarityDistribution()
        {
            // 确保管理器已初始化（Singleton<T>.Instance 自动创建实例）
            if (!EnemyLootManager.Instance.IsInitialized)
            {
                EnemyLootManager.Instance.Initialize();
            }

            string report = "=== 装备稀有度掉落分布测试 ===\n\n";
            for (int wave = 0; wave <= 12; wave += 3)
            {
                report += EnemyLootManager.Instance.TestRarityDistribution(wave, 1000);
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
}
