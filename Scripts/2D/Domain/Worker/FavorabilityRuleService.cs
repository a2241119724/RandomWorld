namespace LAB2D.Domain.Worker
{
    using System;

    /// <summary>
    /// 好感度纯算术规则 — 零 Unity 依赖，可在 Editor 单测中直接使用。
    /// 数值范围 [0, 100]，初始 50。
    /// 阈值/增减量等所有业务魔法值集中于此，Constant 层反向委托引用。
    /// </summary>
    public static class FavorabilityRuleService
    {
        // ---- 数值范围 ----
        public const float MinFavorability = 0f;
        public const float MaxFavorability = 100f;
        public const float InitialFavorability = 50f;

        // ---- 门控阈值 ----
        /// <summary>对玩家好感低于该值 → 拒绝接受玩家悬赏/指派。</summary>
        public const float PlayerBountyRefuseThreshold = 35f;
        /// <summary>对其他 Worker 好感低于该值 → 拒绝接受其悬赏。</summary>
        public const float WorkerBountyRefuseThreshold = 40f;
        /// <summary>卖者对买者好感低于该值 → 拒卖。</summary>
        public const float TradeRefuseThreshold = 30f;

        // ---- 交易价格 ----
        public const float TradePriceStep = 0.004f;  // 每点好感度对价格的修正幅度
        public const float TradePriceMin = 0.7f;     // 最低折扣
        public const float TradePriceMax = 1.3f;     // 最高加价

        // ---- 增减量 ----
        public const float AttackToPlayerDelta = -15f;    // Player 攻击 Worker，每次命中
        public const float KillToPlayerBonus = -10f;      // Player 击杀 Worker，额外惩罚
        public const float WorkerAttackDelta = -10f;      // Worker 互殴，受害→肇事
        public const float HelpVsEnemyDelta = 8f;         // Player 在 Worker 附近击伤敌方
        public const float PlayerBountyCompleteDelta = 8f;    // 完成 Player 悬赏
        public const float PlayerBountyCompleteLowDelta = 4f; // 低奖励悬赏
        public const float PlayerBountyLowReward = 40f;
        public const float WorkerBountyCompleteDelta = 6f;    // Worker 间悬赏完成（双向）
        public const float TradeSuccessBuyerDelta = 4f;       // 交易成功，buyer→seller
        public const float TradeSuccessSellerDelta = 2f;      // 交易成功，seller→buyer
        public const float TradeRejectDelta = -3f;            // 交易被拒，buyer→seller
        public const float ConversationDelta = 2f;            // 一次对话结束
        public const float ConversationDailyCap = 10f;        // 每日对话好感上限
        public const float ProximityPerTick = 0.1f;           // 接近/共事每 tick 增量
        public const float ProximityPerTickWithPlayer = 0.15f; // 与 Player 接近每 tick
        public const float ProximityMaxPerPair = 10f;         // 每对累计上限

        // ---- Mood 联动 ----
        public const float MoodChangeThreshold = 5f;  // |delta| 低于此不联动
        public const float MoodChangeScale = 0.05f;   // delta → Mood 缩放
        public const float MoodChangeMax = 5f;        // 单次 Mood 变化上限

        // ---- 纯函数 ----

        /// <summary>好感度 Clamp 到 [0, 100]。</summary>
        public static float Clamp(float value)
        {
            return Math.Max(MinFavorability, Math.Min(MaxFavorability, value));
        }

        /// <summary>是否愿意接受玩家悬赏（好感 &gt;= 阈值）。</summary>
        public static bool IsWillingForPlayerBounty(float favorability)
        {
            return favorability >= PlayerBountyRefuseThreshold;
        }

        /// <summary>是否愿意接受其他 Worker 的悬赏（好感 &gt;= 阈值）。</summary>
        public static bool IsWillingForWorkerBounty(float favorability)
        {
            return favorability >= WorkerBountyRefuseThreshold;
        }

        /// <summary>是否愿意出售给指定买者（卖者对买者好感 &gt;= 阈值）。</summary>
        public static bool IsWillingToTrade(float sellerFavorabilityToBuyer)
        {
            return sellerFavorabilityToBuyer >= TradeRefuseThreshold;
        }

        /// <summary>
        /// 交易价格乘数：高好感 → 折扣（&lt;1），低好感 → 加价（&gt;1）。
        /// 公式：1 + (50 - 好感) * 0.004，clamp 到 [0.7, 1.3]。
        /// </summary>
        public static float GetTradePriceMultiplier(float favorability)
        {
            float multiplier = 1f + (InitialFavorability - favorability) * TradePriceStep;
            return Math.Max(TradePriceMin, Math.Min(TradePriceMax, multiplier));
        }

        /// <summary>好感度态度标签。&lt;30 敌对 / 30-49 疏远 / 50-69 友好 / 70-84 亲近 / ≥85 挚友。</summary>
        public static string GetAttitudeLabel(float favorability)
        {
            if (favorability >= 85f) return "挚友";
            if (favorability >= 70f) return "亲近";
            if (favorability >= 50f) return "友好";
            if (favorability >= 30f) return "疏远";
            return "敌对";
        }

        /// <summary>完成 Player 悬赏的好感增益（低奖励悬赏给较少好感）。</summary>
        public static float GetPlayerBountyCompleteGain(float reward)
        {
            return reward < PlayerBountyLowReward ? PlayerBountyCompleteLowDelta : PlayerBountyCompleteDelta;
        }

        /// <summary>对话是否仍在每日限额内。</summary>
        public static bool IsConversationAllowed(int talkCountToday, int cap)
        {
            return talkCountToday < cap;
        }

        /// <summary>好感变化对 Mood 的微调：|delta|&lt;阈值不生效，clamp 到 ±上限。</summary>
        public static float GetMoodDelta(float favorabilityDelta)
        {
            float abs = Math.Abs(favorabilityDelta);
            if (abs < MoodChangeThreshold) return 0f;
            float delta = favorabilityDelta * MoodChangeScale;
            return Math.Max(-MoodChangeMax, Math.Min(MoodChangeMax, delta));
        }
    }
}
