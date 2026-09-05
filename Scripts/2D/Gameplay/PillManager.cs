namespace LAB2D.Gameplay
{
    using System;
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Alchemy;
    using LAB2D.Domain.Gameplay.Cultivation;
    using UnityEngine;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 炼丹管理器 — 丹方规则落地宿主：扣灵气、按品质结算即时效果、Tip 反馈。
    /// 效果语义：聚气/治伤/归元立即生效；渡劫辅助以「等效补灵气」实现
    /// （Qi += 当前境需求 × 减免比例，零新存档字段、零 CultivationManager 改动，
    /// 突破扣全额时等效打折，Qi 溢出无副作用——玩家突破手动触发）；
    /// 洗髓走 PermanentRealmBonus 累进管线并触发属性重算。
    /// 单例，由 GlobalInit 注册。入口：Editor 菜单「工具/炼丹」（Play 模式）；
    /// K 面板丹药按钮行待场景摆节点后接入（面板已改场景摆放，纯代码不自建）。
    /// </summary>
    public class PillManager : Singleton<PillManager>
    {
        internal static Action<string> TipProvider { get; set; }
            = (msg) =>
            {
                try
                {
                    Core.GameServices.ShowTipProvider(msg);
                }
                catch (Exception)
                {
                    // Tip 不可用时静默降级（初始化早期/测试环境）
                }
            };

        /// <summary>为玩家炼丹（Editor 菜单与后续 UI 入口共用）。</summary>
        public bool TryCraftForPlayer(string pillId)
        {
            return this.TryCraft(pillId, CultivationManager.GetPlayerData());
        }

        /// <summary>
        /// 炼丹入口：规则判定 → 扣灵气 → 品质结算 → 效果落地 → Tip 反馈。
        /// </summary>
        /// <returns>是否炼成。</returns>
        public bool TryCraft(string pillId, GameCharacter.CharacterData data)
        {
            PillDef pill = PillLibrary.FindById(pillId);
            if (pill == null || data == null)
            {
                TipProvider("丹方不存在");
                return false;
            }

            GrowthData.Ensure(ref data.Growth);
            if (!PillRuleService.TryCraft(data.Growth, pill, out PillCraftResult result))
            {
                this.TipCraftBlocked(pill, data.Growth);
                return false;
            }

            this.ApplyEffect(data, result);
            TipProvider($"炼成{QualityToName(result.Quality)}·{pill.Name}（{this.DescribeEffect(data.Growth, result)}）");
            AWorkerTask.LogProvider(
                $"[PillDiag] {data.Name} 炼制{pill.Name} -> {QualityToName(result.Quality)}",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        private void TipCraftBlocked(PillDef pill, GrowthData growth)
        {
            if (pill.Effect == PillEffectType.BreakthroughAid && RealmLibrary.IsMax(growth.RealmIndex))
            {
                TipProvider($"已至{RealmRuleService.GetRealm(growth).Name}巅峰，无可破之境");
            }
            else if (growth.RealmIndex < pill.RequiredRealmIndex)
            {
                TipProvider($"境界不足：{pill.Name}需{RealmLibrary.Get(pill.RequiredRealmIndex).Name}期");
            }
            else
            {
                TipProvider($"灵气不足：炼制{pill.Name}需 {pill.QiCost:F0} 灵气");
            }
        }

        /// <summary>按效果类型落地结算值（数值已在 Domain 层乘品质倍率）。</summary>
        private void ApplyEffect(GameCharacter.CharacterData data, PillCraftResult result)
        {
            switch (result.Pill.Effect)
            {
                case PillEffectType.GainQi:
                    data.Growth.Qi += result.EffectValue;
                    break;

                case PillEffectType.HealHp:
                    data.Hp = Mathf.Min(data.MaxHp, data.Hp + data.MaxHp * result.EffectValue);
                    break;

                case PillEffectType.RestoreMp:
                    data.Mp = Mathf.Min(data.MaxMp, data.Mp + (int)(data.MaxMp * result.EffectValue));
                    break;

                case PillEffectType.BreakthroughAid:
                    data.Growth.Qi += RealmRuleService.QiToNext(data.Growth) * result.EffectValue;
                    break;

                case PillEffectType.PermanentStats:
                    data.Growth.PermanentRealmBonus += new GrowthBonus(result.PermanentBonus);
                    data.Character?.RecomputeGrowthAttributes();
                    break;
            }
        }

        private string DescribeEffect(GrowthData growth, PillCraftResult result)
        {
            switch (result.Pill.Effect)
            {
                case PillEffectType.GainQi:
                    return $"+{result.EffectValue:F0} 灵气";
                case PillEffectType.HealHp:
                    return $"恢复{result.EffectValue:P0}生命";
                case PillEffectType.RestoreMp:
                    return $"恢复{result.EffectValue:P0}灵力";
                case PillEffectType.BreakthroughAid:
                    return $"破境之资+{RealmRuleService.QiToNext(growth) * result.EffectValue:F0}灵气";
                case PillEffectType.PermanentStats:
                    return $"永久强化（攻+{result.PermanentBonus.ATN:F0} 防+{result.PermanentBonus.DEF:F0}）";
                default:
                    return string.Empty;
            }
        }

        internal static string QualityToName(PillQuality quality)
        {
            switch (quality)
            {
                case PillQuality.Superior: return "上品";
                case PillQuality.Premium: return "极品";
                default: return "凡品";
            }
        }
    }
}
