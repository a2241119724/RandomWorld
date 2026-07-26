namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    /// <summary>
    /// 工人补给需求的纯算术规则。
    /// </summary>
    public sealed class WorkerSupplyRuleService
    {
        /// <summary>饥饿值绝对阈值：低于该值时判定为需要食物补给。</summary>
        public const float HungryThreshold = 20.0f;

        /// <summary>疲劳值绝对阈值：低于该值时判定为需要休息。</summary>
        public const float TiredThreshold = 20.0f;

        public float GetRecoverNeed(float current, float max)
        {
            float need = max - current;
            return need < 0.0f ? 0.0f : need;
        }

        public float ClampRefreshInterval(float interval)
        {
            return MathHelper.ClampRefreshInterval(interval);
        }

        public int ToRecoverNeedCount(float recoverNeed)
        {
            return MathHelper.CeilToInt(recoverNeed);
        }

        public int GetVisibleIssueCount(int issueCount, int maxIssueCount)
        {
            int safeIssueCount = issueCount < 0 ? 0 : issueCount;
            int safeMaxIssueCount = maxIssueCount < 0 ? 0 : maxIssueCount;
            return safeIssueCount < safeMaxIssueCount ? safeIssueCount : safeMaxIssueCount;
        }

        /// <summary>
        /// 判断 Worker 是否需要食物补给。
        /// 当饥饿值低于绝对阈值或饥饿比例低于 WarningRatio 时返回 true。
        /// </summary>
        /// <param name="snapshot">工人只读状态快照。</param>
        /// <returns>需要食物时返回 true。</returns>
        public bool NeedsFood(WorkerAgentSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            float hungryRatio = MathHelper.GetSafeRatio(snapshot.CurHungry, snapshot.MaxHungry);
            return snapshot.CurHungry <= HungryThreshold ||
                hungryRatio <= WorkerConditionRuleService.WarningRatio;
        }

        /// <summary>
        /// 判断 Worker 是否需要休息。
        /// 当疲劳值低于绝对阈值或疲劳比例低于 WarningRatio 时返回 true。
        /// </summary>
        /// <param name="snapshot">工人只读状态快照。</param>
        /// <returns>需要休息时返回 true。</returns>
        public bool NeedsRest(WorkerAgentSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            float tiredRatio = MathHelper.GetSafeRatio(snapshot.CurTired, snapshot.MaxTired);
            return snapshot.CurTired <= TiredThreshold ||
                tiredRatio <= WorkerConditionRuleService.WarningRatio;
        }

        /// <summary>
        /// 根据工人生存状态和补给缺口判断优先补给问题类型。
        /// 优先级：Critical > BedShortage > HungryWorker > TiredWorker > None。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <param name="needsFood">是否需要食物。</param>
        /// <param name="needsRest">是否需要休息。</param>
        /// <param name="missingBed">是否缺少床位绑定。</param>
        /// <returns>优先展示的问题类型。</returns>
        public WorkerSupplyIssueType GetWorkerPrimaryIssue(
            WorkerConditionState state,
            bool needsFood,
            bool needsRest,
            bool missingBed)
        {
            if (state == WorkerConditionState.Critical)
            {
                return WorkerSupplyIssueType.CriticalWorker;
            }

            if (missingBed)
            {
                return WorkerSupplyIssueType.BedShortage;
            }

            if (needsFood)
            {
                return WorkerSupplyIssueType.HungryWorker;
            }

            if (needsRest)
            {
                return WorkerSupplyIssueType.TiredWorker;
            }

            return WorkerSupplyIssueType.None;
        }
    }
}
