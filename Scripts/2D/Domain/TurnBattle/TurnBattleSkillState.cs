namespace LAB2D.Domain.TurnBattle
{
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2DEnum = LAB2D.Enum;

    /// <summary>
    /// 回合制战斗中的技能快照 — 从 SkillManager.SkillData 映射而来，
    /// 冷却从"实时秒"换算为"回合数"，战斗内独立推进不回写实时光轴。
    /// </summary>
    public sealed class TurnBattleSkillState
    {
        /// <summary>技能 Id（与 SkillConstant 的字符串 Id 一致）。</summary>
        public string SkillId = string.Empty;

        /// <summary>显示名。</summary>
        public string SkillName = string.Empty;

        /// <summary>耗蓝（回合制内从快照 Mp 扣，普攻 0）。</summary>
        public int ManaCost;

        /// <summary>技能类型（SelfAOE/SingleTarget/SelfBuff/SelfHeal；Movement/Pull 在快照阶段已被过滤）。</summary>
        public LAB2DEnum.SkillType Type;

        /// <summary>效果类型（演出与治疗/伤害分流依据）。</summary>
        public LAB2DEnum.SkillEffectType EffectType;

        /// <summary>伤害/治疗/增益倍率（普攻 1.0）。</summary>
        public float EffectMultiplier = 1f;

        /// <summary>精神系技能（火球）用 INT 作伤害基数。</summary>
        public bool ScaleByInt;

        /// <summary>技能等级（伤害随等级 +10%/级）。</summary>
        public int Level = 1;

        /// <summary>冷却总回合数（实时秒经 ConvertCooldownSecondsToTurns 换算，≤1 回合的只耗蓝）。</summary>
        public int CooldownTurns;

        /// <summary>剩余冷却回合数（每回合结束递减，>0 不可选）。</summary>
        public int RemainingCooldown;

        /// <summary>Buff 持续回合数（SelfBuff 用，实时秒换算）。</summary>
        public int BuffDurationTurns;

        /// <summary>功法元素（决定五行克制）；默认技能无元素 = null，恒中性 1.0。</summary>
        public Element? Element;

        /// <summary>本回合是否可释放（存活单位视角：冷却 0 且蓝够）。</summary>
        public bool IsUsable(int currentMp)
        {
            return this.RemainingCooldown <= 0 && currentMp >= this.ManaCost;
        }
    }
}
