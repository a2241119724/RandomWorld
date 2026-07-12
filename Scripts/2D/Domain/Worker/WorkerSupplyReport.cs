namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 工人补给缺口报告（纯数据）。
    /// 由 WorkerSupplyIssueManager 维护，供 HUD、Editor 菜单和其他业务只读查询。
    /// 表现层方法已提取到 WorkerSupplyTool。
    /// </summary>
    [Serializable]
    public class WorkerSupplyReport
    {
        /// <summary>参与扫描的 Worker 数量。</summary>
        public int WorkerCount;

        /// <summary>需要食物的 Worker 数量。</summary>
        public int HungryWorkerCount;

        /// <summary>需要休息的 Worker 数量。</summary>
        public int TiredWorkerCount;

        /// <summary>处于临界状态的 Worker 数量。</summary>
        public int CriticalWorkerCount;

        /// <summary>缺少床位绑定的 Worker 数量。</summary>
        public int WorkerWithoutBedCount;

        /// <summary>仓库食物份数。</summary>
        public int FoodItemCount;

        /// <summary>仓库食物可恢复饥饿值估算。</summary>
        public int FoodRecoverValue;

        /// <summary>当前饥饿 Worker 需要恢复的饥饿值总量。</summary>
        public int RequiredFoodRecoverValue;

        /// <summary>床位总数。</summary>
        public int TotalBedCount;

        /// <summary>已绑定床位数。</summary>
        public int AssignedBedCount;

        /// <summary>空床位数。</summary>
        public int EmptyBedCount;

        /// <summary>是否存在食物缺口。</summary>
        public bool HasFoodShortage;

        /// <summary>是否存在床位缺口。</summary>
        public bool HasBedShortage;

        /// <summary>当前最高优先级补给问题。</summary>
        public WorkerSupplyIssueType PrimaryIssue;

        /// <summary>扫描异常信息，正常为空。</summary>
        public string ErrorMessage;

        /// <summary>单个 Worker 的补给问题列表。</summary>
        public List<WorkerSupplyIssueSnapshot> Issues = new List<WorkerSupplyIssueSnapshot>();

        /// <summary>
        /// 报告是否包含需要提示的问题。
        /// </summary>
        public bool HasIssue
        {
            get
            {
                return this.PrimaryIssue != WorkerSupplyIssueType.None ||
                    this.HasFoodShortage ||
                    this.HasBedShortage ||
                    this.Issues.Count > 0;
            }
        }

        /// <summary>
        /// 构建用于变化检测的签名（纯 StringBuilder，无外部依赖）。
        /// </summary>
        /// <returns>报告关键字段签名。</returns>
        public string BuildSignature()
        {
            StringBuilder builder = new StringBuilder(256);
            builder.Append(this.WorkerCount).Append('|')
                .Append(this.HungryWorkerCount).Append('|')
                .Append(this.TiredWorkerCount).Append('|')
                .Append(this.CriticalWorkerCount).Append('|')
                .Append(this.WorkerWithoutBedCount).Append('|')
                .Append(this.FoodItemCount).Append('|')
                .Append(this.FoodRecoverValue).Append('|')
                .Append(this.RequiredFoodRecoverValue).Append('|')
                .Append(this.TotalBedCount).Append('|')
                .Append(this.AssignedBedCount).Append('|')
                .Append(this.EmptyBedCount).Append('|')
                .Append(this.PrimaryIssue);

            for (int i = 0; i < this.Issues.Count; i++)
            {
                builder.Append('|')
                    .Append(this.Issues[i].WorkerName)
                    .Append(':')
                    .Append(this.Issues[i].IssueType);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 获取可见补给问题数量（委托给规则服务）。
        /// </summary>
        /// <param name="issueCount">问题总数。</param>
        /// <param name="maxLines">最大显示行数。</param>
        /// <returns>可见问题数量。</returns>
        public int GetVisibleIssueCount(int issueCount, int maxLines)
        {
            WorkerSupplyRuleService ruleService = new WorkerSupplyRuleService();
            return ruleService.GetVisibleIssueCount(issueCount, maxLines);
        }
    }

    /// <summary>
    /// 单个 Worker 的补给问题快照（纯数据）。
    /// 只保存显示所需数据，不引用 Worker 实例，避免 UI 层持有运行时对象。
    /// </summary>
    [Serializable]
    public class WorkerSupplyIssueSnapshot
    {
        /// <summary>Worker 名称。</summary>
        public string WorkerName;

        /// <summary>补给问题类型。</summary>
        public WorkerSupplyIssueType IssueType;

        /// <summary>饥饿值比例。</summary>
        public float HungryRatio;

        /// <summary>疲劳值比例。</summary>
        public float TiredRatio;

        /// <summary>是否已绑定床位。</summary>
        public bool HasBed;
    }
}
