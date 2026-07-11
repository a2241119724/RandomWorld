namespace LAB2D.Domain.Gameplay
{
    using LAB2D.Enum;
    using System.Collections.Generic;

    /// <summary>
    /// 装备掉落稀有度缩放与属性计算的纯算术规则。
    /// 所有方法均不依赖 UnityEngine.Random，可独立测试。
    /// </summary>
    public sealed class EquipmentLootRuleService
    {
        // ============================================================
        // 稀有度掉落权重（纯规则层常量副本，来源：EquipmentLootConstant）
        // ============================================================

        private const float CommonWeight = 50f;
        private const float UncommonWeight = 25f;
        private const float RareWeight = 15f;
        private const float EpicWeight = 7f;
        private const float LegendaryWeight = 2.5f;
        private const float MythicWeight = 0.5f;

        // ============================================================
        // 稀有度属性倍率（纯规则层常量副本，来源：EquipmentLootConstant）
        // ============================================================

        private const float CommonStatMultiplier = 1.0f;
        private const float UncommonStatMultiplier = 1.3f;
        private const float RareStatMultiplier = 1.6f;
        private const float EpicStatMultiplier = 2.0f;
        private const float LegendaryStatMultiplier = 2.5f;
        private const float MythicStatMultiplier = 3.2f;

        // ============================================================
        // 极值属性规则
        // ============================================================

        private const int LegendaryExtremeStatCount = 1;
        private const int MythicExtremeStatCount = 2;
        private const float ExtremeStatMultiplier = 2.0f;

        // ============================================================
        // 装备属性名数组（8条属性，顺序固定）
        // ============================================================

        private static readonly string[] StatNames = { "ATN", "INT", "DEF", "RES", "CRT", "CSD", "SPD", "HIT" };

        /// <summary>
        /// 获取稀有度权重加成系数（波次越高，高稀有度出现概率越大）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）。</param>
        /// <param name="bonusPerWave">每波加成量。</param>
        /// <param name="maxBonus">加成上限。</param>
        /// <returns>加成系数，范围 [0, maxBonus]。</returns>
        public float GetRarityWeightBonus(int waveNumber, float bonusPerWave, float maxBonus)
        {
            float bonus = waveNumber * bonusPerWave;
            if (bonus < 0.0f)
            {
                return 0.0f;
            }

            return bonus > maxBonus ? maxBonus : bonus;
        }

        /// <summary>
        /// 获取稀有度随机池的总权重（用于生成随机投点范围）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）。</param>
        /// <param name="bonusPerWave">每波加成量。</param>
        /// <param name="maxBonus">加成上限。</param>
        /// <returns>总权重值。</returns>
        public float GetRarityTotalWeight(int waveNumber, float bonusPerWave = 0.03f, float maxBonus = 0.5f)
        {
            float bonus = GetRarityWeightBonus(waveNumber, bonusPerWave, maxBonus);

            float commonW = CommonWeight * (1f - bonus);
            float uncommonW = UncommonWeight;
            float rareW = RareWeight * (1f + bonus);
            float epicW = EpicWeight * (1f + bonus * 2f);
            float legendaryW = LegendaryWeight * (1f + bonus * 3f);
            float mythicW = MythicWeight * (1f + bonus * 5f);

            return commonW + uncommonW + rareW + epicW + legendaryW + mythicW;
        }

        /// <summary>
        /// 按稀有度权重和给定随机投点值选择一个稀有度等级（纯 C# 方法，可测试）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）。</param>
        /// <param name="randomRoll">随机投点值，范围 [0, GetRarityTotalWeight(...))。</param>
        /// <param name="bonusPerWave">每波加成量。</param>
        /// <param name="maxBonus">加成上限。</param>
        /// <returns>选中的稀有度等级。</returns>
        public EquipmentRarityType RollRarityWithRoll(
            int waveNumber,
            float randomRoll,
            float bonusPerWave = 0.03f,
            float maxBonus = 0.5f)
        {
            float bonus = GetRarityWeightBonus(waveNumber, bonusPerWave, maxBonus);

            float commonW = CommonWeight * (1f - bonus);
            float uncommonW = UncommonWeight;
            float rareW = RareWeight * (1f + bonus);
            float epicW = EpicWeight * (1f + bonus * 2f);
            float legendaryW = LegendaryWeight * (1f + bonus * 3f);
            float mythicW = MythicWeight * (1f + bonus * 5f);

            float cursor = 0f;
            cursor += commonW;
            if (randomRoll <= cursor) return EquipmentRarityType.Common;
            cursor += uncommonW;
            if (randomRoll <= cursor) return EquipmentRarityType.Uncommon;
            cursor += rareW;
            if (randomRoll <= cursor) return EquipmentRarityType.Rare;
            cursor += epicW;
            if (randomRoll <= cursor) return EquipmentRarityType.Epic;
            cursor += legendaryW;
            if (randomRoll <= cursor) return EquipmentRarityType.Legendary;
            return EquipmentRarityType.Mythic;
        }

        /// <summary>
        /// 获取稀有度对应的属性倍率。
        /// </summary>
        /// <param name="rarity">稀有度等级。</param>
        /// <returns>属性倍率。</returns>
        public float GetStatMultiplier(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return CommonStatMultiplier;
                case EquipmentRarityType.Uncommon:  return UncommonStatMultiplier;
                case EquipmentRarityType.Rare:      return RareStatMultiplier;
                case EquipmentRarityType.Epic:      return EpicStatMultiplier;
                case EquipmentRarityType.Legendary: return LegendaryStatMultiplier;
                case EquipmentRarityType.Mythic:    return MythicStatMultiplier;
                default:                            return 1.0f;
            }
        }

        /// <summary>
        /// 对单条属性施加稀有度基础倍率。
        /// 不包含极值属性随机逻辑（极值属性依赖 UnityEngine.Random，由 Tool 层处理）。
        /// </summary>
        /// <param name="statValue">原始属性值。</param>
        /// <param name="rarity">稀有度等级。</param>
        /// <returns>倍率缩放后的属性值。</returns>
        public float ApplyRarityToStat(float statValue, EquipmentRarityType rarity)
        {
            return statValue * GetStatMultiplier(rarity);
        }

        /// <summary>
        /// 获取稀有度对应的极值属性条数。
        /// </summary>
        /// <param name="rarity">稀有度等级。</param>
        /// <returns>极值属性条数（Legendary=1, Mythic=2, 其他=0）。</returns>
        public int GetExtremeStatCount(EquipmentRarityType rarity)
        {
            if (rarity == EquipmentRarityType.Legendary) return LegendaryExtremeStatCount;
            if (rarity == EquipmentRarityType.Mythic) return MythicExtremeStatCount;
            return 0;
        }

        /// <summary>
        /// 获取极值属性倍率。
        /// </summary>
        /// <returns>极值属性倍率。</returns>
        public float GetExtremeStatMultiplier()
        {
            return ExtremeStatMultiplier;
        }

        /// <summary>
        /// 统计升级条目数（新属性高于旧属性的条目数）。
        /// </summary>
        /// <param name="before">旧装备属性字典（statName -> value），可为 null 表示空槽。</param>
        /// <param name="after">新装备属性字典（statName -> value）。</param>
        /// <returns>提升的条目数；旧装备为 null 时返回新装备的全部条目数。</returns>
        public int CountUpgrades(Dictionary<string, float> before, Dictionary<string, float> after)
        {
            if (after == null) return 0;
            if (before == null) return after.Count;

            int count = 0;
            foreach (KeyValuePair<string, float> kvp in after)
            {
                float oldVal;
                before.TryGetValue(kvp.Key, out oldVal);
                if (kvp.Value > oldVal)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 计算新旧装备各属性的差值（new - old）。
        /// 用于 BuildCompareLines 的规则计算部分。
        /// </summary>
        /// <param name="oldValues">旧装备属性值数组（可为 null 表示空槽）。</param>
        /// <param name="newValues">新装备属性值数组，顺序与 StatNames 对应。</param>
        /// <returns>各属性差值数组。</returns>
        public float[] GetStatDiffs(float[] oldValues, float[] newValues)
        {
            if (newValues == null) return new float[0];

            int len = newValues.Length;
            float[] diffs = new float[len];
            for (int i = 0; i < len; i++)
            {
                float oldVal = (oldValues != null && i < oldValues.Length) ? oldValues[i] : 0f;
                diffs[i] = newValues[i] - oldVal;
            }

            return diffs;
        }

        /// <summary>
        /// 获取装备属性名列表（8条属性，顺序固定）。
        /// </summary>
        /// <returns>属性名数组：ATN, INT, DEF, RES, CRT, CSD, SPD, HIT。</returns>
        public string[] GetStatNames()
        {
            return StatNames;
        }
    }
}
