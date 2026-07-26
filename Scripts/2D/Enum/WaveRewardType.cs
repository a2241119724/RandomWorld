namespace LAB2D.Enum
{
    /// <summary>
    /// 波间奖励类型。
    /// 用于奖励生成、奖励按钮、日志和后续成就条件复用。
    /// 后续可以追加新奖励类型，但已有枚举值语义不得改变。
    /// </summary>
    public enum WaveRewardType
    {
        /// <summary>恢复玩家生命值。</summary>
        Heal,

        /// <summary>给予玩家经验值。</summary>
        Experience,

        /// <summary>提高玩家本局伤害倍率。</summary>
        DamageBoost,

        /// <summary>降低玩家本局受到的伤害。</summary>
        DefenseBoost,

        /// <summary>提高玩家本局移动速度。</summary>
        MoveSpeedBoost,
    }
}
