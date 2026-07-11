namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 装备掉落系统公共工具类。
    /// 提供稀有度颜色映射、属性加权生成、装备对比计算、装备文本格式化等纯函数。
    /// 所有方法均为静态，无副作用，不依赖 UnityEditor，可被所有子模块安全调用。
    /// </summary>
    public static class EquipmentLootTool
    {
        private static readonly EquipmentLootRuleService RuleService = new EquipmentLootRuleService();

        /// <summary>
        /// 按稀有度权重随机选择一个稀有度等级（Unity 随机便利方法）。
        /// waveNumber 越大，高稀有度的有效权重越高（低稀有度权重等比缩减）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <returns>随机选中的稀有度等级</returns>
        public static EquipmentRarityType RollRarity(int waveNumber)
        {
            float total = GetRarityTotalWeight(waveNumber);
            float roll = UnityEngine.Random.Range(0f, total);
            return RollRarityWithRoll(waveNumber, roll);
        }

        /// <summary>
        /// 按稀有度权重和给定随机投点值选择一个稀有度等级（纯 C# 方法，可测试）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <param name="randomRoll">随机投点值，范围 [0, GetRarityTotalWeight(waveNumber))。</param>
        /// <returns>选中的稀有度等级</returns>
        public static EquipmentRarityType RollRarityWithRoll(int waveNumber, float randomRoll)
        {
            float bonus = RuleService.GetRarityWeightBonus(
                waveNumber,
                EquipmentLootConstant.RarityWeightBonusPerWave,
                0.5f);

            float commonW = EquipmentLootConstant.CommonWeight * (1f - bonus);
            float uncommonW = EquipmentLootConstant.UncommonWeight;
            float rareW = EquipmentLootConstant.RareWeight * (1f + bonus);
            float epicW = EquipmentLootConstant.EpicWeight * (1f + bonus * 2f);
            float legendaryW = EquipmentLootConstant.LegendaryWeight * (1f + bonus * 3f);
            float mythicW = EquipmentLootConstant.MythicWeight * (1f + bonus * 5f);

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
        /// 获取稀有度随机池的总权重（用于生成随机投点范围）。
        /// </summary>
        /// <param name="waveNumber">当前波次编号（0-based）</param>
        /// <returns>总权重值</returns>
        public static float GetRarityTotalWeight(int waveNumber)
        {
            float bonus = RuleService.GetRarityWeightBonus(
                waveNumber,
                EquipmentLootConstant.RarityWeightBonusPerWave,
                0.5f);

            float commonW = EquipmentLootConstant.CommonWeight * (1f - bonus);
            float uncommonW = EquipmentLootConstant.UncommonWeight;
            float rareW = EquipmentLootConstant.RareWeight * (1f + bonus);
            float epicW = EquipmentLootConstant.EpicWeight * (1f + bonus * 2f);
            float legendaryW = EquipmentLootConstant.LegendaryWeight * (1f + bonus * 3f);
            float mythicW = EquipmentLootConstant.MythicWeight * (1f + bonus * 5f);

            return commonW + uncommonW + rareW + epicW + legendaryW + mythicW;
        }

        /// <summary>
        /// 获取稀有度对应的显示颜色。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>对应的 RGBA 颜色</returns>
        public static Color GetRarityColor(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return EquipmentLootConstant.CommonColor;
                case EquipmentRarityType.Uncommon:  return EquipmentLootConstant.UncommonColor;
                case EquipmentRarityType.Rare:      return EquipmentLootConstant.RareColor;
                case EquipmentRarityType.Epic:      return EquipmentLootConstant.EpicColor;
                case EquipmentRarityType.Legendary: return EquipmentLootConstant.LegendaryColor;
                case EquipmentRarityType.Mythic:    return EquipmentLootConstant.MythicColor;
                default:                            return Color.white;
            }
        }

        /// <summary>
        /// 获取稀有度对应的属性倍率。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>属性倍率</returns>
        public static float GetStatMultiplier(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return EquipmentLootConstant.CommonStatMultiplier;
                case EquipmentRarityType.Uncommon:  return EquipmentLootConstant.UncommonStatMultiplier;
                case EquipmentRarityType.Rare:      return EquipmentLootConstant.RareStatMultiplier;
                case EquipmentRarityType.Epic:      return EquipmentLootConstant.EpicStatMultiplier;
                case EquipmentRarityType.Legendary: return EquipmentLootConstant.LegendaryStatMultiplier;
                case EquipmentRarityType.Mythic:    return EquipmentLootConstant.MythicStatMultiplier;
                default:                            return 1.0f;
            }
        }

        /// <summary>
        /// 获取稀有度的中文显示名称。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>中文名称</returns>
        public static string GetRarityName(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return "普通";
                case EquipmentRarityType.Uncommon:  return "不凡";
                case EquipmentRarityType.Rare:      return "稀有";
                case EquipmentRarityType.Epic:      return "史诗";
                case EquipmentRarityType.Legendary: return "传说";
                case EquipmentRarityType.Mythic:    return "神话";
                default:                            return "未知";
            }
        }

        /// <summary>
        /// 根据稀有度为已有装备属性施加倍率。
        /// 对装备的 8 条属性（ATN/INT/DEF/RES/CRT/CSD/SPD/HIT）逐一乘稀有度倍率。
        /// Legendary+ 装备随机选择 1-2 条属性额外翻倍（极值属性）。
        /// </summary>
        /// <param name="attr">装备属性对象（将被直接修改）</param>
        /// <param name="rarity">稀有度等级</param>
        public static void ApplyRarityToAttributes(Character.Attribute attr, EquipmentRarityType rarity)
        {
            if (attr == null) return;

            float mult = GetStatMultiplier(rarity);
            attr.ATN *= mult;
            attr.INT *= mult;
            attr.DEF *= mult;
            attr.RES *= mult;
            attr.CRT *= mult;
            attr.CSD *= mult;
            attr.SPD *= mult;
            attr.HIT *= mult;

            // 传说级和神话级装备有额外极值属性
            int extremeCount = 0;
            if (rarity == EquipmentRarityType.Legendary) extremeCount = EquipmentLootConstant.LegendaryExtremeStatCount;
            if (rarity == EquipmentRarityType.Mythic) extremeCount = EquipmentLootConstant.MythicExtremeStatCount;

            if (extremeCount > 0)
            {
                // 随机选择 extremeCount 条属性翻倍
                List<int> indices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
                for (int i = 0; i < extremeCount; i++)
                {
                    int idx = UnityEngine.Random.Range(0, indices.Count);
                    int chosen = indices[idx];
                    indices.RemoveAt(idx);

                    switch (chosen)
                    {
                        case 0: attr.ATN *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 1: attr.INT *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 2: attr.DEF *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 3: attr.RES *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 4: attr.CRT *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 5: attr.CSD *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 6: attr.SPD *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                        case 7: attr.HIT *= EquipmentLootConstant.ExtremeStatMultiplier; break;
                    }
                }
            }
        }

        /// <summary>
        /// 比较新旧装备的8条属性，返回带标记的对比行列表。
        /// 正差值标记 ↑（绿色），负差值标记 ↓（红色），零差值标记 =（白色）。
        /// </summary>
        /// <param name="oldAttr">当前已装备的属性（可为 null 表示空槽）</param>
        /// <param name="newAttr">新装备的属性</param>
        /// <returns>格式化后的对比文本行列表</returns>
        public static List<string> BuildCompareLines(Character.Attribute oldAttr, Character.Attribute newAttr)
        {
            List<string> lines = new List<string>();
            if (newAttr == null) return lines;

            string[] names = { "物理攻击", "魔法攻击", "物理防御", "魔法防御", "暴击率", "暴击伤害", "速度/回避", "命中/连击" };
            float[] oldVals = oldAttr != null
                ? new float[] { oldAttr.ATN, oldAttr.INT, oldAttr.DEF, oldAttr.RES, oldAttr.CRT, oldAttr.CSD, oldAttr.SPD, oldAttr.HIT }
                : new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            float[] newVals = { newAttr.ATN, newAttr.INT, newAttr.DEF, newAttr.RES, newAttr.CRT, newAttr.CSD, newAttr.SPD, newAttr.HIT };

            for (int i = 0; i < names.Length; i++)
            {
                float diff = newVals[i] - oldVals[i];
                string prefix;
                if (diff > 0.001f) prefix = EquipmentLootConstant.StatUpPrefix;
                else if (diff < -0.001f) prefix = EquipmentLootConstant.StatDownPrefix;
                else prefix = EquipmentLootConstant.StatEqualPrefix;

                lines.Add(string.Format("{0}{1}: {2:F1}", prefix, names[i], newVals[i]));
            }

            return lines;
        }

        /// <summary>
        /// 获取对比行中"提升"的行数（用于判断装备是否更好）。
        /// </summary>
        /// <param name="oldAttr">当前装备属性</param>
        /// <param name="newAttr">新装备属性</param>
        /// <returns>提升的条目数</returns>
        public static int CountUpgrades(Character.Attribute oldAttr, Character.Attribute newAttr)
        {
            if (oldAttr == null) return 8; // 空槽装备上新装备，全算提升
            if (newAttr == null) return 0;

            int count = 0;
            if (newAttr.ATN > oldAttr.ATN) count++;
            if (newAttr.INT > oldAttr.INT) count++;
            if (newAttr.DEF > oldAttr.DEF) count++;
            if (newAttr.RES > oldAttr.RES) count++;
            if (newAttr.CRT > oldAttr.CRT) count++;
            if (newAttr.CSD > oldAttr.CSD) count++;
            if (newAttr.SPD > oldAttr.SPD) count++;
            if (newAttr.HIT > oldAttr.HIT) count++;
            return count;
        }

        /// <summary>
        /// 格式化装备属性为单行摘要文本（用于面板槽位显示）。
        /// </summary>
        /// <param name="attr">装备属性</param>
        /// <returns>属性摘要字符串</returns>
        public static string FormatAttributeSummary(Character.Attribute attr)
        {
            if (attr == null) return EquipmentLootConstant.EmptySlotText;
            return string.Format(
                "ATN:{0:F0} INT:{1:F0} DEF:{2:F0} RES:{3:F0} CRT:{4:F2} CSD:{5:F1} SPD:{6:F0} HIT:{7:F0}",
                attr.ATN, attr.INT, attr.DEF, attr.RES, attr.CRT, attr.CSD, attr.SPD, attr.HIT);
        }

        /// <summary>
        /// 格式化稀有度标签文本（如 "[传说]"）。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>格式化标签</returns>
        public static string FormatRarityLabel(EquipmentRarityType rarity)
        {
            return string.Format(EquipmentLootConstant.RarityLabelFormat, GetRarityName(rarity));
        }

        /// <summary>
        /// 根据装备类型获取中文槽位名称。
        /// </summary>
        /// <param name="type">装备槽位类型</param>
        /// <returns>中文名称</returns>
        public static string GetSlotName(AEquipment.EquipTypeEnum type)
        {
            switch (type)
            {
                case AEquipment.EquipTypeEnum.Head:     return "头部";
                case AEquipment.EquipTypeEnum.Body:     return "上衣";
                case AEquipment.EquipTypeEnum.Trouser:  return "裤子";
                case AEquipment.EquipTypeEnum.Shoes:    return "鞋子";
                case AEquipment.EquipTypeEnum.Weapon:   return "武器";
                case AEquipment.EquipTypeEnum.Shield:   return "盾牌";
                case AEquipment.EquipTypeEnum.Ring:     return "戒指";
                case AEquipment.EquipTypeEnum.Necklace: return "项链";
                case AEquipment.EquipTypeEnum.Bracelet: return "手镯";
                case AEquipment.EquipTypeEnum.Belt:     return "腰带";
                case AEquipment.EquipTypeEnum.Earring:  return "耳环";
                case AEquipment.EquipTypeEnum.Wing:     return "翅膀";
                case AEquipment.EquipTypeEnum.Mount:    return "坐骑";
                case AEquipment.EquipTypeEnum.Pet:      return "宠物";
                default:                                return "未知";
            }
        }

        /// <summary>
        /// 判断装备稀有度是否发光（Epic 及以上发光）。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>是否发光</returns>
        public static bool HasGlowEffect(EquipmentRarityType rarity)
        {
            return rarity >= EquipmentRarityType.Epic;
        }

        /// <summary>
        /// 将稀有度映射到已有的品质枚举（BackpackItemQualityEnum）。
        /// 用于把稀有度信息写入装备对象，供装备面板等 UI 展示。
        /// </summary>
        /// <param name="rarity">稀有度等级</param>
        /// <returns>对应的品质枚举值</returns>
        public static ABackpackItem.BackpackItemQualityEnum MapRarityToQuality(EquipmentRarityType rarity)
        {
            switch (rarity)
            {
                case EquipmentRarityType.Common:    return ABackpackItem.BackpackItemQualityEnum.Gray;
                case EquipmentRarityType.Uncommon:  return ABackpackItem.BackpackItemQualityEnum.Green;
                case EquipmentRarityType.Rare:      return ABackpackItem.BackpackItemQualityEnum.Blue;
                case EquipmentRarityType.Epic:      return ABackpackItem.BackpackItemQualityEnum.Purple;
                case EquipmentRarityType.Legendary: return ABackpackItem.BackpackItemQualityEnum.Orange;
                case EquipmentRarityType.Mythic:    return ABackpackItem.BackpackItemQualityEnum.Red;
                default:                            return ABackpackItem.BackpackItemQualityEnum.Gray;
            }
        }

        /// <summary>
        /// 根据品质枚举获取对应的属性倍率。
        /// 用于 Info 面板展示品质对装备数值的影响。
        /// </summary>
        /// <param name="quality">品质枚举值</param>
        /// <returns>属性倍率</returns>
        public static float GetQualityStatMultiplier(ABackpackItem.BackpackItemQualityEnum quality)
        {
            switch (quality)
            {
                case ABackpackItem.BackpackItemQualityEnum.Gray:   return EquipmentLootConstant.CommonStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.White:  return 1.0f;
                case ABackpackItem.BackpackItemQualityEnum.Green:  return EquipmentLootConstant.UncommonStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.Blue:   return EquipmentLootConstant.RareStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.Purple: return EquipmentLootConstant.EpicStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.Orange: return EquipmentLootConstant.LegendaryStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.Yellow: return 1.0f;
                case ABackpackItem.BackpackItemQualityEnum.Red:    return EquipmentLootConstant.MythicStatMultiplier;
                case ABackpackItem.BackpackItemQualityEnum.Black:  return 1.0f;
                default:                                           return 1.0f;
            }
        }

        /// <summary>
        /// 根据品质枚举获取对应的显示颜色。
        /// 用于装备面板中按品质着色槽位文字。
        /// </summary>
        /// <param name="quality">品质枚举值</param>
        /// <returns>对应的 RGBA 颜色</returns>
        public static Color GetQualityColor(ABackpackItem.BackpackItemQualityEnum quality)
        {
            switch (quality)
            {
                case ABackpackItem.BackpackItemQualityEnum.Gray:   return EquipmentLootConstant.CommonColor;
                case ABackpackItem.BackpackItemQualityEnum.White:  return Color.white;
                case ABackpackItem.BackpackItemQualityEnum.Green:  return EquipmentLootConstant.UncommonColor;
                case ABackpackItem.BackpackItemQualityEnum.Blue:   return EquipmentLootConstant.RareColor;
                case ABackpackItem.BackpackItemQualityEnum.Purple: return EquipmentLootConstant.EpicColor;
                case ABackpackItem.BackpackItemQualityEnum.Orange: return EquipmentLootConstant.LegendaryColor;
                case ABackpackItem.BackpackItemQualityEnum.Red:    return EquipmentLootConstant.MythicColor;
                default:                                           return Color.gray;
            }
        }
    }
}
