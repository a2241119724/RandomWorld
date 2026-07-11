namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Worker 任务分配诊断报告。
    /// 只保存任务队列和 Worker 状态的只读聚合结果，不持有任务修改意图，不改变调度。
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
        /// 构建用于变化检测的报告签名。
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

        /// <summary>
        /// 生成适合 Editor 日志的纯文本摘要。
        /// </summary>
        /// <returns>多行纯文本摘要。</returns>
        public string ToPlainText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            StringBuilder builder = new StringBuilder(512);
            builder.AppendFormat(
                "人力: 总 {0} | 空闲 {1} | 忙碌 {2} | 临界 {3}",
                this.WorkerCount,
                this.IdleWorkerCount,
                this.BusyWorkerCount,
                this.CriticalWorkerCount);
            builder.AppendLine();
            builder.AppendFormat(
                "任务: 总 {0} | 等待 {1} | 进行中 {2} | 阻塞 {3}",
                this.TotalTaskCount,
                this.WaitingTaskCount,
                this.RunningTaskCount,
                this.BlockedTaskCount);
            builder.AppendLine();

            if (this.PrimaryBlockReason != WorkerTaskBlockReason.None)
            {
                builder.Append("主要阻塞: ")
                    .Append(ColonyCommandCenterTool.GetBlockReasonName(this.PrimaryBlockReason))
                    .AppendLine();
            }

            for (int i = 0; i < Math.Min(this.ReasonSummaries.Count, 5); i++)
            {
                builder.Append("- ")
                    .Append(ColonyCommandCenterTool.GetBlockReasonName(this.ReasonSummaries[i].Reason))
                    .Append(": ")
                    .Append(this.ReasonSummaries[i].Count)
                    .AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// 单个任务阻塞原因聚合。
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
    /// 单个等待任务的阻塞诊断明细。
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
        public AWorkerTask.WorkerTaskTypeEnum TaskType;

        /// <summary>任务目标位置文本。</summary>
        public string TargetText;

        /// <summary>阻塞原因。</summary>
        public WorkerTaskBlockReason Reason;

        /// <summary>
        /// 生成 HUD 展示行。
        /// </summary>
        /// <returns>单行任务诊断文本。</returns>
        public string ToDisplayLine()
        {
            string color = ColonyCommandCenterTool.GetBlockReasonRichColor(this.Reason);
            string taskName = string.IsNullOrEmpty(this.TaskName)
                ? WorkerTaskSummaryTool.GetTaskDisplayName(this.TaskType)
                : this.TaskName;
            return $"<color={color}>{WorkerTaskSummaryTool.GetTaskDisplayName(this.TaskType)}</color> " +
                $"#{this.TaskId} {taskName} {this.TargetText}: " +
                $"{ColonyCommandCenterTool.GetBlockReasonName(this.Reason)}";
        }
    }

    /// <summary>
    /// 殖民地运营指挥中心聚合报告。
    /// 由 `ColonyCommandCenterManager` 维护，供 HUD、Tip、Editor 菜单和后续运营事件只读查询。
    /// </summary>
    [Serializable]
    public class ColonyCommandCenterReport
    {
        /// <summary>整体警戒等级。</summary>
        public ColonyCommandAlertLevel AlertLevel;

        /// <summary>面向玩家的主问题摘要。</summary>
        public string FocusText;

        /// <summary>面向玩家的行动建议。</summary>
        public string AdviceText;

        /// <summary>报告生成时间。</summary>
        public float UpdatedTime;

        /// <summary>任务分配诊断报告。</summary>
        public WorkerTaskAssignmentReport AssignmentReport;

        /// <summary>补给缺口报告。</summary>
        public WorkerSupplyReport SupplyReport;

        /// <summary>任务拥堵报告。</summary>
        public WorkerTaskCongestionReport CongestionReport;

        /// <summary>任务队列快照。</summary>
        public WorkerTaskQueueSnapshot QueueSnapshot;

        /// <summary>诊断异常信息，正常为空。</summary>
        public string ErrorMessage;

        /// <summary>
        /// 是否应主动显示 Tip。
        /// </summary>
        public bool ShouldShowTip
        {
            get
            {
                return this.AlertLevel == ColonyCommandAlertLevel.Warning ||
                    this.AlertLevel == ColonyCommandAlertLevel.Critical;
            }
        }

        /// <summary>
        /// 构建用于变化检测的报告签名。
        /// </summary>
        /// <returns>报告关键字段签名。</returns>
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.Append(this.AlertLevel).Append('|')
                .Append(this.FocusText).Append('|')
                .Append(this.AdviceText).Append('|')
                .Append(this.ErrorMessage).Append('|');

            if (this.AssignmentReport != null)
            {
                builder.Append(this.AssignmentReport.BuildSignature());
            }

            if (this.SupplyReport != null)
            {
                builder.Append('|').Append(this.SupplyReport.BuildSignature());
            }

            if (this.CongestionReport != null)
            {
                builder.Append('|').Append(this.CongestionReport.BuildSignature());
            }

            return builder.ToString();
        }

        /// <summary>
        /// 生成 HUD 主摘要文本。
        /// </summary>
        /// <returns>带 RichText 颜色的主摘要。</returns>
        public string ToMainText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            WorkerTaskAssignmentReport assignment = this.AssignmentReport;
            WorkerTaskQueueSnapshot queue = this.QueueSnapshot;
            WorkerSupplyReport supply = this.SupplyReport;

            StringBuilder builder = new StringBuilder(512);
            builder.Append("<color=")
                .Append(ColonyCommandCenterTool.GetAlertLevelRichColor(this.AlertLevel))
                .Append(">")
                .Append(ColonyCommandCenterTool.GetAlertLevelName(this.AlertLevel))
                .Append("</color> ");
            builder.Append(this.FocusText ?? ColonyCommandCenterConstant.EmptyText);
            builder.AppendLine();

            if (assignment != null)
            {
                builder.AppendFormat(
                    "人力: 总 {0} | 空闲 {1} | 忙碌 {2} | 临界 {3}",
                    assignment.WorkerCount,
                    assignment.IdleWorkerCount,
                    assignment.BusyWorkerCount,
                    assignment.CriticalWorkerCount);
                builder.AppendLine();
            }

            if (queue != null)
            {
                builder.AppendFormat(
                    "任务: 总 {0} | 等待 {1} | 进行中 {2}",
                    queue.TotalTaskCount,
                    queue.WaitingTaskCount,
                    queue.RunningTaskCount);
                if (assignment != null)
                {
                    builder.Append(" | 阻塞 ").Append(assignment.BlockedTaskCount);
                }

                builder.AppendLine();
            }

            if (supply != null && supply.WorkerCount > 0)
            {
                builder.AppendFormat(
                    "补给: 食物 {0} 份 | 缺床 {1} | 饥饿 {2} | 疲劳 {3}",
                    supply.FoodItemCount,
                    supply.WorkerWithoutBedCount,
                    supply.HungryWorkerCount,
                    supply.TiredWorkerCount);
                builder.AppendLine();
            }

            builder.Append("建议: ").Append(this.AdviceText ?? "继续观察殖民地运行。");
            return builder.ToString();
        }

        /// <summary>
        /// 生成 HUD 细节文本。
        /// </summary>
        /// <returns>带 RichText 颜色的细节文本。</returns>
        public string ToDetailText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(768);
            WorkerTaskAssignmentReport assignment = this.AssignmentReport;
            if (assignment != null && assignment.ReasonSummaries.Count > 0)
            {
                builder.AppendLine("<color=" + PixelUITheme.RichSky + ">任务阻塞原因</color>");
                for (int i = 0; i < Math.Min(assignment.ReasonSummaries.Count, 4); i++)
                {
                    WorkerTaskBlockReasonSummary summary = assignment.ReasonSummaries[i];
                    builder.Append("- <color=")
                        .Append(ColonyCommandCenterTool.GetBlockReasonRichColor(summary.Reason))
                        .Append(">")
                        .Append(ColonyCommandCenterTool.GetBlockReasonName(summary.Reason))
                        .Append("</color> x")
                        .Append(summary.Count)
                        .AppendLine();
                }
            }

            if (assignment != null && assignment.Details.Count > 0)
            {
                builder.AppendLine("<color=" + PixelUITheme.RichSky + ">等待任务样例</color>");
                for (int i = 0; i < Math.Min(assignment.Details.Count, 3); i++)
                {
                    builder.AppendLine(assignment.Details[i].ToDisplayLine());
                }
            }

            if (this.CongestionReport != null && this.CongestionReport.Level != WorkerTaskCongestionLevel.None)
            {
                builder.Append("<color=")
                    .Append(WorkerTaskCongestionTool.GetLevelRichColor(this.CongestionReport.Level))
                    .Append(">拥堵</color> ")
                    .Append(WorkerTaskCongestionTool.GetLevelName(this.CongestionReport.Level))
                    .Append(" | ")
                    .Append(this.CongestionReport.AdviceText)
                    .AppendLine();
            }

            return builder.Length == 0 ? "暂无详细问题。" : builder.ToString().TrimEnd();
        }

        /// <summary>
        /// 生成适合 TipUI 展示的短文案。
        /// </summary>
        /// <returns>短 Tip 文案。</returns>
        public string ToTipText()
        {
            return $"{ColonyCommandCenterTool.GetAlertLevelName(this.AlertLevel)}: {this.FocusText} {this.AdviceText}";
        }
    }
}
