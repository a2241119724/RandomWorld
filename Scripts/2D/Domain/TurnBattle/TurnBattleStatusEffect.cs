namespace LAB2D.Domain.TurnBattle
{
    /// <summary>
    /// 回合制战斗状态效果类型。
    /// </summary>
    public enum TurnBattleStatusKind
    {
        /// <summary>中毒：回合结束掉血，数值按目标 MaxHp 比率在施加时算定。</summary>
        Poison,

        /// <summary>灼烧：回合结束掉血，数值按施加者攻击基数（ATN/INT）比率算定（火系技能附带）。</summary>
        Burn,

        /// <summary>流血：回合结束掉血，数值按目标 MaxHp 比率算定（物理技能附带）。</summary>
        Bleed,

        /// <summary>眩晕：跳过行动（行动时拦截并演出可见）。</summary>
        Stun,

        /// <summary>攻击提升：造成伤害 ×Value。</summary>
        AttackUp,

        /// <summary>攻击降低：造成伤害 ×Value。</summary>
        AttackDown,

        /// <summary>防御提升：受到伤害 ×Value（&lt;1 为减伤）。</summary>
        DefenseUp,

        /// <summary>防御降低：受到伤害 ×Value（&gt;1 为增伤）。</summary>
        DefenseDown,
    }

    /// <summary>
    /// 单位身上的一种状态实例 — 同类状态单实例，
    /// 重复施加 = 刷新剩余回合并覆盖数值（最简可测叠加规则）。
    /// </summary>
    public sealed class TurnBattleStatusEffect
    {
        public TurnBattleStatusKind Kind;

        /// <summary>剩余回合数（回合结束递减，归零移除）。</summary>
        public int RemainingTurns;

        /// <summary>DOT=施加时算定的每回合伤害；增减益=伤害倍率（1.3/0.75 之类）；眩晕未用。</summary>
        public float Value;

        /// <summary>施加者 UnitId（DOT 致死归因与演出条目 ActorId）。</summary>
        public long SourceUnitId;

        /// <summary>来源技能名（演出日志用）。</summary>
        public string SourceName = string.Empty;
    }
}
