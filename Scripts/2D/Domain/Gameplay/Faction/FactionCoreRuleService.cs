namespace LAB2D.Domain.Gameplay.Faction
{
    using System;

    /// <summary>
    /// 阵营核心数值规则（纯函数）— 耐久曲线/升级消耗/复活与解散闸门。
    /// 零 Unity 依赖，可单测；系数区别于山门核心（BuildingDamageRuleService）：
    /// 阵营核心可陷落但只解散阵营，不触发终局（胜负软化，P5）。
    /// </summary>
    public static class FactionCoreRuleService
    {
        /// <summary>1 级基础耐久。</summary>
        public const float CoreMaxHpBase = 400f;

        /// <summary>每级追加耐久。</summary>
        public const float CoreHpPerLevel = 150f;

        /// <summary>被击破后复活的耐久比例。</summary>
        public const float ReviveHpRatio = 0.5f;

        /// <summary>核心最高等级。</summary>
        public const int MaxLevel = 3;

        /// <summary>最多被击破次数（达到即阵营解散）。</summary>
        public const int MaxDownfalls = 2;

        /// <summary>1→2 级升级金库消耗。</summary>
        public const int UpgradeGoldCostLevel1To2 = 300;

        /// <summary>2→3 级升级金库消耗。</summary>
        public const int UpgradeGoldCostLevel2To3 = 800;

        /// <summary>1→2 级升级贡献池消耗。</summary>
        public const int UpgradeContributionCostLevel1To2 = 200;

        /// <summary>2→3 级升级贡献池消耗。</summary>
        public const int UpgradeContributionCostLevel2To3 = 600;

        /// <summary>指定等级的核心最大耐久。</summary>
        public static float GetMaxHp(int level)
        {
            int clamped = Math.Max(1, level);
            return CoreMaxHpBase + (CoreHpPerLevel * (clamped - 1));
        }

        /// <summary>升级金库消耗（从 fromLevel 升级；满级返回 0 表示不可升）。</summary>
        public static int GetUpgradeGoldCost(int fromLevel)
        {
            return fromLevel switch
            {
                1 => UpgradeGoldCostLevel1To2,
                2 => UpgradeGoldCostLevel2To3,
                _ => 0,
            };
        }

        /// <summary>升级贡献池消耗（从 fromLevel 升级；满级返回 0 表示不可升）。</summary>
        public static int GetUpgradeContributionCost(int fromLevel)
        {
            return fromLevel switch
            {
                1 => UpgradeContributionCostLevel1To2,
                2 => UpgradeContributionCostLevel2To3,
                _ => 0,
            };
        }

        /// <summary>被击破后按当前等级比例复活耐久。</summary>
        public static float ComputeReviveHp(int level)
        {
            return GetMaxHp(level) * ReviveHpRatio;
        }

        /// <summary>核心受击结果（照 BuildingDamageRuleService.BuildingDamageResult 形状）。</summary>
        public struct CoreDamageResult
        {
            /// <summary>剩余耐久（不小于 0）。</summary>
            public float RemainingHp;

            /// <summary>是否被击破。</summary>
            public bool IsDestroyed;
        }

        /// <summary>核心受击结算：扣血并判断是否击破。</summary>
        public static CoreDamageResult ApplyDamage(float currentHp, float damage)
        {
            float remaining = Math.Max(0f, currentHp - damage);
            return new CoreDamageResult
            {
                RemainingHp = remaining,
                IsDestroyed = remaining <= 0f,
            };
        }

        /// <summary>被击破次数是否达到解散闸门。</summary>
        public static bool ShouldDissolve(int downfallCount)
        {
            return downfallCount >= MaxDownfalls;
        }
    }
}
