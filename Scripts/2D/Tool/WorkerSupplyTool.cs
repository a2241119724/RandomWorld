namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Domain.Worker;
    /// <summary>
    /// 工人补给缺口工具类。
    /// 只负责补给缺口判断、百分比格式化和显示文案生成，不持有运行时状态，不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
    /// 使用边界：本工具不会扣减食物、不会预取资源、不会分配床位，只提供只读提示所需的计算结果。
    /// </summary>
    public static class WorkerSupplyTool
    {
        private static readonly WorkerConditionRuleService ConditionRuleService = new WorkerConditionRuleService();
        private static readonly WorkerSupplyRuleService SupplyRuleService = new WorkerSupplyRuleService();

        /// <summary>
        /// 判断 Worker 是否需要食物补给。
        /// </summary>
        /// <param name="workerData">Worker 数据。</param>
        /// <returns>饥饿值低于阈值或警戒比例时返回 true。</returns>
        public static bool NeedsFood(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return false;
            }

            var snapshot = new WorkerAgentSnapshot(
                workerId: 0L,
                position: default,
                isIdle: false,
                isPaused: false,
                curHungry: workerData.CurHungry,
                maxHungry: workerData.MaxHungry,
                curTired: workerData.CurTired,
                maxTired: workerData.MaxTired);
            return SupplyRuleService.NeedsFood(snapshot);
        }

        /// <summary>
        /// 判断 Worker 是否需要休息。
        /// </summary>
        /// <param name="workerData">Worker 数据。</param>
        /// <returns>疲劳值低于阈值或警戒比例时返回 true。</returns>
        public static bool NeedsRest(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return false;
            }

            var snapshot = new WorkerAgentSnapshot(
                workerId: 0L,
                position: default,
                isIdle: false,
                isPaused: false,
                curHungry: workerData.CurHungry,
                maxHungry: workerData.MaxHungry,
                curTired: workerData.CurTired,
                maxTired: workerData.MaxTired);
            return SupplyRuleService.NeedsRest(snapshot);
        }

        /// <summary>
        /// 计算 Worker 吃满所需的饥饿恢复值。
        /// </summary>
        /// <param name="workerData">Worker 数据。</param>
        /// <returns>需要恢复的饥饿值，最小为 0。</returns>
        public static float GetHungryRecoverNeed(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return 0.0f;
            }

            return SupplyRuleService.GetRecoverNeed(workerData.CurHungry, workerData.MaxHungry);
        }

        /// <summary>
        /// 获取单个 Worker 的优先补给问题类型。委托至 WorkerSupplyRuleService。
        /// </summary>
        /// <param name="state">Worker 当前状态。</param>
        /// <param name="needsFood">是否需要食物。</param>
        /// <param name="needsRest">是否需要休息。</param>
        /// <param name="missingBed">是否缺少床位绑定。</param>
        /// <returns>优先展示的问题类型。</returns>
        public static WorkerSupplyIssueType GetWorkerPrimaryIssue(
            WorkerConditionState state,
            bool needsFood,
            bool needsRest,
            bool missingBed)
        {
            return SupplyRuleService.GetWorkerPrimaryIssue(state, needsFood, needsRest, missingBed);
        }

        /// <summary>
        /// 获取补给问题中文名称。
        /// </summary>
        /// <param name="issueType">补给问题类型。</param>
        /// <returns>适合 UI 和日志展示的中文名称。</returns>
        public static string GetIssueName(WorkerSupplyIssueType issueType)
        {
            switch (issueType)
            {
                case WorkerSupplyIssueType.FoodShortage:
                    return "食物不足";
                case WorkerSupplyIssueType.BedShortage:
                    return "缺少床位";
                case WorkerSupplyIssueType.HungryWorker:
                    return "需要食物";
                case WorkerSupplyIssueType.TiredWorker:
                    return "需要休息";
                case WorkerSupplyIssueType.CriticalWorker:
                    return "临界停工";
                default:
                    return "补给正常";
            }
        }

        /// <summary>
        /// 获取补给问题 RichText 颜色。
        /// </summary>
        /// <param name="issueType">补给问题类型。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetIssueRichColor(WorkerSupplyIssueType issueType)
        {
            switch (issueType)
            {
                case WorkerSupplyIssueType.FoodShortage:
                case WorkerSupplyIssueType.HungryWorker:
                    return PixelUITheme.RichGold;
                case WorkerSupplyIssueType.BedShortage:
                case WorkerSupplyIssueType.TiredWorker:
                    return PixelUITheme.RichLavender;
                case WorkerSupplyIssueType.CriticalWorker:
                    return PixelUITheme.RichCoral;
                default:
                    return PixelUITheme.RichMint;
            }
        }

        /// <summary>
        /// 生成单个 Worker 的补给问题行。
        /// </summary>
        /// <param name="workerName">Worker 名称。</param>
        /// <param name="issueType">补给问题类型。</param>
        /// <param name="hungryRatio">饥饿值比例。</param>
        /// <param name="tiredRatio">疲劳值比例。</param>
        /// <param name="hasBed">是否已绑定床位。</param>
        /// <returns>适合 HUD 展示的一行 RichText 文案。</returns>
        public static string BuildWorkerIssueLine(
            string workerName,
            WorkerSupplyIssueType issueType,
            float hungryRatio,
            float tiredRatio,
            bool hasBed)
        {
            string color = GetIssueRichColor(issueType);
            string bedText = hasBed ? "有床" : "无床";
            return $"<color={color}>{workerName}</color> {GetIssueName(issueType)} | " +
                $"饥饿 {FormatPercent(hungryRatio)} | 疲劳 {FormatPercent(tiredRatio)} | {bedText}";
        }

        /// <summary>
        /// 生成补给缺口 Tip 文案。
        /// </summary>
        /// <param name="issueType">优先问题类型。</param>
        /// <param name="hungryWorkerCount">需要食物的工人数。</param>
        /// <param name="tiredWorkerCount">需要休息的工人数。</param>
        /// <param name="foodItemCount">仓库食物份数。</param>
        /// <param name="workerWithoutBedCount">缺少床位绑定的工人数。</param>
        /// <returns>适合游戏内 Tip 显示的短文案。</returns>
        public static string BuildTipText(
            WorkerSupplyIssueType issueType,
            int hungryWorkerCount,
            int tiredWorkerCount,
            int foodItemCount,
            int workerWithoutBedCount)
        {
            if (issueType == WorkerSupplyIssueType.None)
            {
                return WorkerSupplyConstant.NoIssueText;
            }

            return $"{GetIssueName(issueType)}: 饥饿工人 {hungryWorkerCount}，疲劳工人 {tiredWorkerCount}，" +
                $"食物 {foodItemCount} 份，缺床 {workerWithoutBedCount} 人";
        }

        /// <summary>
        /// 格式化比例为百分比。
        /// </summary>
        /// <param name="ratio">0 到 1 的比例。</param>
        /// <returns>百分比文本。</returns>
        public static string FormatPercent(float ratio)
        {
            return $"{ConditionRuleService.ToPercentInt(ratio)}%";
        }
    }
}
