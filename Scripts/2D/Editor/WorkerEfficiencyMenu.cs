namespace LAB2D
{
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Editor 菜单：工人工作效率统计与调试工具。
    /// 提供 Play Mode 下的效率数据查看、最高效 Worker 查询和任务分布分析入口。
    /// 菜单路径：工具/工人效率/
    /// </summary>
    public static class WorkerEfficiencyMenu
    {
        private const string MenuRoot = "工具/工人效率/";

        [MenuItem(MenuRoot + "查看效率汇总")]
        private static void ShowEfficiencySummary()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人工作效率", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            WorkerEfficiencyTracker tracker = WorkerEfficiencyTracker.Instance;
            if (tracker == null)
            {
                EditorUtility.DisplayDialog("工人工作效率", "WorkerEfficiencyTracker 实例不可用。", "确定");
                return;
            }

            string summary = tracker.BuildSummaryText();
            EditorUtility.DisplayDialog("工人工作效率报告", summary, "确定");
        }

        [MenuItem(MenuRoot + "查看最高效工人")]
        private static void ShowMostProductiveWorker()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人工作效率", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            WorkerEfficiencyTracker tracker = WorkerEfficiencyTracker.Instance;
            if (tracker == null)
            {
                EditorUtility.DisplayDialog("工人工作效率", "WorkerEfficiencyTracker 实例不可用。", "确定");
                return;
            }

            WorkerEfficiencyTracker.WorkerEfficiencyRecord best = tracker.GetMostProductiveWorker();
            if (best == null)
            {
                EditorUtility.DisplayDialog("最高效 Worker", "暂无 Worker 效率记录。", "确定");
                return;
            }

            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("=== 最高效 Worker ===");
            sb.AppendLine();
            sb.AppendFormat("名称: {0}", best.WorkerName).AppendLine();
            sb.AppendFormat("实例ID: {0}", best.WorkerInstanceId).AppendLine();
            sb.AppendFormat("存活: {0}", best.IsAlive ? "是" : "否").AppendLine();
            sb.AppendFormat("完成任务总数: {0}", best.TotalTasksCompleted).AppendLine();
            sb.AppendFormat("预估速率: {0:F2} 任务/分钟", best.GetTasksPerMinute()).AppendLine();
            sb.AppendFormat("最常见任务: {0}", best.GetMostFrequentTaskType()).AppendLine();
            sb.AppendFormat("累计预计耗时: {0:F1} 秒", best.TotalEstimatedWorkTime).AppendLine();
            sb.AppendFormat("死亡次数: {0}", best.DeathCount).AppendLine();
            sb.AppendLine();
            sb.AppendLine("任务类型分布:");
            foreach (var kv in best.TasksByType)
            {
                sb.AppendFormat("  {0}: {1}", kv.Key, kv.Value).AppendLine();
            }

            EditorUtility.DisplayDialog("最高效 Worker", sb.ToString(), "确定");
        }

        [MenuItem(MenuRoot + "查看工人列表")]
        private static void ShowWorkerList()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人工作效率", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            WorkerEfficiencyTracker tracker = WorkerEfficiencyTracker.Instance;
            if (tracker == null)
            {
                EditorUtility.DisplayDialog("工人工作效率", "WorkerEfficiencyTracker 实例不可用。", "确定");
                return;
            }

            var allRecords = tracker.GetAllRecords();
            if (allRecords.Count == 0)
            {
                EditorUtility.DisplayDialog("Worker 列表", "暂无 Worker 效率记录。", "确定");
                return;
            }

            StringBuilder sb = new StringBuilder(1024);
            sb.AppendFormat("=== Worker 列表（共 {0} 名，按完成任务数降序）===", allRecords.Count).AppendLine();
            sb.AppendLine();
            sb.AppendLine("排名 | 名称 | 存活 | 完成数 | 速率 | 死亡 | 常用任务");
            sb.AppendLine("-----|------|------|--------|------|------|----------");

            for (int i = 0; i < allRecords.Count; i++)
            {
                WorkerEfficiencyTracker.WorkerEfficiencyRecord r = allRecords[i];
                sb.AppendFormat(
                    " #{0} | {1} | {2} | {3} | {4:F1}/min | {5} | {6}",
                    i + 1,
                    r.WorkerName,
                    r.IsAlive ? "+" : "x",
                    r.TotalTasksCompleted,
                    r.GetTasksPerMinute(),
                    r.DeathCount,
                    r.GetMostFrequentTaskType()).AppendLine();
            }

            EditorUtility.DisplayDialog("Worker 效率列表", sb.ToString(), "确定");
        }

        [MenuItem(MenuRoot + "查看全局任务分布")]
        private static void ShowGlobalTaskDistribution()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("工人工作效率", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            WorkerEfficiencyTracker tracker = WorkerEfficiencyTracker.Instance;
            if (tracker == null)
            {
                EditorUtility.DisplayDialog("工人工作效率", "WorkerEfficiencyTracker 实例不可用。", "确定");
                return;
            }

            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("=== 全局统计 ===");
            sb.AppendLine();
            sb.AppendFormat("追踪 Worker 数量: {0}", tracker.TrackedWorkerCount).AppendLine();
            sb.AppendFormat("全局任务完成总数: {0}", tracker.TotalTasksCompleted).AppendLine();
            sb.AppendFormat("全局 Worker 死亡总数: {0}", tracker.TotalWorkerDeaths).AppendLine();

            // 同时输出 GameplaySessionStats 中的 Worker 相关统计
            if (GameplaySessionStats.Instance != null)
            {
                GameplaySessionStatsSnapshot snapshot = GameplaySessionStats.Instance.CreateSnapshot();
                sb.AppendLine();
                sb.AppendLine("--- GameplaySessionStats 全局统计 ---");
                sb.AppendFormat("Worker 任务完成总数: {0}", snapshot.TotalWorkerTaskCompletedCount).AppendLine();
                sb.AppendFormat("Worker 死亡总数: {0}", snapshot.TotalWorkerDeathCount).AppendLine();

                if (snapshot.CompletedWorkerTasksByType.Count > 0)
                {
                    sb.AppendLine("  任务类型分布:");
                    foreach (var kv in snapshot.CompletedWorkerTasksByType)
                    {
                        sb.AppendFormat("    {0}: {1}", kv.Key, kv.Value).AppendLine();
                    }
                }
            }

            EditorUtility.DisplayDialog("任务分布", sb.ToString(), "确定");
        }

        [MenuItem(MenuRoot + "查看会话工人统计")]
        private static void ShowSessionStatsWorkerStats()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("GameplaySessionStats", "请在 Play Mode 中使用此功能。", "确定");
                return;
            }

            if (GameplaySessionStats.Instance == null)
            {
                EditorUtility.DisplayDialog("GameplaySessionStats", "GameplaySessionStats 实例不可用。", "确定");
                return;
            }

            GameplaySessionStatsSnapshot snapshot = GameplaySessionStats.Instance.CreateSnapshot();
            StringBuilder sb = new StringBuilder(512);
            sb.AppendLine("=== GameplaySessionStats Worker 相关统计 ===");
            sb.AppendLine();
            sb.AppendFormat("Worker 任务完成总数: {0}", snapshot.TotalWorkerTaskCompletedCount).AppendLine();
            sb.AppendFormat("Worker 死亡总数: {0}", snapshot.TotalWorkerDeathCount).AppendLine();
            sb.AppendLine();
            sb.AppendLine("按任务类型统计:");
            if (snapshot.CompletedWorkerTasksByType.Count == 0)
            {
                sb.AppendLine("  （暂无数据 — RecordWorkerTaskCompleted 尚未被调用）");
            }
            else
            {
                foreach (var kv in snapshot.CompletedWorkerTasksByType)
                {
                    sb.AppendFormat("  {0}: {1}", kv.Key, kv.Value).AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("验证结论:");
            if (snapshot.TotalWorkerTaskCompletedCount > 0)
            {
                sb.AppendLine("  [OK] RecordWorkerTaskCompleted 已被正确接入");
            }
            else
            {
                sb.AppendLine("  [待验证] 请完成至少一个 Worker 任务后再次查看");
            }

            EditorUtility.DisplayDialog("GameplaySessionStats Worker 统计", sb.ToString(), "确定");
        }
    }
}
