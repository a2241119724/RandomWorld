namespace LAB2D.Domain.Worker
{
    using LAB2D.Character.Worker.Task;
    using LAB2D.Enum;
    using System;
    using System.Text;

    /// <summary>
    /// 工人任务队列拥堵报告（纯数据）。
    /// 由 WorkerTaskCongestionAdvisor 维护，供 Tip、HUD、Editor 菜单和后续任务目标系统只读查询。
    /// 表现层方法已提取到 WorkerTaskCongestionTool。
    /// </summary>
    [Serializable]
    public class WorkerTaskCongestionReport
    {
        /// <summary>当前队列中的任务总数。</summary>
        public int TotalTaskCount;

        /// <summary>等待 Worker 接取的任务数。</summary>
        public int WaitingTaskCount;

        /// <summary>已被 Worker 接取并进行中的任务数。</summary>
        public int RunningTaskCount;

        /// <summary>当前拥堵等级。</summary>
        public WorkerTaskCongestionLevel Level;

        /// <summary>是否存在主要积压任务类型。</summary>
        public bool HasPrimaryTaskType;

        /// <summary>主要积压任务类型。</summary>
        public AWorkerTask.WorkerTaskTypeEnum PrimaryTaskType;

        /// <summary>主要任务类型的等待数量。</summary>
        public int PrimaryWaitingTaskCount;

        /// <summary>面向玩家的建议文案。</summary>
        public string AdviceText;

        /// <summary>扫描异常信息，正常为空。</summary>
        public string ErrorMessage;

        /// <summary>
        /// 是否达到可主动 Tip 的拥堵程度。
        /// </summary>
        public bool ShouldShowTip
        {
            get
            {
                return this.Level == WorkerTaskCongestionLevel.Congested ||
                    this.Level == WorkerTaskCongestionLevel.Critical;
            }
        }

        /// <summary>
        /// 构建用于变化检测的签名（纯 StringBuilder，无外部依赖）。
        /// </summary>
        /// <returns>报告关键字段签名。</returns>
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append(this.TotalTaskCount).Append('|')
                .Append(this.WaitingTaskCount).Append('|')
                .Append(this.RunningTaskCount).Append('|')
                .Append(this.Level).Append('|')
                .Append(this.HasPrimaryTaskType).Append('|')
                .Append(this.PrimaryTaskType).Append('|')
                .Append(this.PrimaryWaitingTaskCount).Append('|')
                .Append(this.AdviceText);

            return builder.ToString();
        }
    }
}
