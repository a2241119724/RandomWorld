namespace LAB2D.Domain.Worker
{
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Worker 任务分配诊断报告（纯数据）。
    /// 只保存任务队列和 Worker 状态的只读聚合结果，不持有任务修改意图，不改变调度。
    /// 表现层方法已提取到 ColonyCommandCenterTool。
    /// </summary>
    [Serializable]
    public class WorkerTaskAssignmentReport
    {
        /// <summary>参与扫描的 Worker 数量。</summary>
        public int WorkerCount;

        /// <summary>当前空闲 Worker 数量。</summary>
        public int IdleWorkerCount;

        /// <summary>当前忙碌 Worker 数量。</summary>
        public int BusyWorkerCount;

        /// <summary>处于临界状态的 Worker 数量。</summary>
        public int CriticalWorkerCount;

        /// <summary>任务总数。</summary>
        public int TotalTaskCount;

        /// <summary>进行中任务数量。</summary>
        public int RunningTaskCount;

        /// <summary>等待中任务数量。</summary>
        public int WaitingTaskCount;

        /// <summary>明确诊断为阻塞的任务数量。</summary>
        public int BlockedTaskCount;

        /// <summary>没有明显阻塞、只是等待接取的任务数量。</summary>
        public int MaybeAssignableTaskCount;

        /// <summary>最主要的阻塞原因。</summary>
        public WorkerTaskBlockReason PrimaryBlockReason = WorkerTaskBlockReason.None;

        /// <summary>诊断异常信息，正常为空。</summary>
        public string ErrorMessage;

        /// <summary>按原因聚合的阻塞统计。</summary>
        public List<WorkerTaskBlockReasonSummary> ReasonSummaries = new List<WorkerTaskBlockReasonSummary>();

        /// <summary>等待任务的阻塞明细。</summary>
        public List<WorkerTaskBlockDetail> Details = new List<WorkerTaskBlockDetail>();

        /// <summary>
        /// 是否存在明确阻塞任务。
        /// </summary>
        public bool HasBlockedTask
        {
            get { return this.BlockedTaskCount > 0; }
        }

        /// <summary>
        /// 构建用于变化检测的报告签名（纯 StringBuilder，无外部依赖）。
        /// </summary>
        /// <returns>报告关键字段签名。</returns>
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append(this.WorkerCount).Append('|')
                .Append(this.IdleWorkerCount).Append('|')
                .Append(this.BusyWorkerCount).Append('|')
                .Append(this.CriticalWorkerCount).Append('|')
                .Append(this.TotalTaskCount).Append('|')
                .Append(this.RunningTaskCount).Append('|')
                .Append(this.WaitingTaskCount).Append('|')
                .Append(this.BlockedTaskCount).Append('|')
                .Append(this.MaybeAssignableTaskCount).Append('|')
                .Append(this.PrimaryBlockReason).Append('|')
                .Append(this.ErrorMessage);

            for (int i = 0; i < this.ReasonSummaries.Count; i++)
            {
                builder.Append('|')
                    .Append(this.ReasonSummaries[i].Reason)
                    .Append(':')
                    .Append(this.ReasonSummaries[i].Count);
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// 单个任务阻塞原因聚合（纯数据）。
    /// </summary>
    [Serializable]
    public class WorkerTaskBlockReasonSummary
    {
        /// <summary>阻塞原因。</summary>
        public WorkerTaskBlockReason Reason;

        /// <summary>该原因命中的任务数量。</summary>
        public int Count;
    }

    /// <summary>
    /// 单个等待任务的阻塞诊断明细（纯数据）。
    /// 不持有任务引用，只保存展示和报告需要的安全字段。
    /// </summary>
    [Serializable]
    public class WorkerTaskBlockDetail
    {
        /// <summary>任务 ID。</summary>
        public long TaskId;

        /// <summary>任务名称。</summary>
        public string TaskName;

        /// <summary>任务类型。</summary>
        public WorkerTaskType TaskType;

        /// <summary>任务目标位置文本。</summary>
        public string TargetText;

        /// <summary>阻塞原因。</summary>
        public WorkerTaskBlockReason Reason;
    }
}
