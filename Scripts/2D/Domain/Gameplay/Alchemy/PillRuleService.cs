namespace LAB2D.Domain.Gameplay.Alchemy
{
    using System;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;

    /// <summary>
    /// 炼丹规则 — 纯函数：可炼判定、品质 roll、按品质结算效果倍率。
    /// 随机源注入可测（默认纯 C# System.Random，构造期禁 icall 的项目约定）；
    /// GrowthData 调用方先 Ensure（集合字段反序列化契约）。
    /// </summary>
    public static class PillRuleService
    {
        /// <summary>上品阈值：roll ∈ [0.6, 0.9) 为上品。</summary>
        public const float SuperiorThreshold = 0.6f;

        /// <summary>极品阈值：roll ∈ [0.9, 1) 为极品。</summary>
        public const float PremiumThreshold = 0.9f;

        /// <summary>随机源（[0,1) 均匀；测试用 UseSequence 桩注入，TearDown 恢复）。</summary>
        public static Func<float> RandomFloatProvider { get; set; } = NextUniformUnitFloat;

        /// <summary>纯 C# 随机源（构造期/icall 禁令——不触碰 UnityEngine.Random）。</summary>
        private static readonly System.Random Rng = new System.Random();

        private static float NextUniformUnitFloat()
        {
            return (float)Rng.NextDouble();
        }

        /// <summary>
        /// 是否可炼：境界达门槛且灵气够成本（null 任一为 false）。
        /// </summary>
        public static bool CanCraft(GrowthData growth, PillDef pill)
        {
            if (growth == null || pill == null)
            {
                return false;
            }

            return growth.RealmIndex >= pill.RequiredRealmIndex && growth.Qi >= pill.QiCost;
        }

        /// <summary>
        /// 炼丹结算：扣灵气、roll 品质、效果基准 × 品质倍率。
        /// 失败（不可炼）不改 growth 且 result 为默认值。
        /// </summary>
        /// <returns>是否炼成。</returns>
        public static bool TryCraft(GrowthData growth, PillDef pill, out PillCraftResult result)
        {
            result = default;

            if (!CanCraft(growth, pill))
            {
                return false;
            }

            PillQuality quality = RollQuality(RandomFloatProvider());
            growth.Qi -= pill.QiCost;

            float multiplier = QualityToMultiplier(quality);
            result = new PillCraftResult
            {
                Success = true,
                Pill = pill,
                Quality = quality,
                EffectValue = pill.EffectValue * multiplier,
                PermanentBonus = MultiplyStats(pill.PermanentBonus, multiplier),
            };
            return true;
        }

        /// <summary>
        /// roll 值 → 品质档（[0,0.6) 凡 / [0.6,0.9) 上 / [0.9,1) 极）。
        /// </summary>
        public static PillQuality RollQuality(float roll)
        {
            if (roll < SuperiorThreshold)
            {
                return PillQuality.Common;
            }

            if (roll < PremiumThreshold)
            {
                return PillQuality.Superior;
            }

            return PillQuality.Premium;
        }

        /// <summary>品质 → 效果倍率（凡 ×1.0 / 上 ×1.5 / 极 ×2.0）。</summary>
        public static float QualityToMultiplier(PillQuality quality)
        {
            switch (quality)
            {
                case PillQuality.Superior:
                    return 1.5f;
                case PillQuality.Premium:
                    return 2f;
                default:
                    return 1f;
            }
        }

        private static BattleStats MultiplyStats(BattleStats stats, float multiplier)
        {
            return new BattleStats(
                stats.ATN * multiplier,
                stats.INT * multiplier,
                stats.DEF * multiplier,
                stats.RES * multiplier,
                stats.CRT * multiplier,
                stats.CSD * multiplier,
                stats.SPD * multiplier,
                stats.HIT * multiplier);
        }
    }
}
