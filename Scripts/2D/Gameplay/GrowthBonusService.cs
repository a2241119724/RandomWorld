namespace LAB2D.Gameplay
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Item;
    using LAB2D.Item.Backpack.Equipment;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 成长加成收集服务 — 装备词条 → GrowthBonus 投影，并接管
    /// <see cref="GameCharacter.CharacterData.GrowthCollectProvider"/>（属性重算时的成长源收集入口）。
    /// 数值词条（FlatAtn/FlatInt/MaxHp）进统一属性管线；
    /// 特殊词条（Lifesteal/Reflect）进 GrowthBonus.Special，
    /// 由 CharacterHealthComponent 在战斗事件点（命中/受击）消费。
    /// </summary>
    public static class GrowthBonusService
    {
        /// <summary>
        /// 启动接线：注入词条/灵根随机提供者（Domain 层不依赖 UnityEngine）+ 接管成长收集。
        /// 在 GlobalInit.RegisterSafeServices 调用一次。
        /// </summary>
        public static void Install()
        {
            AffixRuleService.RandomFloatProvider = (min, max) => UnityEngine.Random.Range(min, max);
            LingGenRuleService.RandomFloatProvider = (min, max) => UnityEngine.Random.Range(min, max);
            AwakenedPowerRuleService.RandomFloatProvider = (min, max) => UnityEngine.Random.Range(min, max);
            GameCharacter.CharacterData.GrowthCollectProvider = CollectFromData;
            GameCharacter.CharacterData.LingGenRollProvider = LingGenRuleService.RollIfNotGenerated;
        }

        /// <summary>
        /// 从角色数据收集全部成长加成：每件已装备装备 + 武器的词条，逐件投影后累加。
        /// </summary>
        public static GrowthSourceResult CollectFromData(GameCharacter.CharacterData data)
        {
            GrowthSourceResult result = new GrowthSourceResult();
            if (data == null)
            {
                return result;
            }

            Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = data.GetEquipments();
            if (equipments != null)
            {
                foreach (KeyValuePair<AEquipment.EquipTypeEnum, AEquipment> slot in equipments)
                {
                    if (slot.Value != null)
                    {
                        result.Add(FromAffixes(slot.Value.GetAffixes()));
                    }
                }
            }

            if (data.Weapon != null)
            {
                result.Add(FromAffixes(data.Weapon.GetAffixes()));
            }

            // 境界突破的永久加成（Struct 中性值，未突破时加零无副作用）
            // 激活内功的被动加成（同时仅一本；MaxMpFlat/mpRegenPerSec 走 Special 维度）
            if (data.Growth != null)
            {
                result.Add(data.Growth.PermanentRealmBonus);

                LAB2D.Domain.Gameplay.GongFa.GongFaDef neiGong =
                    LAB2D.Domain.Gameplay.GongFa.GongFaLibrary.Get(data.Growth.ActiveNeiGongId);
                if (neiGong != null && neiGong.IsNeiGong)
                {
                    result.Add(neiGong.Bonus);
                }
            }

            return result;
        }

        /// <summary>
        /// 词条列表 → 成长加成投影（纯函数，可测试）。
        /// 未知类型忽略，比例词条直接累加（多件装备的吸血/反伤叠加）。
        /// </summary>
        public static GrowthBonus FromAffixes(IReadOnlyList<EquipmentAffix> affixes)
        {
            if (affixes == null || affixes.Count == 0)
            {
                return GrowthBonus.Zero;
            }

            BattleStats stats = BattleStats.Zero;
            float maxHpFlat = 0f;
            float lifesteal = 0f;
            float reflect = 0f;

            foreach (EquipmentAffix affix in affixes)
            {
                switch (affix.Type)
                {
                    case EquipmentAffixType.FlatAtn:
                        stats += new BattleStats(affix.Value, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
                        break;
                    case EquipmentAffixType.FlatInt:
                        stats += new BattleStats(0f, affix.Value, 0f, 0f, 0f, 0f, 0f, 0f);
                        break;
                    case EquipmentAffixType.MaxHp:
                        maxHpFlat += affix.Value;
                        break;
                    case EquipmentAffixType.Lifesteal:
                        lifesteal += affix.Value;
                        break;
                    case EquipmentAffixType.Reflect:
                        reflect += affix.Value;
                        break;
                }
            }

            return new GrowthBonus(
                stats,
                maxHpFlat: maxHpFlat,
                lifestealRatio: lifesteal,
                reflectRatio: reflect);
        }
    }
}
