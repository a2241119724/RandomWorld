namespace LAB2D.Domain.Worker
{
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    /// <summary>
    /// 工人状态比率和显示值的纯算术规则。
    /// </summary>
    public sealed class WorkerConditionRuleService
    {
        /// <summary>进入饥饿或疲劳提示的比例阈值（疲劳比例高于 1-该值 触发警告）。</summary>
        public const float WarningRatio = 0.35f;

        /// <summary>进入濒临停工状态的比例阈值（疲劳比例高于 1-该值 触发最强惩罚）。</summary>
        public const float CriticalRatio = 0.05f;

        /// <summary>健康状况下移动速度倍率。</summary>
        public const float MoveSpeedNormal = 1.0f;

        /// <summary>单项饥饿时的移动速度倍率。</summary>
        public const float HungryMoveSpeedMultiplier = 0.86f;

        /// <summary>单项疲劳时的移动速度倍率。</summary>
        public const float TiredMoveSpeedMultiplier = 0.9f;

        /// <summary>饥饿且疲劳时的移动速度倍率。</summary>
        public const float ExhaustedMoveSpeedMultiplier = 0.72f;

        /// <summary>濒临停工时的移动速度倍率。</summary>
        public const float CriticalMoveSpeedMultiplier = 0.58f;

        /// <summary>单项饥饿时的普通任务进度倍率。</summary>
        public const float HungryWorkProgressMultiplier = 0.82f;

        /// <summary>单项疲劳时的普通任务进度倍率。</summary>
        public const float TiredWorkProgressMultiplier = 0.76f;

        /// <summary>饥饿且疲劳时的普通任务进度倍率。</summary>
        public const float ExhaustedWorkProgressMultiplier = 0.6f;

        /// <summary>濒临停工时的普通任务进度倍率。</summary>
        public const float CriticalWorkProgressMultiplier = 0.45f;

        /// <summary>压力满时的工作进度倍率下限（70% 以上压力开始线性下滑到该值）。</summary>
        public const float StressWorkMultiplierMin = 0.7f;

        /// <summary>压力惩罚起始比例：压力比例超过该值才开始掉效率。</summary>
        public const float StressWorkPenaltyStartRatio = 0.7f;

        /// <summary>低士气下的工作进度倍率下限（士气低于 40% 开始线性下滑到该值）。</summary>
        public const float MoraleWorkMultiplierMin = 0.75f;

        /// <summary>士气惩罚起始比例：士气比例低于该值才开始掉效率。</summary>
        public const float MoraleWorkPenaltyBelowRatio = 0.4f;

        public float GetSafeRatio(float current, float max)
        {
            if (max <= 0.0f)
            {
                return 0.0f;
            }

            return MathHelper.Clamp01(current / max);
        }

        public int ToPercentInt(float ratio)
        {
            return MathHelper.ToPercentInt(ratio);
        }

        /// <summary>
        /// 根据 WorkerAgentSnapshot 计算工人生存状态。
        /// 疲劳语义：CurTired 为累积疲劳值（越大越疲）。
        /// 优先判定 Critical（饥饿低于临界比例 或 疲劳高于 1-CriticalRatio），
        /// 其次判定 Exhausted（双高/双低），然后依次判定 Hungry 和 Tired。
        /// </summary>
        /// <param name="snapshot">工人只读状态快照。</param>
        /// <returns>工人生存状态。</returns>
        public WorkerConditionState GetState(WorkerAgentSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return WorkerConditionState.Healthy;
            }

            float hungryRatio = GetSafeRatio(snapshot.CurHungry, snapshot.MaxHungry);
            float tiredRatio = GetSafeRatio(snapshot.CurTired, snapshot.MaxTired);

            bool criticalHungry = snapshot.CurHungry <= 0.0f || hungryRatio <= CriticalRatio;
            bool criticalTired = snapshot.CurTired >= snapshot.MaxTired || tiredRatio >= 1.0f - CriticalRatio;
            if (criticalHungry || criticalTired)
            {
                return WorkerConditionState.Critical;
            }

            bool hungry = hungryRatio <= WarningRatio;
            bool tired = tiredRatio >= 1.0f - WarningRatio;
            if (hungry && tired)
            {
                return WorkerConditionState.Exhausted;
            }

            if (hungry)
            {
                return WorkerConditionState.Hungry;
            }

            if (tired)
            {
                return WorkerConditionState.Tired;
            }

            return WorkerConditionState.Healthy;
        }

        /// <summary>
        /// 根据工人生存状态获取移动速度倍率。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <returns>移动速度倍率，1 表示不变化。</returns>
        public float GetMoveSpeedMultiplier(WorkerConditionState state)
        {
            switch (state)
            {
                case WorkerConditionState.Hungry:
                    return HungryMoveSpeedMultiplier;
                case WorkerConditionState.Tired:
                    return TiredMoveSpeedMultiplier;
                case WorkerConditionState.Exhausted:
                    return ExhaustedMoveSpeedMultiplier;
                case WorkerConditionState.Critical:
                    return CriticalMoveSpeedMultiplier;
                default:
                    return MoveSpeedNormal;
            }
        }

        /// <summary>
        /// 根据工人生存状态获取任务进度倍率。
        /// Eat/Sleep 类型任务不受任何惩罚，始终返回 1。
        /// </summary>
        /// <param name="state">工人生存状态。</param>
        /// <param name="isEatOrSleepTask">是否为吃饭或睡觉任务。</param>
        /// <returns>任务进度倍率。</returns>
        public float GetTaskProgressMultiplier(WorkerConditionState state, bool isEatOrSleepTask)
        {
            if (isEatOrSleepTask)
            {
                return 1.0f;
            }

            switch (state)
            {
                case WorkerConditionState.Hungry:
                    return HungryWorkProgressMultiplier;
                case WorkerConditionState.Tired:
                    return TiredWorkProgressMultiplier;
                case WorkerConditionState.Exhausted:
                    return ExhaustedWorkProgressMultiplier;
                case WorkerConditionState.Critical:
                    return CriticalWorkProgressMultiplier;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 压力对工作进度的惩罚倍率：压力比例超过 <see cref="StressWorkPenaltyStartRatio"/>
        /// 后线性下滑到 <see cref="StressWorkMultiplierMin"/>。吃饭/睡觉任务由调用方豁免。
        /// </summary>
        /// <param name="stressRatio">压力比例（0~1）。</param>
        /// <returns>工作进度倍率。</returns>
        public float GetStressWorkMultiplier(float stressRatio)
        {
            if (stressRatio <= StressWorkPenaltyStartRatio)
            {
                return 1.0f;
            }

            float t = MathHelper.Clamp01((stressRatio - StressWorkPenaltyStartRatio) / (1.0f - StressWorkPenaltyStartRatio));
            return 1.0f - ((1.0f - StressWorkMultiplierMin) * t);
        }

        /// <summary>
        /// 士气对工作进度的惩罚倍率：士气比例低于 <see cref="MoraleWorkPenaltyBelowRatio"/>
        /// 后线性下滑到 <see cref="MoraleWorkMultiplierMin"/>。吃饭/睡觉任务由调用方豁免。
        /// </summary>
        /// <param name="moraleRatio">士气比例（0~1）。</param>
        /// <returns>工作进度倍率。</returns>
        public float GetMoraleWorkMultiplier(float moraleRatio)
        {
            if (moraleRatio >= MoraleWorkPenaltyBelowRatio)
            {
                return 1.0f;
            }

            float t = MathHelper.Clamp01((MoraleWorkPenaltyBelowRatio - moraleRatio) / MoraleWorkPenaltyBelowRatio);
            return 1.0f - ((1.0f - MoraleWorkMultiplierMin) * t);
        }
    }
}
