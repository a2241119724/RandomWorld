namespace LAB2D.Editor
{
    using LAB2D;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单：玩家物品收集统计与里程碑调试工具。
    /// 提供 Play Mode 下的收集数据查看和里程碑管理入口。
    /// 菜单路径：工具/物品收集/
    /// </summary>
    public static class ItemCollectionMenu
    {
        private const string MenuRoot = "工具/物品收集/";

        [MenuItem(MenuRoot + "查看收集统计")]
        private static void ShowCollectionStats()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("物品收集统计", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            GameplaySessionStatsSnapshot snapshot = GameplaySessionStats.Instance.CreateSnapshot();
            ItemCollectionTracker tracker = ItemCollectionTracker.Instance;

            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("=== 物品收集统计 ===");
            sb.AppendLine();
            sb.AppendFormat("累计收集总数: {0}", snapshot.TotalCollectedItemCount).AppendLine();
            sb.AppendFormat("追踪器累计数: {0}", tracker.TotalCollected).AppendLine();
            sb.AppendLine();

            sb.AppendLine("--- 按物品类型统计 ---");
            if (snapshot.CollectedItemsByType.Count == 0)
            {
                sb.AppendLine("  （暂无数据）");
            }
            else
            {
                foreach (var kv in snapshot.CollectedItemsByType)
                {
                    sb.AppendFormat("  {0}: {1} 个", kv.Key, kv.Value).AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- 按物品 ID 统计 (前20) ---");
            int count = 0;
            foreach (var kv in snapshot.CollectedItemsById)
            {
                if (count >= 20)
                {
                    sb.AppendLine("  ... (更多数据请查看完整快照)");
                    break;
                }

                string itemName = "Unknown";
                if (ItemDataManager.Instance != null)
                {
                    ItemData data = ItemDataManager.Instance.GetById(kv.Key);
                    if (data != null)
                    {
                        itemName = data.CnName;
                    }
                }

                sb.AppendFormat("  [{0}] {1}: {2} 个", kv.Key, itemName, kv.Value).AppendLine();
                count++;
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("物品收集统计", sb.ToString(), "确定");
        }

        [MenuItem(MenuRoot + "查看里程碑")]
        private static void ShowMilestones()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("收集里程碑", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            ItemCollectionTracker tracker = ItemCollectionTracker.Instance;
            var reached = tracker.GetReachedMilestones();

            StringBuilder sb = new StringBuilder(256);
            sb.AppendLine("=== 收集里程碑 ===");
            sb.AppendFormat("当前累计收集: {0}", tracker.TotalCollected).AppendLine();
            sb.AppendLine();

            if (reached.Count == 0)
            {
                sb.AppendLine("  暂无已触达的里程碑。");
            }
            else
            {
                sb.AppendLine("已触达里程碑:");
                foreach (int milestone in reached)
                {
                    sb.AppendFormat("  ✓ {0} 个物品", milestone).AppendLine();
                }
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("收集里程碑", sb.ToString(), "确定");
        }

        [MenuItem(MenuRoot + "重置里程碑")]
        private static void ResetMilestones()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("重置里程碑", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "重置里程碑",
                "确定要重置所有收集里程碑追踪状态吗？\n（不会影响 GameplaySessionStats 中的历史数据）",
                "确定重置",
                "取消");

            if (confirm)
            {
                ItemCollectionTracker.Instance.ResetMilestones();
                EditorUtility.DisplayDialog("重置里程碑", "收集里程碑追踪状态已重置。", "确定");
            }
        }
    }
}
