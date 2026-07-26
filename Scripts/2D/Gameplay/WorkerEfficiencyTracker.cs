namespace LAB2D.Gameplay
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 工人工作效率追踪器。
    /// 以独立 Singleton 形式追踪每个 Worker 的任务完成情况、任务类型分布和死亡统计，
    /// 为殖民地管理 UI 或数据分析提供数据层。
    /// 不修改存档、Scene、Prefab 或 ScriptableObject，仅在运行时记录效率数据。
    ///
    /// 接入方式：在 AWorkerTask.Start/Finish 和 AWorker.Death 中调用对应 Record* 方法。
    /// 后续可订阅 WorkerEfficiencyChanged 事件驱动 UI 更新。
    /// </summary>
    public class WorkerEfficiencyTracker : Singleton<WorkerEfficiencyTracker>
    {
        /// <summary>单条 Worker 效率记录</summary>
        [Serializable]
        public class WorkerEfficiencyRecord
        {
            /// <summary>Worker 名称</summary>
            public string WorkerName;

            /// <summary>Worker 的 GameObject 实例 ID（用于关联运行时对象）</summary>
            public int WorkerInstanceId;

            /// <summary>是否存活</summary>
            public bool IsAlive;

            /// <summary>累计完成任务总数</summary>
            public int TotalTasksCompleted;

            /// <summary>按任务类型分组的完成数量</summary>
            public Dictionary<WorkerTaskType, int> TasksByType;

            /// <summary>死亡次数</summary>
            public int DeathCount;

            /// <summary>上一次任务开始的时间（this.GameTime.Time）</summary>
            public float LastTaskStartTime;

            /// <summary>上一次任务类型</summary>
            public WorkerTaskType LastTaskType;

            /// <summary>累计任务预计耗时总和（maxProgress，秒）</summary>
            public float TotalEstimatedWorkTime;

            /// <summary>记录创建时间（this.GameTime.Time）</summary>
            public float CreatedTime;

            /// <summary>最近一次完成任务的时间（this.GameTime.Time）</summary>
            public float LastCompletionTime;

            public WorkerEfficiencyRecord()
            {
                this.TasksByType = new Dictionary<WorkerTaskType, int>();
                this.IsAlive = true;
            }

            /// <summary>
            /// 计算平均每分钟完成任务数（任务频率）。
            /// 基于累计预计工作耗时估算。
            /// </summary>
            public float GetTasksPerMinute()
            {
                if (this.TotalTasksCompleted == 0)
                {
                    return 0f;
                }

                float elapsedMinutes = this.TotalEstimatedWorkTime / 60f;
                if (elapsedMinutes <= 0f)
                {
                    return 0f;
                }

                return this.TotalTasksCompleted / elapsedMinutes;
            }

            /// <summary>
            /// 获取完成最多的任务类型。
            /// </summary>
            public WorkerTaskType GetMostFrequentTaskType()
            {
                WorkerTaskType best = WorkerTaskType.Build;
                int maxCount = -1;
                foreach (var kv in this.TasksByType)
                {
                    if (kv.Value > maxCount)
                    {
                        maxCount = kv.Value;
                        best = kv.Key;
                    }
                }

                return best;
            }
        }

        /// <summary>所有 Worker 效率记录（Key: Worker 实例 ID）</summary>
        private readonly Dictionary<int, WorkerEfficiencyRecord> records;

        /// <summary>全局任务完成总数（所有 Worker 累计）</summary>
        private int totalTasksCompleted;

        /// <summary>全局 Worker 死亡总数</summary>
        private int totalWorkerDeaths;
        private IGameTime gameTime;

        private IGameTime GameTime => this.gameTime ?? (this.gameTime = Core.ServiceLocator.Get<IGameTime>());

        /// <summary>Worker 效率变化事件（参数：Worker 名称）</summary>
        public event Action<string> WorkerEfficiencyChanged;

        /// <summary>Worker 完成任务事件</summary>
        public event Action<AWorker, AWorkerTask> TaskCompleted;

        /// <summary>Worker 死亡事件</summary>
        public event Action<AWorker> WorkerDied;

        public WorkerEfficiencyTracker()
        {
            this.records = new Dictionary<int, WorkerEfficiencyRecord>();
        }

        /// <summary>
        /// 获取全局任务完成总数。
        /// </summary>
        public int TotalTasksCompleted
        {
            get { return this.totalTasksCompleted; }
        }

        /// <summary>
        /// 获取全局 Worker 死亡总数。
        /// </summary>
        public int TotalWorkerDeaths
        {
            get { return this.totalWorkerDeaths; }
        }

        /// <summary>
        /// 获取当前已记录的 Worker 数量。
        /// </summary>
        public int TrackedWorkerCount
        {
            get { return this.records.Count; }
        }

        /// <summary>
        /// 记录 Worker 开始执行任务。
        /// 由 AWorkerTask.Start 调用。
        /// </summary>
        /// <param name="worker">执行任务的 Worker</param>
        /// <param name="task">被接受的任务</param>
        public void RecordTaskStarted(AWorker worker, AWorkerTask task)
        {
            if (worker == null || task == null)
            {
                return;
            }

            int instanceId = worker.GetInstanceID();
            WorkerEfficiencyRecord record = this.GetOrCreateRecord(worker, instanceId);
            record.LastTaskStartTime = this.GameTime.Time;
            record.LastTaskType = task.TaskType;
        }

        /// <summary>
        /// 记录 Worker 完成任务。
        /// 由 AWorkerTask.Finish 调用，同时激活 GameplaySessionStats 中的全局统计。
        /// </summary>
        /// <param name="worker">完成任务的 Worker</param>
        /// <param name="task">已完成的任务</param>
        public void RecordTaskCompleted(AWorker worker, AWorkerTask task)
        {
            if (worker == null || task == null)
            {
                return;
            }

            int instanceId = worker.GetInstanceID();
            WorkerEfficiencyRecord record = this.GetOrCreateRecord(worker, instanceId);

            // 更新 Worker 个人统计
            record.TotalTasksCompleted++;
            if (record.TasksByType.ContainsKey(task.TaskType))
            {
                record.TasksByType[task.TaskType]++;
            }
            else
            {
                record.TasksByType[task.TaskType] = 1;
            }

            record.TotalEstimatedWorkTime += 2.0f; // maxProgress 默认值为 2 秒
            record.LastCompletionTime = this.GameTime.Time;

            // 更新全局统计
            this.totalTasksCompleted++;

            // 激活 GameplaySessionStats 中的已有死代码：RecordWorkerTaskCompleted
            Core.ServiceLocator.Get<GameplaySessionStats>().RecordWorkerTaskCompleted(task.TaskType);

            // 触发事件
            this.TaskCompleted?.Invoke(worker, task);
            this.WorkerEfficiencyChanged?.Invoke(record.WorkerName);
        }

        /// <summary>
        /// 记录 Worker 死亡。
        /// 由 AWorker.Death 调用，同时激活 GameplaySessionStats 中的全局死亡统计。
        /// </summary>
        /// <param name="worker">死亡的 Worker</param>
        public void RecordWorkerDeath(AWorker worker)
        {
            if (worker == null)
            {
                return;
            }

            int instanceId = worker.GetInstanceID();
            WorkerEfficiencyRecord record = this.GetOrCreateRecord(worker, instanceId);

            // 更新 Worker 个人统计
            record.DeathCount++;
            record.IsAlive = false;

            // 更新全局统计
            this.totalWorkerDeaths++;

            // 激活 GameplaySessionStats 中的已有死代码：RecordWorkerDeath
            Core.ServiceLocator.Get<GameplaySessionStats>().RecordWorkerDeath();

            // 触发事件
            this.WorkerDied?.Invoke(worker);
            this.WorkerEfficiencyChanged?.Invoke(record.WorkerName);
        }

        /// <summary>
        /// 获取指定 Worker 的效率记录。
        /// 如果 Worker 尚未被追踪，返回 null。
        /// </summary>
        /// <param name="worker">目标 Worker</param>
        /// <returns>效率记录，未追踪时返回 null</returns>
        public WorkerEfficiencyRecord GetWorkerRecord(AWorker worker)
        {
            if (worker == null)
            {
                return null;
            }

            int instanceId = worker.GetInstanceID();
            if (this.records.TryGetValue(instanceId, out WorkerEfficiencyRecord record))
            {
                return record;
            }

            return null;
        }

        /// <summary>
        /// 获取所有 Worker 效率记录的只读列表（按完成任务数降序排列）。
        /// </summary>
        public List<WorkerEfficiencyRecord> GetAllRecords()
        {
            List<WorkerEfficiencyRecord> list = new List<WorkerEfficiencyRecord>(this.records.Values);
            list.Sort((a, b) => b.TotalTasksCompleted.CompareTo(a.TotalTasksCompleted));
            return list;
        }

        /// <summary>
        /// 获取最高效 Worker（完成任务最多），无 Worker 时返回 null。
        /// </summary>
        public WorkerEfficiencyRecord GetMostProductiveWorker()
        {
            WorkerEfficiencyRecord best = null;
            int maxTasks = -1;
            foreach (WorkerEfficiencyRecord record in this.records.Values)
            {
                if (record.TotalTasksCompleted > maxTasks)
                {
                    maxTasks = record.TotalTasksCompleted;
                    best = record;
                }
            }

            return best;
        }

        /// <summary>
        /// 获取当前存活 Worker 的效率记录。
        /// </summary>
        public List<WorkerEfficiencyRecord> GetAliveWorkerRecords()
        {
            List<WorkerEfficiencyRecord> alive = new List<WorkerEfficiencyRecord>();
            foreach (WorkerEfficiencyRecord record in this.records.Values)
            {
                if (record.IsAlive)
                {
                    alive.Add(record);
                }
            }

            return alive;
        }

        /// <summary>
        /// 构建可读的效率摘要文本。
        /// </summary>
        public string BuildSummaryText()
        {
            StringBuilder sb = new StringBuilder(1024);
            sb.AppendLine("=== 工人工作效率报告 ===");
            sb.AppendLine();
            sb.AppendFormat("追踪 Worker 数量: {0}", this.TrackedWorkerCount).AppendLine();
            sb.AppendFormat("全局任务完成总数: {0}", this.totalTasksCompleted).AppendLine();
            sb.AppendFormat("全局 Worker 死亡总数: {0}", this.totalWorkerDeaths).AppendLine();
            sb.AppendLine();

            WorkerEfficiencyRecord mostProductive = this.GetMostProductiveWorker();
            if (mostProductive != null)
            {
                sb.AppendLine("--- 最高效 Worker ---");
                sb.AppendFormat("  名称: {0}", mostProductive.WorkerName).AppendLine();
                sb.AppendFormat("  完成任务: {0}", mostProductive.TotalTasksCompleted).AppendLine();
                sb.AppendFormat("  预估速率: {0:F1} 任务/分钟", mostProductive.GetTasksPerMinute()).AppendLine();
                sb.AppendFormat("  最常见任务: {0}", mostProductive.GetMostFrequentTaskType()).AppendLine();
                sb.AppendFormat("  存活: {0}", mostProductive.IsAlive ? "是" : "否").AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("--- 各 Worker 详情 ---");
            List<WorkerEfficiencyRecord> allRecords = this.GetAllRecords();
            if (allRecords.Count == 0)
            {
                sb.AppendLine("  （暂无 Worker 记录）");
            }
            else
            {
                foreach (WorkerEfficiencyRecord record in allRecords)
                {
                    sb.AppendFormat("  [{0}] {1} — 完成: {2} 任务, 速率: {3:F1}/min, 死亡: {4}",
                        record.IsAlive ? "存活" : "阵亡",
                        record.WorkerName,
                        record.TotalTasksCompleted,
                        record.GetTasksPerMinute(),
                        record.DeathCount).AppendLine();

                    if (record.TasksByType.Count > 0)
                    {
                        sb.Append("    任务分布: ");
                        bool first = true;
                        foreach (var kv in record.TasksByType)
                        {
                            if (!first)
                            {
                                sb.Append(", ");
                            }

                            sb.AppendFormat("{0}:{1}", kv.Key, kv.Value);
                            first = false;
                        }

                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- 全局任务类型分布 ---");
            Dictionary<WorkerTaskType, int> globalTypeDist = new Dictionary<WorkerTaskType, int>();
            foreach (WorkerEfficiencyRecord record in this.records.Values)
            {
                foreach (var kv in record.TasksByType)
                {
                    if (globalTypeDist.ContainsKey(kv.Key))
                    {
                        globalTypeDist[kv.Key] += kv.Value;
                    }
                    else
                    {
                        globalTypeDist[kv.Key] = kv.Value;
                    }
                }
            }

            if (globalTypeDist.Count == 0)
            {
                sb.AppendLine("  （暂无任务完成记录）");
            }
            else
            {
                foreach (var kv in globalTypeDist)
                {
                    float pct = this.totalTasksCompleted > 0
                        ? (float)kv.Value / this.totalTasksCompleted * 100f
                        : 0f;
                    sb.AppendFormat("  {0}: {1} ({2:F1}%)", kv.Key, kv.Value, pct).AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取或创建 Worker 效率记录。
        /// </summary>
        private WorkerEfficiencyRecord GetOrCreateRecord(AWorker worker, int instanceId)
        {
            if (this.records.TryGetValue(instanceId, out WorkerEfficiencyRecord record))
            {
                // 如果 Worker 之前被标记为死亡，但又被复用（新 Worker），则重置部分字段
                if (!record.IsAlive)
                {
                    record.IsAlive = true;
                    record.LastTaskStartTime = 0f;
                }

                return record;
            }

            record = new WorkerEfficiencyRecord
            {
                WorkerName = worker.name,
                WorkerInstanceId = instanceId,
                CreatedTime = this.GameTime.Time,
            };

            this.records[instanceId] = record;
            return record;
        }
    }
}
