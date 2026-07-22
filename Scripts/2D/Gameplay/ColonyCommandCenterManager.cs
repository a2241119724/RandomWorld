namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Domain.Worker;
    using System;
    using UnityEngine;

    /// <summary>
    /// A006 殖民地运营指挥中心管理器。
    /// 负责按固定间隔聚合 Worker 人力、任务队列、补给缺口、任务拥堵和阻塞诊断，并向 HUD、Tip 和 Editor 菜单提供只读报告。
    /// 本类不修改任务优先级、不新增或取消任务、不写入存档、不参与 Photon 同步。
    /// </summary>
    public class ColonyCommandCenterManager : Singleton<ColonyCommandCenterManager>, IColonyCommandCenterService
    {
        private ColonyCommandCenterReport currentReport;
        private string lastSignature = string.Empty;
        private float nextRefreshTime;
        private float lastTipTime = -999.0f;
        private readonly ColonyCommandCenterRuleService ruleService = new ColonyCommandCenterRuleService();
        private bool enabled = true;
        private bool tipEnabled = true;

        /// <summary>
        /// 指挥报告变化事件。
        /// HUD 或后续目标系统可订阅该事件刷新展示。
        /// </summary>
        public event Action<ColonyCommandCenterReport> OnCommandReportChanged;

        /// <summary>
        /// 指挥中心 Tip 请求事件。
        /// 外部可订阅该事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnCommandTipRequested;

        /// <summary>
        /// 当前指挥报告。
        /// </summary>
        public ColonyCommandCenterReport CurrentReport
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
        /// 指挥中心监控是否启用。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用指挥中心监控。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.Refresh(false);
        }

        /// <summary>
        /// 禁用指挥中心监控。
        /// 禁用后保留最后一次报告，便于 HUD 和 Editor 查看。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否允许显示指挥中心 Tip。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 按固定间隔刷新指挥报告。
        /// 可从 `GlobalInit.WorkerUpdate()` 每帧调用，内部会自行节流。
        /// </summary>
        public void Tick()
        {
            if (!this.enabled || Time.time < this.nextRefreshTime)
            {
                return;
            }

            this.nextRefreshTime = Time.time + this.ruleService.ClampRefreshInterval(
                ColonyCommandCenterConstant.RefreshInterval);
            this.Refresh(true);
        }

        /// <summary>
        /// 立即刷新指挥报告。
        /// </summary>
        /// <param name="allowTip">是否允许报告变化时显示 Tip。</param>
        /// <returns>新的指挥报告。</returns>
        public ColonyCommandCenterReport Refresh(bool allowTip)
        {
            ColonyCommandCenterReport report = this.BuildReport();
            string signature = report.BuildSignature();
            bool changed = !signature.Equals(this.lastSignature);

            this.currentReport = report;
            if (changed)
            {
                this.lastSignature = signature;
                this.OnCommandReportChanged?.Invoke(report);
            }

            if (allowTip && changed)
            {
                this.TryShowCommandTip(report);
            }

            return report;
        }

        /// <summary>
        /// 构建适合 Editor 菜单或日志展示的纯文本摘要。
        /// </summary>
        /// <returns>多行指挥报告摘要。</returns>
        public string BuildSummaryText()
        {
            return ColonyCommandCenterTool.BuildPlainText(this.CurrentReport);
        }

        /// <summary>
        /// 手动触发一次当前指挥 Tip。
        /// </summary>
        /// <returns>当前报告达到提示等级时返回 true。</returns>
        public bool TryShowCurrentTip()
        {
            ColonyCommandCenterReport report = this.Refresh(false);
            if (report == null || !report.ShouldShowTip)
            {
                return false;
            }

            this.lastTipTime = Time.time;
            this.ShowTip(report.ToTipText());
            return true;
        }

        /// <summary>
        /// 生成当前指挥报告。
        /// </summary>
        /// <returns>只读报告数据。</returns>
        private ColonyCommandCenterReport BuildReport()
        {
            try
            {
                WorkerTaskManager taskManager = Core.ServiceLocator.Get<WorkerTaskManager>();
                WorkerTaskQueueSnapshot queueSnapshot = taskManager == null
                    ? null
                    : taskManager.CreateTaskQueueSnapshot();
                WorkerTaskAssignmentReport assignmentReport = taskManager == null
                    ? ColonyCommandCenterTool.BuildAssignmentReport(null, Core.ServiceLocator.Get<WorkerManager>().Characters)
                    : taskManager.CreateTaskAssignmentReport();
                WorkerSupplyReport supplyReport = Core.ServiceLocator.Get<WorkerSupplyIssueManager>().Refresh(false);
                WorkerTaskCongestionReport congestionReport = Core.ServiceLocator.Get<WorkerTaskCongestionAdvisor>().Refresh(false);

                return ColonyCommandCenterTool.BuildCommandReport(
                    queueSnapshot,
                    assignmentReport,
                    supplyReport,
                    congestionReport,
                    Time.time);
            }
            catch (Exception exception)
            {
                return new ColonyCommandCenterReport
                {
                    AlertLevel = ColonyCommandAlertLevel.Notice,
                    FocusText = "指挥中心扫描暂不可用。",
                    AdviceText = "继续游戏即可，诊断层已降级为安全提示。",
                    ErrorMessage = "指挥中心扫描失败: " + exception.Message,
                    UpdatedTime = Time.time,
                };
            }
        }

        /// <summary>
        /// 在报告变化且达到警告等级时显示 Tip。
        /// </summary>
        /// <param name="report">指挥报告。</param>
        private void TryShowCommandTip(ColonyCommandCenterReport report)
        {
            if (!this.tipEnabled || report == null || !report.ShouldShowTip)
            {
                return;
            }

            float now = Time.time;
            if (now - this.lastTipTime < ColonyCommandCenterConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTime = now;
            this.ShowTip(report.ToTipText());
        }

        /// <summary>
        /// 显示指挥中心提示。
        /// 优先复用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnCommandTipRequested?.Invoke(message);

            try
            {
                AWorkerTask.ShowTipProvider(message);
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(ColonyCommandCenterConstant.LogPrefix + " 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log(ColonyCommandCenterConstant.LogPrefix + " " + message);
        }
    }
}
