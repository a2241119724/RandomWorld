namespace LAB2D.Domain.Worker
{
    using LAB2D.Constant;

    /// <summary>
    /// 玩家命令接受判定 — 纯规则服务，零 Unity 依赖，可单测。
    /// 决定 Worker 对玩家悬赏是 接受 / 拖延 / 拒绝。
    ///
    /// 判定优先级（从高到低）：
    /// 1. 生存硬阻断 → Delay（交给现有紧急打断/BlocksWhenHungry，绝不饿死/累倒）
    /// 2. 拖延冷却中（刚拒绝过）→ Delay（静默，不重复反馈）
    /// 3. 好感度基础门控 → Refuse（现有规则保持兼容）
    /// 4. 怨恨门控 → Refuse（≥85 必拒；≥60 概率拒）
    /// 5. 感恩覆盖 → Accept（玩家救过我，愿意接）
    /// 6. 意愿度（服从意愿+心情/士气加权）过低 → Delay
    /// 7. 随机个人因素（低概率，心情差时略高）→ Delay
    ///
    /// 节奏基准：普通 Worker 仅 ~6% 随机拖延；怨恨≥60 才显著拒；怨恨≥85 必拒。
    /// </summary>
    public static class CommandAcceptanceRuleService
    {
        // ---- 生存硬阻断 ----
        /// <summary>饥饿低于该值 → 强制拖延（交给现有紧急打断）。</summary>
        public const float SurvivalHungryThreshold = 15f;

        /// <summary>疲劳高于 MaxTired-该值 → 强制拖延。</summary>
        public const float SurvivalTiredTolerance = 15f;

        /// <summary>精气神低于该值 → 强制拖延。</summary>
        public const float SurvivalSpiritThreshold = 10f;

        // ---- 怨恨门控 ----
        /// <summary>怨恨 ≥ 该值 → 必拒。</summary>
        public const float ResentmentRefuseHard = 85f;

        /// <summary>怨恨 ≥ 该值 → 概率拒。</summary>
        public const float ResentmentRefuseProbThreshold = 60f;

        /// <summary>怨恨概率拒的判定概率（randomValue &lt; 该值 时拒）。</summary>
        public const float ResentmentRefuseProb = 0.6f;

        // ---- 感恩覆盖 ----
        /// <summary>感恩 ≥ 该值 → 覆盖怨恨/意愿，愿意接。</summary>
        public const float GratitudeOverrideThreshold = 65f;

        // ---- 意愿度 ----
        /// <summary>意愿度低于该值 → 拖延。</summary>
        public const float WillingnessDelayThreshold = 25f;

        /// <summary>意愿度公式中心情的权重。</summary>
        public const float MoodWeight = 0.3f;

        /// <summary>意愿度公式中士气的权重。</summary>
        public const float MoraleWeight = 0.2f;

        // ---- 随机个人因素 ----
        /// <summary>随机拖延基准概率。</summary>
        public const float RandomDelayChance = 0.06f;

        /// <summary>心情低于该值时随机拖延概率升高。</summary>
        public const float LowMoodThreshold = 20f;

        /// <summary>心情过低时的随机拖延概率。</summary>
        public const float LowMoodRandomDelayChance = 0.15f;

        /// <summary>
        /// 评估 Worker 对玩家命令的接受意愿。
        /// </summary>
        /// <param name="curHungry">当前饥饿值。</param>
        /// <param name="maxHungry">最大饥饿值。</param>
        /// <param name="curTired">当前疲劳值。</param>
        /// <param name="maxTired">最大疲劳值。</param>
        /// <param name="curSpirit">当前精气神。</param>
        /// <param name="playerFavorability">对玩家好感度。</param>
        /// <param name="mood">心情（人格 Mood）。</param>
        /// <param name="morale">士气。</param>
        /// <param name="resentment">对玩家怨恨。</param>
        /// <param name="gratitude">对玩家感恩。</param>
        /// <param name="willingnessToObey">服从意愿。</param>
        /// <param name="delayCooldownRemaining">拖延冷却剩余秒数（&gt;0 = 在冷却中）。</param>
        /// <param name="randomValue">[0,1) 随机值（单次掷骰，保证可测试）。</param>
        /// <param name="reasonKey">输出判定理由键（WorkerMindConstant.Reason*）。</param>
        /// <returns>接受/拖延/拒绝。</returns>
        public static CommandAcceptance Evaluate(
            float curHungry, float maxHungry,
            float curTired, float maxTired,
            float curSpirit,
            float playerFavorability,
            float mood, float morale,
            float resentment, float gratitude, float willingnessToObey,
            float delayCooldownRemaining,
            float randomValue,
            out string reasonKey)
        {
            // 1. 生存硬阻断 → 拖延（交给现有紧急打断/BlocksWhenHungry）
            if (curHungry < SurvivalHungryThreshold
                || curTired > maxTired - SurvivalTiredTolerance
                || curSpirit < SurvivalSpiritThreshold)
            {
                reasonKey = WorkerMindConstant.ReasonSurvival;
                return CommandAcceptance.Delay;
            }

            // 2. 拖延冷却中（刚拒绝过）→ 静默拖延，不重复反馈
            if (delayCooldownRemaining > 0f)
            {
                reasonKey = WorkerMindConstant.ReasonCooldown;
                return CommandAcceptance.Delay;
            }

            // 3. 好感度基础门控（现有规则保持兼容）
            if (playerFavorability < FavorabilityRuleService.PlayerBountyRefuseThreshold)
            {
                reasonKey = WorkerMindConstant.ReasonFavorability;
                return CommandAcceptance.Refuse;
            }

            // 4. 怨恨门控
            if (resentment >= ResentmentRefuseHard)
            {
                reasonKey = WorkerMindConstant.ReasonResentment;
                return CommandAcceptance.Refuse;
            }
            if (resentment >= ResentmentRefuseProbThreshold && randomValue < ResentmentRefuseProb)
            {
                reasonKey = WorkerMindConstant.ReasonResentment;
                return CommandAcceptance.Refuse;
            }

            // 5. 感恩覆盖：玩家救过我 → 这次愿意接
            if (gratitude >= GratitudeOverrideThreshold)
            {
                reasonKey = WorkerMindConstant.ReasonGratitude;
                return CommandAcceptance.Accept;
            }

            // 6. 意愿度 = 服从意愿 + 心情/士气加权
            float willingness = 50f + (willingnessToObey - 50f)
                              + (mood - 50f) * MoodWeight
                              + (morale - 50f) * MoraleWeight;
            if (willingness < WillingnessDelayThreshold)
            {
                reasonKey = WorkerMindConstant.ReasonWillingness;
                return CommandAcceptance.Delay;
            }

            // 7. 随机个人因素（低概率，心情差时略高）
            if (randomValue < RandomDelayChance
                || (mood < LowMoodThreshold && randomValue < LowMoodRandomDelayChance))
            {
                reasonKey = WorkerMindConstant.ReasonRandomMood;
                return CommandAcceptance.Delay;
            }

            reasonKey = WorkerMindConstant.ReasonAccept;
            return CommandAcceptance.Accept;
        }
    }
}
