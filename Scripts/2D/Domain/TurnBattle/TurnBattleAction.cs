namespace LAB2D.Domain.TurnBattle
{
    using System.Collections.Generic;

    /// <summary>
    /// 玩家/单位在本回合选择的行动。
    /// </summary>
    public enum TurnBattleActionKind
    {
        /// <summary>普攻（0 耗蓝，永远可用 — MP 不足的兜底）。</summary>
        Attack,

        /// <summary>释放技能（Skills[SkillIndex]）。</summary>
        UseSkill,

        /// <summary>使用道具（第一版：血瓶，回血量由 Manager 查物品表传入）。</summary>
        UseItem,

        /// <summary>尝试逃跑（可能失败并浪费本回合）。</summary>
        Flee,
    }

    /// <summary>
    /// 行动请求 — 玩家菜单或敌方 AI 产生，交规则引擎结算。
    /// </summary>
    public sealed class TurnBattleAction
    {
        public TurnBattleActionKind Kind;

        /// <summary>UseSkill：施放者 Skills 列表索引。</summary>
        public int SkillIndex = -1;

        /// <summary>Attack/UseSkill(单体)：目标 UnitId（-1 = 自动选取首个存活敌方）。</summary>
        public long TargetUnitId = -1;

        /// <summary>UseItem：治疗量（Manager 查物品表填入，规则层不感知具体物品）。</summary>
        public float ItemHealAmount;

        /// <summary>UseItem：显示名（演出日志用）。</summary>
        public string ItemDisplayName = string.Empty;
    }

    /// <summary>
    /// 单条结算效果 — 演出的最小单元（飘字/白闪/MISS/倒下）。
    /// </summary>
    public enum TurnBattleEffectKind
    {
        /// <summary>造成伤害（Value = 伤害量）。</summary>
        Damage,

        /// <summary>治疗（Value = 治疗量）。</summary>
        Heal,

        /// <summary>未命中。</summary>
        Miss,

        /// <summary>攻击增益生效（Value = 倍率，UI 显示"攻击提升"）。</summary>
        Buff,

        /// <summary>单位倒下（目标卡置灰下沉）。</summary>
        Down,

        /// <summary>逃跑失败（浪费一回合，敌方全体获得行动）。</summary>
        FleeFail,

        /// <summary>逃跑成功（战斗结束）。</summary>
        FleeSuccess,
    }

    /// <summary>
    /// 结算效果条目 — UI 按序演出。
    /// </summary>
    public sealed class TurnBattleEffectEntry
    {
        public TurnBattleEffectKind Kind;

        /// <summary>行动者 UnitId（演出时行动者卡片前冲）。</summary>
        public long ActorId;

        /// <summary>目标 UnitId（Miss/Down 也带目标）。</summary>
        public long TargetId;

        /// <summary>数值（伤害/治疗量）。</summary>
        public float Value;

        public bool IsCritical;

        /// <summary>五行克制倍率（≠1 时 UI 显示"克制!/抵抗"）。</summary>
        public float ElementMultiplier = 1f;

        /// <summary>结算后目标 HP（血条动画直接对齐）。</summary>
        public float RemainingHp;

        /// <summary>技能/道具名（演出日志用，普攻为空）。</summary>
        public string SourceName = string.Empty;
    }

    /// <summary>
    /// 一次行动的完整结算 — 规则引擎返回值（不走 EventBus），
    /// 由 Manager 交 UI 演出；全部效果按列表顺序播放。
    /// </summary>
    public sealed class TurnBattleActionResolution
    {
        /// <summary>行动者 UnitId。</summary>
        public long ActorId;

        public TurnBattleFaction ActorFaction;

        /// <summary>发生回合。</summary>
        public int Round;

        public TurnBattleActionKind ActionKind;

        /// <summary>结算效果（按序演出）。</summary>
        public List<TurnBattleEffectEntry> Effects = new List<TurnBattleEffectEntry>();
    }
}
