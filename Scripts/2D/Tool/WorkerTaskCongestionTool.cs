namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using System.Text;
    /// <summary>
    /// 工人任务队列拥堵建议工具。
    /// 只负责根据只读任务队列快照计算拥堵等级、主积压任务类型和玩家建议文案；不新增任务、不取消任务、不调整任务优先级。
    /// 使用边界：本工具不访问 Scene、Prefab、存档、Photon 或 AssetBundle，调用方应传入已经构建好的任务快照。
    /// </summary>
    public static class WorkerTaskCongestionTool
    {
        private static readonly WorkerTaskCongestionRuleService RuleService =
            new WorkerTaskCongestionRuleService();
        /// <summary>
        /// 根据任务队列快照构建拥堵报告。
        /// </summary>
        /// <param name="snapshot">任务队列只读快照。</param>
        /// <returns>拥堵报告；快照为空时返回未初始化报告。</returns>
        public static WorkerTaskCongestionReport BuildReport(WorkerTaskQueueSnapshot snapshot)
        {
            WorkerTaskCongestionReport report = new WorkerTaskCongestionReport();
            if (snapshot == null)
            {
                report.Level = WorkerTaskCongestionLevel.None;
                report.ErrorMessage = WorkerTaskCongestionConstant.ManagerUnavailableText;
                report.AdviceText = WorkerTaskCongestionConstant.ManagerUnavailableText;
                return report;
            }

            report.TotalTaskCount = snapshot.TotalTaskCount;
            report.WaitingTaskCount = snapshot.WaitingTaskCount;
            report.RunningTaskCount = snapshot.RunningTaskCount;
            report.Level = GetCongestionLevel(snapshot.WaitingTaskCount);

            WorkerTaskTypeSummary primarySummary = GetPrimaryWaitingSummary(snapshot);
            if (primarySummary != null && primarySummary.WaitingCount > 0)
            {
                report.HasPrimaryTaskType = true;
                report.PrimaryTaskType = primarySummary.TaskType;
                report.PrimaryWaitingTaskCount = primarySummary.WaitingCount;
            }

            report.AdviceText = BuildAdviceText(snapshot, report);
            return report;
        }

        /// <summary>
        /// 根据等待任务数获取拥堵等级。
        /// </summary>
        /// <param name="waitingTaskCount">等待中的任务数量。</param>
        /// <returns>拥堵等级。</returns>
        public static WorkerTaskCongestionLevel GetCongestionLevel(int waitingTaskCount)
        {
            return RuleService.GetCongestionLevel(waitingTaskCount);
        }

        /// <summary>
        /// 获取拥堵等级中文名。
        /// </summary>
        /// <param name="level">拥堵等级。</param>
        /// <returns>适合 UI 和日志展示的中文名。</returns>
        public static string GetLevelName(WorkerTaskCongestionLevel level)
        {
            switch (level)
            {
                case WorkerTaskCongestionLevel.Smooth:
                    return "平稳";
                case WorkerTaskCongestionLevel.Busy:
                    return "繁忙";
                case WorkerTaskCongestionLevel.Congested:
                    return "拥堵";
                case WorkerTaskCongestionLevel.Critical:
                    return "严重拥堵";
                default:
                    return "无数据";
            }
        }

        /// <summary>
        /// 获取拥堵等级 RichText 颜色。
        /// </summary>
        /// <param name="level">拥堵等级。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetLevelRichColor(WorkerTaskCongestionLevel level)
        {
            switch (level)
            {
                case WorkerTaskCongestionLevel.Busy:
                    return PixelUITheme.RichGold;
                case WorkerTaskCongestionLevel.Congested:
                case WorkerTaskCongestionLevel.Critical:
                    return PixelUITheme.RichCoral;
                case WorkerTaskCongestionLevel.Smooth:
                    return PixelUITheme.RichMint;
                default:
                    return PixelUITheme.RichSky;
            }
        }

        /// <summary>
        /// 获取等待任务最多的任务类型统计。
        /// </summary>
        /// <param name="snapshot">任务队列只读快照。</param>
        /// <returns>等待数量最多的任务类型；无等待任务时返回空。</returns>
        public static WorkerTaskTypeSummary GetPrimaryWaitingSummary(WorkerTaskQueueSnapshot snapshot)
        {
            if (snapshot == null || snapshot.TaskTypeSummaries == null)
            {
                return null;
            }

            WorkerTaskTypeSummary primary = null;
            foreach (WorkerTaskTypeSummary summary in snapshot.TaskTypeSummaries)
            {
                if (summary == null || summary.WaitingCount <= 0)
                {
                    continue;
                }

                if (primary == null || summary.WaitingCount > primary.WaitingCount)
                {
                    primary = summary;
                }
            }

            return primary;
        }

        /// <summary>
        /// 判断报告中的主积压类型是否足够明显。
        /// </summary>
        /// <param name="report">拥堵报告。</param>
        /// <returns>主类型等待数量和占比都达到阈值时返回 true。</returns>
        public static bool HasDominantTaskType(WorkerTaskCongestionReport report)
        {
            if (report == null || !report.HasPrimaryTaskType || report.WaitingTaskCount <= 0)
            {
                return false;
            }

            return RuleService.HasDominantTaskType(
                report.PrimaryWaitingTaskCount,
                report.WaitingTaskCount);
        }

        /// <summary>
        /// 构建玩家可读的任务调整建议。
        /// </summary>
        /// <param name="snapshot">任务队列快照。</param>
        /// <param name="report">拥堵报告。</param>
        /// <returns>建议文案；不包含会改变任务状态的指令。</returns>
        public static string BuildAdviceText(WorkerTaskQueueSnapshot snapshot, WorkerTaskCongestionReport report)
        {
            if (snapshot == null || report == null)
            {
                return WorkerTaskCongestionConstant.ManagerUnavailableText;
            }

            if (report.Level == WorkerTaskCongestionLevel.None || report.Level == WorkerTaskCongestionLevel.Smooth)
            {
                return WorkerTaskCongestionConstant.NoCongestionText;
            }

            if (!HasDominantTaskType(report))
            {
                return report.Level == WorkerTaskCongestionLevel.Critical
                    ? "多类型任务严重积压，建议暂停新增指令，等待工人消化现有队列。"
                    : "多类型任务同时积压，建议暂缓扩张，先观察工人是否能接走等待任务。";
            }

            switch (report.PrimaryTaskType)
            {
                case WorkerTaskType.Build:
                    return "建造任务积压，建议暂停新增建造，优先确认材料和搬运链路。";
                case WorkerTaskType.Carry:
                    return "搬运任务积压，建议减少集中建造或采集，保留更多工人执行搬运。";
                case WorkerTaskType.Gather:
                    return "采集任务积压，建议分批下达采集，并确认目标附近可达。";
                case WorkerTaskType.Eat:
                    return "吃饭任务积压，建议补充食物库存，避免饥饿工人继续排队。";
                case WorkerTaskType.Sleep:
                    return "睡觉任务积压，建议检查床位绑定，暂缓消耗疲劳的工作。";
                case WorkerTaskType.Wear:
                    return "穿戴任务积压，建议确认装备可达，临时关闭非关键穿戴任务。";
                case WorkerTaskType.Plant:
                    return "种植任务积压，建议先稳定搬运和采集，再继续扩张农田。";
                case WorkerTaskType.Exercise:
                    return "锻炼任务积压，建议临时关闭锻炼任务，优先处理生产队列。";
                case WorkerTaskType.Demolish:
                    return "拆除任务积压，建议暂停拆除指令，让工人完成当前拆除。";
                default:
                    return "任务队列积压，建议暂缓新增任务并观察工人可用状态。";
            }
        }

        /// <summary>
        /// 生成 WorkerTaskCongestionReport 的 HUD/Editor 摘要文本（扩展方法，表现层）。
        /// </summary>
        public static string ToSummaryText(this WorkerTaskCongestionReport report)
        {
            if (report == null || !string.IsNullOrEmpty(report.ErrorMessage))
            {
                return report?.ErrorMessage ?? string.Empty;
            }

            StringBuilder builder = new StringBuilder(256);
            builder.AppendFormat(
                "任务队列拥堵: {0} | 总数 {1} | 等待 {2} | 进行中 {3}",
                WorkerTaskCongestionTool.GetLevelName(report.Level),
                report.TotalTaskCount,
                report.WaitingTaskCount,
                report.RunningTaskCount);
            builder.AppendLine();

            if (report.HasPrimaryTaskType)
            {
                builder.AppendFormat(
                    "主要积压: {0} {1} 个等待",
                    WorkerTaskSummaryTool.GetTaskDisplayName(report.PrimaryTaskType),
                    report.PrimaryWaitingTaskCount);
                builder.AppendLine();
            }

            builder.Append(report.AdviceText ?? WorkerTaskCongestionConstant.NoCongestionText);
            return builder.ToString();
        }

        /// <summary>
        /// 生成 WorkerTaskCongestionReport 的 Tip 短文案（扩展方法，表现层）。
        /// </summary>
        public static string ToTipText(this WorkerTaskCongestionReport report)
        {
            if (report == null || !string.IsNullOrEmpty(report.ErrorMessage))
            {
                return report?.ErrorMessage ?? string.Empty;
            }

            string levelName = WorkerTaskCongestionTool.GetLevelName(report.Level);
            if (report.HasPrimaryTaskType)
            {
                return $"任务{levelName}: 等待 {report.WaitingTaskCount}，主要积压 " +
                    $"{WorkerTaskSummaryTool.GetTaskDisplayName(report.PrimaryTaskType)} {report.PrimaryWaitingTaskCount}。{report.AdviceText}";
            }

            return $"任务{levelName}: 等待 {report.WaitingTaskCount}。{report.AdviceText}";
        }
    }
}
