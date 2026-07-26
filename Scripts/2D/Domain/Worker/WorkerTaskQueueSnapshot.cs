namespace LAB2D.Domain.Worker
{
    using LAB2D.Enum;
    using System.Collections.Generic;

    /// <summary>
    /// 工人任务队列只读快照（纯数据）。
    /// 用于 HUD、Editor 菜单和后续运营面板展示当前任务压力；不持有任务引用，不修改任务状态。
    /// </summary>
    public class WorkerTaskQueueSnapshot
    {
        /// <summary>
        /// 创建任务队列快照。
        /// </summary>
        /// <param name="totalTaskCount">当前队列中的任务总数。</param>
        /// <param name="runningTaskCount">已被 Worker 接取并正在进行的任务数。</param>
        /// <param name="taskTypeSummaries">按任务类型聚合后的统计列表。</param>
        public WorkerTaskQueueSnapshot(
            int totalTaskCount,
            int runningTaskCount,
            List<WorkerTaskTypeSummary> taskTypeSummaries)
        {
            this.TotalTaskCount = totalTaskCount;
            this.RunningTaskCount = runningTaskCount;
            this.TaskTypeSummaries = taskTypeSummaries ?? new List<WorkerTaskTypeSummary>();
        }

        /// <summary>
        /// 当前队列中的任务总数。
        /// </summary>
        public int TotalTaskCount { get; }

        /// <summary>
        /// 已被 Worker 接取并正在进行的任务数。
        /// </summary>
        public int RunningTaskCount { get; }

        /// <summary>
        /// 尚未被 Worker 接取的等待中任务数。
        /// </summary>
        public int WaitingTaskCount
        {
            get
            {
                return this.TotalTaskCount > this.RunningTaskCount
                    ? this.TotalTaskCount - this.RunningTaskCount
                    : 0;
            }
        }

        /// <summary>
        /// 按任务类型聚合后的统计列表。
        /// </summary>
        public IReadOnlyList<WorkerTaskTypeSummary> TaskTypeSummaries { get; }

        /// <summary>
        /// 是否存在可展示任务。
        /// </summary>
        public bool HasTask
        {
            get { return this.TotalTaskCount > 0; }
        }

        /// <summary>
        /// 获取指定任务类型的统计。
        /// </summary>
        /// <param name="taskType">任务类型。</param>
        /// <returns>存在时返回统计对象，否则返回空。</returns>
        public WorkerTaskTypeSummary GetSummary(WorkerTaskType taskType)
        {
            foreach (WorkerTaskTypeSummary summary in this.TaskTypeSummaries)
            {
                if (summary.TaskType == taskType)
                {
                    return summary;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 单个 Worker 任务类型的队列统计（纯数据）。
    /// 该数据只表达展示层需要的数量，不改变 WorkerTaskManager 内部字典。
    /// </summary>
    public class WorkerTaskTypeSummary
    {
        /// <summary>
        /// 创建单个任务类型统计。
        /// </summary>
        /// <param name="taskType">任务类型。</param>
        /// <param name="totalCount">该类型任务总数。</param>
        /// <param name="runningCount">该类型进行中任务数。</param>
        public WorkerTaskTypeSummary(
            WorkerTaskType taskType,
            int totalCount,
            int runningCount)
        {
            this.TaskType = taskType;
            this.TotalCount = totalCount;
            this.RunningCount = runningCount;
        }

        /// <summary>
        /// 任务类型。
        /// </summary>
        public WorkerTaskType TaskType { get; }

        /// <summary>
        /// 该类型任务总数。
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// 该类型已被 Worker 接取并正在进行的任务数。
        /// </summary>
        public int RunningCount { get; }

        /// <summary>
        /// 该类型尚未被 Worker 接取的等待中任务数。
        /// </summary>
        public int WaitingCount
        {
            get
            {
                return this.TotalCount > this.RunningCount
                    ? this.TotalCount - this.RunningCount
                    : 0;
            }
        }

        /// <summary>
        /// 是否存在该类型任务。
        /// </summary>
        public bool HasTask
        {
            get { return this.TotalCount > 0; }
        }
    }
}
