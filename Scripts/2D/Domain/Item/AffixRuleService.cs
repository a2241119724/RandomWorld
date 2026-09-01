namespace LAB2D.Domain.Item
{
    using LAB2D.Constant;
    using LAB2D.Enum;
    using LAB2D.Domain.Gameplay;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 装备词条滚动规则 — 按稀有度决定词条条数、无重复抽取类型、数值乘稀有度倍率。
    /// 随机依赖通过 <see cref="RandomFloatProvider"/> 注入（Gameplay 层封装 UnityEngine.Random），
    /// 本类不依赖 UnityEngine，可独立测试。
    /// </summary>
    public sealed class AffixRuleService
    {
        private static readonly EquipmentLootRuleService LootRuleService = new EquipmentLootRuleService();

        /// <summary>
        /// 随机浮点数提供者 (minInclusive, maxInclusive) → [min, max]。
        /// 必须在使用 Roll 前由 Gameplay 层注入；未注入时 Roll 返回空列表。
        /// </summary>
        public static Func<float, float, float> RandomFloatProvider { get; set; }

        /// <summary>
        /// 按稀有度滚动一组词条（类型不重复）。
        /// </summary>
        /// <param name="rarity">装备稀有度。</param>
        /// <returns>词条列表；随机提供者未注入时返回空列表。</returns>
        public List<EquipmentAffix> Roll(EquipmentRarityType rarity)
        {
            if (RandomFloatProvider == null)
            {
                return new List<EquipmentAffix>();
            }

            int count = this.RollAffixCount(rarity);
            List<EquipmentAffixType> pool = this.BuildTypePool();
            List<EquipmentAffix> affixes = new List<EquipmentAffix>(count);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = (int)RandomFloatProvider(0f, pool.Count - 1);
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= pool.Count)
                {
                    index = pool.Count - 1;
                }

                EquipmentAffixType type = pool[index];
                pool.RemoveAt(index);
                affixes.Add(new EquipmentAffix(type, this.RollValue(type, rarity)));
            }

            return affixes;
        }

        /// <summary>
        /// 按稀有度滚动词条条数（区间来自 EquipmentAffixConstant）。
        /// </summary>
        public int RollAffixCount(EquipmentRarityType rarity)
        {
            int min;
            int max;
            switch (rarity)
            {
                case EquipmentRarityType.Common:
                    min = EquipmentAffixConstant.CommonCountMin;
                    max = EquipmentAffixConstant.CommonCountMax;
                    break;
                case EquipmentRarityType.Uncommon:
                    min = EquipmentAffixConstant.UncommonCountMin;
                    max = EquipmentAffixConstant.UncommonCountMax;
                    break;
                case EquipmentRarityType.Rare:
                    min = EquipmentAffixConstant.RareCountMin;
                    max = EquipmentAffixConstant.RareCountMax;
                    break;
                case EquipmentRarityType.Epic:
                    min = EquipmentAffixConstant.EpicCountMin;
                    max = EquipmentAffixConstant.EpicCountMax;
                    break;
                case EquipmentRarityType.Legendary:
                    min = EquipmentAffixConstant.LegendaryCountMin;
                    max = EquipmentAffixConstant.LegendaryCountMax;
                    break;
                case EquipmentRarityType.Mythic:
                    min = EquipmentAffixConstant.MythicCountMin;
                    max = EquipmentAffixConstant.MythicCountMax;
                    break;
                default:
                    return 0;
            }

            if (max <= min)
            {
                return min;
            }

            return (int)RandomFloatProvider(min, max);
        }

        /// <summary>
        /// 滚动单条词条数值：区间内随机 × 稀有度属性倍率。
        /// </summary>
        public float RollValue(EquipmentAffixType type, EquipmentRarityType rarity)
        {
            float min = GetMinValue(type);
            float max = GetMaxValue(type);
            float value = RandomFloatProvider(min, max);
            return value * LootRuleService.GetStatMultiplier(rarity);
        }

        /// <summary>词条类型数值下限。</summary>
        public static float GetMinValue(EquipmentAffixType type)
        {
            switch (type)
            {
                case EquipmentAffixType.FlatAtn:   return EquipmentAffixConstant.FlatAtnMin;
                case EquipmentAffixType.FlatInt:   return EquipmentAffixConstant.FlatIntMin;
                case EquipmentAffixType.MaxHp:     return EquipmentAffixConstant.MaxHpMin;
                case EquipmentAffixType.Lifesteal: return EquipmentAffixConstant.LifestealMin;
                case EquipmentAffixType.Reflect:   return EquipmentAffixConstant.ReflectMin;
                default:                           return 0f;
            }
        }

        /// <summary>词条类型数值上限。</summary>
        public static float GetMaxValue(EquipmentAffixType type)
        {
            switch (type)
            {
                case EquipmentAffixType.FlatAtn:   return EquipmentAffixConstant.FlatAtnMax;
                case EquipmentAffixType.FlatInt:   return EquipmentAffixConstant.FlatIntMax;
                case EquipmentAffixType.MaxHp:     return EquipmentAffixConstant.MaxHpMax;
                case EquipmentAffixType.Lifesteal: return EquipmentAffixConstant.LifestealMax;
                case EquipmentAffixType.Reflect:   return EquipmentAffixConstant.ReflectMax;
                default:                           return 0f;
            }
        }

        /// <summary>全类型池（Roll 内做无重复抽取）。</summary>
        private List<EquipmentAffixType> BuildTypePool()
        {
            return new List<EquipmentAffixType>
            {
                EquipmentAffixType.FlatAtn,
                EquipmentAffixType.FlatInt,
                EquipmentAffixType.MaxHp,
                EquipmentAffixType.Lifesteal,
                EquipmentAffixType.Reflect,
            };
        }
    }
}
