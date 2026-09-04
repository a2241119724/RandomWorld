namespace LAB2D.Domain.Gameplay.AncientCave
{
    /// <summary>
    /// 洞府探索结算规则（M4 包 4 地图兴趣点轮 3）— 风险/奖励 roll 纯函数。
    /// 运行时宿主 AncientCaveManager 结算时调用；本类零 Unity 依赖、
    /// 随机数由调用方注入（roll ∈ [0,1)），可独立测试。
    /// </summary>
    public static class CaveExploreRuleService
    {
        /// <summary>风险权重：惊动妖兽 35% / 塌方受伤 10% / 平安 55%。</summary>
        public const float DangerWeight = 0.35f;
        public const float CollapseWeight = 0.10f;

        /// <summary>奖励权重：功法 40% / 物资 25% / 装备 25% / 双倍 10%。</summary>
        public const float GongFaWeight = 0.40f;
        public const float SupplyWeight = 0.25f;
        public const float EquipmentWeight = 0.25f;

        /// <summary>塌方伤害占最大生命比例。</summary>
        public const float CollapseDamageRatio = 0.15f;

        /// <summary>玩家亲自探索读条时长（秒）。</summary>
        public const float PlayerExploreSeconds = 30f;

        /// <summary>Worker 探索驻留时长（秒，maxProgress 基准，实际吃全乘数链）。</summary>
        public const float WorkerExploreSeconds = 60f;

        /// <summary>风险结算结果。</summary>
        public enum RiskOutcome
        {
            /// <summary>平安无事。</summary>
            Safe = 0,

            /// <summary>惊动妖兽：洞府旁生成 2~3 只（白天遇敌，探索者走既有接敌反应）。</summary>
            Danger = 1,

            /// <summary>塌方受伤：探索者扣 15% 最大生命。</summary>
            Collapse = 2,
        }

        /// <summary>奖励结算结果。</summary>
        public enum RewardKind
        {
            /// <summary>无功而返（双倍结算的功法部分失败时也可能落到此值）。</summary>
            None = 0,

            /// <summary>功法秘籍：随机未学功法直接习得（授予玩家）。</summary>
            GongFa = 1,

            /// <summary>物资堆：既有掉落管线在洞府格放材料（ForceDrop 必掉）。</summary>
            Supply = 2,

            /// <summary>遗物装备：装备掉落管线（稀有度按第 5 波档）。</summary>
            Equipment = 3,

            /// <summary>双倍：功法 + 物资各一。</summary>
            Double = 4,
        }

        /// <summary>
        /// 风险 roll（roll ∈ [0,1)）：&lt;0.35 妖兽；&lt;0.45 塌方；其余平安。
        /// </summary>
        public static RiskOutcome RollRisk(float roll)
        {
            if (roll < DangerWeight)
            {
                return RiskOutcome.Danger;
            }

            if (roll < DangerWeight + CollapseWeight)
            {
                return RiskOutcome.Collapse;
            }

            return RiskOutcome.Safe;
        }

        /// <summary>
        /// 奖励 roll（roll ∈ [0,1)）：&lt;0.40 功法；&lt;0.65 物资；&lt;0.90 装备；其余双倍。
        /// </summary>
        public static RewardKind RollReward(float roll)
        {
            if (roll < GongFaWeight)
            {
                return RewardKind.GongFa;
            }

            if (roll < GongFaWeight + SupplyWeight)
            {
                return RewardKind.Supply;
            }

            if (roll < GongFaWeight + SupplyWeight + EquipmentWeight)
            {
                return RewardKind.Equipment;
            }

            return RewardKind.Double;
        }

        /// <summary>
        /// 妖兽数量 roll：&lt;0.5 生成 2 只，否则 3 只。
        /// </summary>
        public static int RollEnemyCount(float roll)
        {
            return roll < 0.5f ? 2 : 3;
        }
    }
}
