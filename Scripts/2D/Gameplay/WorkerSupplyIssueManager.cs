namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Tool;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 工人补给缺口提示管理器。
    /// 负责只读汇总工人饥饿、疲劳、食物库存和床位绑定状态，并向 Tip、HUD 和 Editor 菜单提供补给缺口报告。
    /// 本类不修改存档结构，不预取或扣减食物，不分配床位，不参与 Photon 同步。
    /// </summary>
    public class WorkerSupplyIssueManager : Singleton<WorkerSupplyIssueManager>, IWorkerSupplyIssueManager
    {
        private WorkerSupplyReport currentReport;
        private string lastSignature = string.Empty;
        private float nextRefreshTime;
        private float lastTipTime = -999.0f;
        private readonly WorkerSupplyRuleService ruleService = new WorkerSupplyRuleService();
        private bool enabled = true;
        private bool tipEnabled = true;

        /// <summary>
        /// 补给缺口报告变化事件。
        /// HUD 或其他表现层可订阅该事件刷新显示。
        /// </summary>
        public event Action<WorkerSupplyReport> OnWorkerSupplyReportChanged;

        /// <summary>
        /// 补给缺口 Tip 请求事件。
        /// 外部可订阅该事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnWorkerSupplyTipRequested;

        /// <summary>
        /// 当前补给缺口报告。
        /// </summary>
        public WorkerSupplyReport CurrentReport
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
        /// 补给缺口监控是否启用。
        /// </summary>
        public bool IsEnabled
        {
            get { return this.enabled; }
        }

        /// <summary>
        /// 启用补给缺口监控。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.Refresh(false);
        }

        /// <summary>
        /// 禁用补给缺口监控。
        /// 禁用后 HUD 仍可显示最后一次报告，但不会继续发出 Tip。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
        }

        /// <summary>
        /// 设置是否显示补给缺口 Tip。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip 提示。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 按固定间隔刷新补给缺口报告。
        /// 该方法可从 `GlobalInit.WorkerUpdate()` 每帧调用，内部会自行节流。
        /// </summary>
        public void Tick()
        {
            if (!this.enabled || Time.time < this.nextRefreshTime)
            {
                return;
            }

            this.nextRefreshTime = Time.time + this.ruleService.ClampRefreshInterval(
                WorkerSupplyConstant.MonitorRefreshInterval);
            this.Refresh(true);
        }

        /// <summary>
        /// 立即刷新补给缺口报告。
        /// </summary>
        /// <param name="allowTip">是否允许在报告变化时显示 Tip。</param>
        /// <returns>新的补给缺口报告。</returns>
        public WorkerSupplyReport Refresh(bool allowTip)
        {
            WorkerSupplyReport report = this.BuildReport();
            string signature = report.BuildSignature();
            bool changed = !signature.Equals(this.lastSignature);

            this.currentReport = report;
            if (changed)
            {
                this.lastSignature = signature;
                this.OnWorkerSupplyReportChanged?.Invoke(report);
            }

            if (allowTip && changed)
            {
                this.TryShowSupplyTip(report);
            }

            return report;
        }

        /// <summary>
        /// 构建适合 HUD 和 Editor 菜单展示的补给缺口摘要。
        /// </summary>
        /// <returns>多行补给缺口摘要文本。</returns>
        public string BuildSummaryText()
        {
            return this.CurrentReport == null
                ? WorkerSupplyConstant.EmptyHudText
                : this.CurrentReport.ToSummaryText();
        }

        /// <summary>
        /// 生成补给缺口报告。
        /// </summary>
        /// <returns>只读报告数据。</returns>
        private WorkerSupplyReport BuildReport()
        {
            WorkerSupplyReport report = new WorkerSupplyReport();
            report.FoodItemCount = this.CountFoodItems();
            report.FoodRecoverValue = report.FoodItemCount * WorkerSupplyConstant.FoodRecoverValuePerItem;
            this.FillBedInfo(report);

            try
            {
                if (WorkerManager.Instance == null || WorkerManager.Instance.Characters == null ||
                    WorkerManager.Instance.Characters.Count == 0)
                {
                    report.PrimaryIssue = WorkerSupplyIssueType.None;
                    return report;
                }

                List<AWorker> workers = WorkerManager.Instance.Characters;
                report.WorkerCount = workers.Count;
                for (int i = 0; i < workers.Count; i++)
                {
                    this.AppendWorkerIssue(report, workers[i]);
                }

                report.HasFoodShortage = report.HungryWorkerCount > 0 &&
                    report.FoodRecoverValue < report.RequiredFoodRecoverValue;
                report.HasBedShortage = report.WorkerWithoutBedCount > 0;
                report.PrimaryIssue = this.GetPrimaryIssue(report);
            }
            catch (Exception exception)
            {
                report.PrimaryIssue = WorkerSupplyIssueType.None;
                report.ErrorMessage = "补给缺口扫描失败: " + exception.Message;
            }

            return report;
        }

        /// <summary>
        /// 将单个 Worker 的补给问题追加到报告。
        /// </summary>
        /// <param name="report">目标报告。</param>
        /// <param name="worker">目标 Worker。</param>
        private void AppendWorkerIssue(WorkerSupplyReport report, AWorker worker)
        {
            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return;
            }

            WorkerConditionState state = WorkerConditionTool.GetState(workerData);
            bool needsFood = WorkerSupplyTool.NeedsFood(workerData);
            bool needsRest = WorkerSupplyTool.NeedsRest(workerData);
            bool hasBed = worker != null && worker.BedItem != null;
            bool missingBed = needsRest && !hasBed;

            if (needsFood)
            {
                report.HungryWorkerCount++;
                report.RequiredFoodRecoverValue += this.ruleService.ToRecoverNeedCount(
                    WorkerSupplyTool.GetHungryRecoverNeed(workerData));
            }

            if (needsRest)
            {
                report.TiredWorkerCount++;
            }

            if (missingBed)
            {
                report.WorkerWithoutBedCount++;
            }

            if (state == WorkerConditionState.Critical)
            {
                report.CriticalWorkerCount++;
            }

            WorkerSupplyIssueType issueType = WorkerSupplyTool.GetWorkerPrimaryIssue(
                state,
                needsFood,
                needsRest,
                missingBed);
            if (issueType == WorkerSupplyIssueType.None)
            {
                return;
            }

            report.Issues.Add(new WorkerSupplyIssueSnapshot
            {
                WorkerName = worker == null ? "未知工人" : worker.name,
                IssueType = issueType,
                HungryRatio = WorkerConditionTool.GetSafeRatio(workerData.CurHungry, workerData.MaxHungry),
                TiredRatio = WorkerConditionTool.GetSafeRatio(workerData.CurTired, workerData.MaxTired),
                HasBed = hasBed,
            });
        }

        /// <summary>
        /// 统计仓库中的食物份数。
        /// </summary>
        /// <returns>当前仓库内可见食物数量；读取失败时返回 0。</returns>
        private int CountFoodItems()
        {
            try
            {
                InventoryManager inventory = InventoryManager.Instance;
                if (inventory == null || inventory.TypeToResource == null ||
                    !inventory.TypeToResource.TryGetValue(AItem.ItemTypeEnum.Food, out Dictionary<Vector3Int, ResourceInfo> foods))
                {
                    return 0;
                }

                int count = 0;
                foreach (KeyValuePair<Vector3Int, ResourceInfo> pair in foods)
                {
                    if (pair.Value != null && pair.Value.Count > 0)
                    {
                        count += pair.Value.Count;
                    }
                }

                return count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 填充床位统计信息。
        /// </summary>
        /// <param name="report">目标报告。</param>
        private void FillBedInfo(WorkerSupplyReport report)
        {
            try
            {
                FurnitureManager furniture = FurnitureManager.Instance;
                if (furniture == null || furniture.BedToWorker == null)
                {
                    return;
                }

                report.TotalBedCount = furniture.BedToWorker.Count;
                foreach (KeyValuePair<Vector3Int, AWorker> pair in furniture.BedToWorker)
                {
                    if (pair.Value == null)
                    {
                        report.EmptyBedCount++;
                    }
                    else
                    {
                        report.AssignedBedCount++;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 获取当前报告的最高优先级问题。
        /// </summary>
        /// <param name="report">补给缺口报告。</param>
        /// <returns>最高优先级问题类型。</returns>
        private WorkerSupplyIssueType GetPrimaryIssue(WorkerSupplyReport report)
        {
            if (report == null)
            {
                return WorkerSupplyIssueType.None;
            }

            if (report.CriticalWorkerCount > 0)
            {
                return WorkerSupplyIssueType.CriticalWorker;
            }

            if (report.HasFoodShortage)
            {
                return WorkerSupplyIssueType.FoodShortage;
            }

            if (report.HasBedShortage)
            {
                return WorkerSupplyIssueType.BedShortage;
            }

            if (report.HungryWorkerCount > 0)
            {
                return WorkerSupplyIssueType.HungryWorker;
            }

            if (report.TiredWorkerCount > 0)
            {
                return WorkerSupplyIssueType.TiredWorker;
            }

            return WorkerSupplyIssueType.None;
        }

        /// <summary>
        /// 在补给缺口变化时显示 Tip。
        /// </summary>
        /// <param name="report">补给缺口报告。</param>
        private void TryShowSupplyTip(WorkerSupplyReport report)
        {
            if (!this.tipEnabled || report == null || report.PrimaryIssue == WorkerSupplyIssueType.None)
            {
                return;
            }

            float now = Time.time;
            if (now - this.lastTipTime < WorkerSupplyConstant.TipCooldownSeconds)
            {
                return;
            }

            this.lastTipTime = now;
            string message = report.ToTipText();
            this.ShowTip(message);
        }

        /// <summary>
        /// 显示补给缺口提示。
        /// 优先使用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示内容。</param>
        private void ShowTip(string message)
        {
            this.OnWorkerSupplyTipRequested?.Invoke(message);

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
                Debug.LogWarning("[WorkerSupply] 显示 Tip 失败: " + exception.Message);
            }

            Debug.Log("[工人补给] " + message);
        }
    }

}
