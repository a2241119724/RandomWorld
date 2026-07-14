namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Tool;
    using System;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 工人任务队列拥堵提示管理器。
    /// 负责只读读取任务队列快照，生成拥堵等级与玩家建议，并按冷却规则请求现有 Tip UI 展示。
    /// 本类不修改 Worker 任务优先级，不新增或取消任务，不写入存档，不参与 Photon 同步。
    /// </summary>
    public class WorkerTaskCongestionAdvisor : Singleton<WorkerTaskCongestionAdvisor>, IWorkerTaskCongestionAdvisor
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
}
