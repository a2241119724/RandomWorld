namespace LAB2D.Domain.Gameplay.Faction
{
    using System;

    /// <summary>
    /// 阵营战争规则（纯函数）— 宣战闸门/攻伐窗口/掠夺结算/自动和谈。
    /// 三机制并存：关系值驱动（必要条件）+ 领袖性格驱动（好战度放宽阈值）+
    /// 长老支持率闸门（≥60%）；战争态每 3 天白天一个攻伐窗口、单窗口出征 ≤4 人，
    /// 入夜强制休战（FactionManager 相位事件召回 Raid 任务）。
    /// 零 Unity 依赖，可单测。
    /// </summary>
    public static class FactionWarRuleService
    {
        /// <summary>宣战关系必要条件线（实际阈值随领袖好战度放宽）。</summary>
        public const float WarThreshold = -40f;

        /// <summary>好战度对阈值的最大放宽量（好战度 100 时阈值 -20）。</summary>
        public const float BellicosityThresholdScale = 20f;

        /// <summary>宣战所需长老支持率下限。</summary>
        public const float MinCouncilSupport = 0.6f;

        /// <summary>好战度野心权重。</summary>
        public const float BellicosityAmbitionWeight = 0.6f;

        /// <summary>好战度贪婪权重。</summary>
        public const float BellicosityGreedWeight = 0.4f;

        /// <summary>和谈后关系落点（冷静期，高于宣战线避免秒复战）。</summary>
        public const float PeaceSettleRelation = -10f;

        /// <summary>战争疲劳自动和谈线（关系回升到该线即停战）。</summary>
        public const float AutoPeaceRelation = -20f;

        /// <summary>战争态关系每日回升量（打不动了自然缓和）。</summary>
        public const float WarDailyRelationRecovery = 1.5f;

        /// <summary>攻伐窗口间隔（游戏日）。</summary>
        public const int RaidWindowIntervalDays = 3;

        /// <summary>单窗口单侧最大出征人数。</summary>
        public const int MaxRaidersPerWindow = 4;

        /// <summary>金库掠夺比例。</summary>
        public const float TreasuryLootRatio = 0.2f;

        /// <summary>出征参战战功（贡献点，掠夺达成时发放）。</summary>
        public const float RaidContribution = 10f;

        /// <summary>掠夺成功对关系的额外恶化。</summary>
        public const float RaidLootRelationDelta = -8f;

        /// <summary>领袖好战度（0-100）：野心与贪婪加权。</summary>
        public static float ComputeBellicosity(float ambition, float greed)
        {
            float raw = (ambition * BellicosityAmbitionWeight) + (greed * BellicosityGreedWeight);
            return Math.Max(0f, Math.Min(100f, raw));
        }

        /// <summary>实际宣战阈值：好战领袖放宽（-40 → 最高 -20）。</summary>
        public static float GetEffectiveWarThreshold(float bellicosity)
        {
            return WarThreshold + (Math.Max(0f, Math.Min(100f, bellicosity)) / 100f * BellicosityThresholdScale);
        }

        /// <summary>长老支持率（支持票/总票；总票 0 视为无人反对=1）。</summary>
        public static float ComputeCouncilSupportRatio(int supporters, int total)
        {
            if (total <= 0)
            {
                return 1f;
            }

            return Math.Max(0f, Math.Min(1f, supporters / (float)total));
        }

        /// <summary>宣战闸门：关系低于实际阈值 且 长老支持率达标。</summary>
        public static bool CanDeclareWar(float relation, float bellicosity, float councilSupport)
        {
            return relation < GetEffectiveWarThreshold(bellicosity)
                && councilSupport >= MinCouncilSupport;
        }

        /// <summary>是否攻伐窗口日（每 RaidWindowIntervalDays 天；第 0 日不开）。</summary>
        public static bool IsRaidWindowDay(int dayIndex)
        {
            return dayIndex > 0 && dayIndex % RaidWindowIntervalDays == 0;
        }

        /// <summary>单窗口出征人数（闲散人数不足时取较小值）。</summary>
        public static int ComputeRaiderCount(int availableCount)
        {
            return Math.Max(0, Math.Min(MaxRaidersPerWindow, availableCount));
        }

        /// <summary>掠夺金额（目标金库的 TreasuryLootRatio，向上取整）。</summary>
        public static int ComputeLootGold(int targetTreasuryGold)
        {
            if (targetTreasuryGold <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling(targetTreasuryGold * TreasuryLootRatio);
        }

        /// <summary>战争疲劳自动和谈判定。</summary>
        public static bool ShouldAutoPeace(float relation)
        {
            return relation >= AutoPeaceRelation;
        }
    }
}
