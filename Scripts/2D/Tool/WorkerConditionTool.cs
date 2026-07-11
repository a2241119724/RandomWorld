namespace LAB2D.Tool
{
    using LAB2D;
    using LAB2D.Domain.Worker;
    /// <summary>
    /// 工人饥饿与疲劳状态工具类。
    /// 只负责状态计算、倍率计算和展示文本格式化，不访问 Scene、Prefab、存档、Photon 或 AssetBundle。
    /// 使用边界：本工具不持有运行时状态，也不主动修改 Worker 数据。
    /// </summary>
    public static class WorkerConditionTool
    {
        private static readonly WorkerConditionRuleService RuleService = new WorkerConditionRuleService();

        /// <summary>
        /// 安全获取 WorkerData。
        /// </summary>
        /// <param name="worker">目标工人。</param>
        /// <param name="workerData">返回的工人数据。</param>
        /// <returns>成功获取时返回 true。</returns>
        public static bool TryGetWorkerData(AWorker worker, out AWorker.WorkerData workerData)
        {
            workerData = null;
            if (worker == null || worker.CharacterDataLAB == null)
            {
                return false;
            }

            workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return workerData != null;
        }

        /// <summary>
        /// 计算安全比例。
        /// </summary>
        /// <param name="current">当前值。</param>
        /// <param name="max">最大值。</param>
        /// <returns>0 到 1 之间的比例；最大值无效时返回 0。</returns>
        public static float GetSafeRatio(float current, float max)
        {
            return RuleService.GetSafeRatio(current, max);
        }

        /// <summary>
        /// 根据 WorkerData 计算工人生存状态。
        /// </summary>
        /// <param name="workerData">工人数据。</param>
        /// <returns>工人生存状态。</returns>
        public static WorkerConditionState GetState(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return WorkerConditionState.Healthy;
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
            return RuleService.GetState(snapshot);
        }

        /// <summary>
        /// 获取状态中文名称。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <returns>用于 UI、Tip 和日志展示的中文名称。</returns>
        public static string GetStateName(WorkerConditionState state)
        {
            switch (state)
            {
                case WorkerConditionState.Hungry:
                    return "饥饿";
                case WorkerConditionState.Tired:
                    return "疲劳";
                case WorkerConditionState.Exhausted:
                    return "饥饿疲劳";
                case WorkerConditionState.Critical:
                    return "濒临停工";
                default:
                    return "状态良好";
            }
        }

        /// <summary>
        /// 获取状态 RichText 颜色。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <returns>HTML 颜色字符串。</returns>
        public static string GetStateRichColor(WorkerConditionState state)
        {
            switch (state)
            {
                case WorkerConditionState.Hungry:
                    return PixelUITheme.RichGold;
                case WorkerConditionState.Tired:
                    return PixelUITheme.RichLavender;
                case WorkerConditionState.Exhausted:
                    return PixelUITheme.RichCoral;
                case WorkerConditionState.Critical:
                    return PixelUITheme.RichPink;
                default:
                    return PixelUITheme.RichMint;
            }
        }

        /// <summary>
        /// 获取工人移动速度倍率。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <returns>移动速度倍率，1 表示不变化。</returns>
        public static float GetMoveSpeedMultiplier(WorkerConditionState state)
        {
            return RuleService.GetMoveSpeedMultiplier(state);
        }

        /// <summary>
        /// 获取工人普通任务进度倍率。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <param name="taskType">任务类型。</param>
        /// <returns>任务进度倍率，吃饭和睡觉任务始终返回 1。</returns>
        public static float GetTaskProgressMultiplier(WorkerConditionState state, AWorkerTask.WorkerTaskTypeEnum taskType)
        {
            bool isEatOrSleepTask = taskType == AWorkerTask.WorkerTaskTypeEnum.Eat ||
                taskType == AWorkerTask.WorkerTaskTypeEnum.Sleep;
            return RuleService.GetTaskProgressMultiplier(state, isEatOrSleepTask);
        }

        /// <summary>
        /// 生成单个工人的状态行。
        /// </summary>
        /// <param name="workerName">工人名称。</param>
        /// <param name="state">工人生存状态。</param>
        /// <param name="hungryRatio">饥饿比例。</param>
        /// <param name="tiredRatio">疲劳比例。</param>
        /// <param name="moveMultiplier">移动速度倍率。</param>
        /// <param name="workMultiplier">工作进度倍率。</param>
        /// <returns>适合 HUD 和 Editor 展示的一行文本。</returns>
        public static string BuildConditionLine(
            string workerName,
            WorkerConditionState state,
            float hungryRatio,
            float tiredRatio,
            float moveMultiplier,
            float workMultiplier)
        {
            string color = GetStateRichColor(state);
            return $"<color={color}>{workerName}</color> {GetStateName(state)} | " +
                $"饥饿 {FormatPercent(hungryRatio)} | 疲劳 {FormatPercent(tiredRatio)} | " +
                $"移动 {moveMultiplier:0.00}x | 工作 {workMultiplier:0.00}x";
        }

        /// <summary>
        /// 生成状态变化提示文本。
        /// </summary>
        /// <param name="workerName">工人名称。</param>
        /// <param name="state">工人生存状态。</param>
        /// <param name="moveMultiplier">移动速度倍率。</param>
        /// <param name="workMultiplier">工作进度倍率。</param>
        /// <returns>适合 Tip 展示的短文本。</returns>
        public static string BuildTipText(string workerName, WorkerConditionState state, float moveMultiplier, float workMultiplier)
        {
            if (state == WorkerConditionState.Healthy)
            {
                return $"{workerName} 状态恢复，移动与工作效率恢复正常。";
            }

            return $"{workerName} {GetStateName(state)}：移动 {moveMultiplier:0.00}x，工作 {workMultiplier:0.00}x。";
        }

        /// <summary>
        /// 格式化百分比。
        /// </summary>
        /// <param name="ratio">0 到 1 的比例。</param>
        /// <returns>百分比文本。</returns>
        public static string FormatPercent(float ratio)
        {
            return $"{RuleService.ToPercentInt(ratio)}%";
        }
    }
}
