namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using System;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 工人任务队列拥堵提示管理器。
    /// 负责只读读取任务队列快照，生成拥堵等级与玩家建议，并按冷却规则请求现有 Tip UI 展示。
    /// 本类不修改 Worker 任务优先级，不新增或取消任务，不写入存档，不参与 Photon 同步。
    /// </summary>
    public class WorkerTaskCongestionAdvisor : Singleton<WorkerTaskCongestionAdvisor>
    {
        private WorkerTaskCongestionReport currentReport;
        private string lastSignature = string.Empty;
        private float nextRefreshTime;
        private float lastTipTime = -999.0f;
        private readonly WorkerTaskCongestionRuleService ruleService = new WorkerTaskCongestionRuleService();
        private bool enabled = true;
        private bool tipEnabled = true;

        /// <summary>
        /// 任务拥堵报告变化事件。
        /// HUD、Editor 菜单或后续目标提示系统可订阅该事件刷新展示。
        /// </summary>
        public event Action<WorkerTaskCongestionReport> OnWorkerTaskCongestionReportChanged;

        /// <summary>
        /// 任务拥堵 Tip 请求事件。
        /// 外部可订阅该事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnWorkerTaskCongestionTipRequested;

        /// <summary>
        /// 当前任务拥堵报告。
        /// </summary>
        public WorkerTaskCongestionReport CurrentReport
        {
            get
            {
                if (this.currentReport == null)
                {
                    this.Refresh(false);
                }

                return this.currentReport;
            }
        }

        /// <summary>
        /// 拥堵监控是否启用。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用任务队列拥堵监控。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.Refresh(false);
        }

        /// <summary>
        /// 禁用任务队列拥堵监控。
        /// 禁用后仍保留最后一次报告，便于 Editor 查看。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否允许显示拥堵 Tip。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 按固定间隔刷新任务拥堵报告。
        /// 可从 `GlobalInit.WorkerUpdate()` 每帧调用，内部会自行节流。
        /// </summary>
        public void Tick()
        {
            if (!this.enabled || Time.time < this.nextRefreshTime)
            {
                return;
            }

            this.nextRefreshTime = Time.time + this.ruleService.ClampRefreshInterval(
                WorkerTaskCongestionConstant.MonitorRefreshInterval);
            this.Refresh(true);
        }

        /// <summary>
        /// 立即刷新任务拥堵报告。
        /// </summary>
        /// <param name="allowTip">是否允许在报告变化时显示 Tip。</param>
        /// <returns>新的任务拥堵报告。</returns>
        public WorkerTaskCongestionReport Refresh(bool allowTip)
        {
            WorkerTaskCongestionReport report = this.BuildReport();
            string signature = report.BuildSignature();
            bool changed = !signature.Equals(this.lastSignature);

            this.currentReport = report;
            if (changed)
            {
                this.lastSignature = signature;
                this.OnWorkerTaskCongestionReportChanged?.Invoke(report);
            }

            if (allowTip && changed)
            {
                this.TryShowCongestionTip(report);
            }

            return report;
        }

        /// <summary>
        /// 构建适合 HUD、Editor 菜单或日志展示的拥堵摘要。
        /// </summary>
        /// <returns>多行任务拥堵摘要文本。</returns>
        public string BuildSummaryText()
        {
            return this.CurrentReport == null
                ? WorkerTaskCongestionConstant.ManagerUnavailableText
                : this.CurrentReport.ToSummaryText();
        }

        /// <summary>
        /// 手动触发一次当前拥堵 Tip。
        /// </summary>
        /// <returns>当前存在可提示拥堵时返回 true。</returns>
        public bool TryShowCurrentTip()
        {
            WorkerTaskCongestionReport report = this.Refresh(false);
            if (report == null || !report.ShouldShowTip)
            {
                return false;
            }

            this.lastTipTime = Time.time;
            this.ShowTip(report.ToTipText());
            return true;
        }

        /// <summary>
        /// 生成当前任务拥堵报告。
        /// </summary>
        /// <returns>只读报告数据。</returns>
        private WorkerTaskCongestionReport BuildReport()
        {
            try
            {
                WorkerTaskManager manager = WorkerTaskManager.Instance;
                if (manager == null)
                {
                    return WorkerTaskCongestionTool.BuildReport(null);
                }

                WorkerTaskQueueSnapshot snapshot = manager.CreateTaskQueueSnapshot();
                return WorkerTaskCongestionTool.BuildReport(snapshot);
            }
            catch (Exception exception)
            {
                return new WorkerTaskCongestionReport
                {
                    Level = WorkerTaskCongestionLevel.None,
                    ErrorMessage = "任务拥堵扫描失败: " + exception.Message,
                    AdviceText = "任务拥堵扫描失败: " + exception.Message,
                };
            }
        }

        /// <summary>
        /// 在任务拥堵变化时显示 Tip。
        /// </summary>
        /// <param name="report">任务拥堵报告。</param>
        private void TryShowCongestionTip(WorkerTaskCongestionReport report)
        {
            if (!this.tipEnabled || report == null || !report.ShouldShowTip)
            {
                return;
            }

            float now = Time.time;
            if (now - this.lastTipTime < WorkerTaskCongestionConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTime = now;
            this.ShowTip(report.ToTipText());
        }

        /// <summary>
        /// 显示任务拥堵提示。
        /// 优先复用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnWorkerTaskCongestionTipRequested?.Invoke(message);

            try
            {
                if (GlobalInit.Instance != null)
                {
                    GlobalInit.Instance.ShowTip(message);
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(WorkerTaskCongestionConstant.LogPrefix + " 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log(WorkerTaskCongestionConstant.LogPrefix + " " + message);
        }
    }

    /// <summary>
    /// 工人任务队列拥堵报告。
    /// 由 WorkerTaskCongestionAdvisor 维护，供 Tip、HUD、Editor 菜单和后续任务目标系统只读查询。
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
        /// 构建用于变化检测的签名。
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

        /// <summary>
        /// 生成 HUD、Editor 菜单和日志使用的摘要文本。
        /// </summary>
        /// <returns>多行任务拥堵摘要。</returns>
        public string ToSummaryText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            StringBuilder builder = new StringBuilder(256);
            builder.AppendFormat(
                "任务队列拥堵: {0} | 总数 {1} | 等待 {2} | 进行中 {3}",
                WorkerTaskCongestionTool.GetLevelName(this.Level),
                this.TotalTaskCount,
                this.WaitingTaskCount,
                this.RunningTaskCount);
            builder.AppendLine();

            if (this.HasPrimaryTaskType)
            {
                builder.AppendFormat(
                    "主要积压: {0} {1} 个等待",
                    WorkerTaskSummaryTool.GetTaskDisplayName(this.PrimaryTaskType),
                    this.PrimaryWaitingTaskCount);
                builder.AppendLine();
            }

            builder.Append(this.AdviceText ?? WorkerTaskCongestionConstant.NoCongestionText);
            return builder.ToString();
        }

        /// <summary>
        /// 生成适合现有 TipUI 展示的短文案。
        /// </summary>
        /// <returns>短 Tip 文案。</returns>
        public string ToTipText()
        {
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                return this.ErrorMessage;
            }

            string levelName = WorkerTaskCongestionTool.GetLevelName(this.Level);
            if (this.HasPrimaryTaskType)
            {
                return $"任务{levelName}: 等待 {this.WaitingTaskCount}，主要积压 " +
                    $"{WorkerTaskSummaryTool.GetTaskDisplayName(this.PrimaryTaskType)} {this.PrimaryWaitingTaskCount}。{this.AdviceText}";
            }

            return $"任务{levelName}: 等待 {this.WaitingTaskCount}。{this.AdviceText}";
        }
    }
}
