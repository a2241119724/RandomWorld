namespace LAB2D.Domain.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Domain.Character;
    using LAB2D.Domain.Gameplay.Cultivation;

    /// <summary>
    /// 回合制战斗阵营。
    /// </summary>
    public enum TurnBattleFaction
    {
        /// <summary>我方（玩家 + 交战 Worker）。</summary>
        Ally,

        /// <summary>敌方（交战 Enemy）。</summary>
        Enemy,
    }

    /// <summary>
    /// 回合制参战单位快照 — 战斗开始时从大世界 CharacterData 拍照，
    /// 战斗内一切结算只读写快照，结束时一次性写回。
    /// </summary>
    public sealed class TurnBattleUnit
    {
        /// <summary>单位 Id（对应 CharacterData.Id，写回时定位大世界角色）。</summary>
        public long UnitId;

        /// <summary>显示名。</summary>
        public string DisplayName = string.Empty;

        /// <summary>阵营。</summary>
        public TurnBattleFaction Faction;

        public float Hp;

        public float MaxHp;

        public int Mp;

        public int MaxMp;

        public int Level = 1;

        /// <summary>8 维战斗属性（SPD 定出手顺序、HIT 参与命中、CRT/CSD 暴击）。</summary>
        public BattleStats Stats;

        /// <summary>五行灵根副本（防守侧克制底座；Enemy 无灵根 = 空表恒 1.0）。</summary>
        public List<Element> LingGenElements = new List<Element>();

        /// <summary>是否玩家单位（倒下 = 立即判负，写回走 Player.Death 管线）。</summary>
        public bool IsPlayerUnit;

        /// <summary>技能快照列表（Worker/Enemy 第一版为空 = 只有普攻）。</summary>
        public List<TurnBattleSkillState> Skills = new List<TurnBattleSkillState>();

        /// <summary>攻击增益剩余回合数（SelfBuff，回合结束递减，归零清除增益）。</summary>
        public int AttackBuffTurns;

        /// <summary>攻击增益倍率（无 Buff = 1.0）。</summary>
        public float AttackBuffMultiplier = 1f;

        /// <summary>倒下（HP≤0）— 战斗内仅置灰退场，"死"的落定由写回规则决定。</summary>
        public bool IsDown
        {
            get { return this.Hp <= 0f; }
        }

        /// <summary>本回合是否可行动（未倒下）。</summary>
        public bool CanAct
        {
            get { return !this.IsDown; }
        }
    }
}
