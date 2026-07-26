namespace LAB2D.Enum
{
    using LAB2D;
    /// <summary>
    /// 技能效果类型枚举，定义技能激活后产生的具体游戏效果。
    /// 用于 SkillData 的效果标记和 SkillManager 的效果执行分支。
    /// 新增效果类型时追加到末尾，不得修改已有值的语义。
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>
        /// 物理伤害：基于玩家 ATN 属性计算伤害，受目标 DEF 减免。
        /// </summary>
        PhysicalDamage = 0,

        /// <summary>
        /// 魔法伤害：基于玩家 INT 属性计算伤害，受目标 RES 减免。
        /// 预留类型，当前版本暂无对应技能实现。
        /// </summary>
        MagicDamage = 1,

        /// <summary>
        /// 治疗：回复玩家自身生命值，不受防御属性影响。
        /// </summary>
        Heal = 2,

        /// <summary>
        /// 攻击力增益：临时提升玩家 ATN 属性。
        /// </summary>
        AttackBuff = 3,

        /// <summary>
        /// 防御增益：临时提升玩家 DEF 属性。
        /// 预留类型，当前版本暂无对应技能实现。
        /// </summary>
        DefenseBuff = 4,

        /// <summary>
        /// 移动速度增益：临时提升玩家 MoveSpeed。
        /// 预留类型，当前版本暂无对应技能实现。
        /// </summary>
        SpeedBuff = 5,

        /// <summary>
        /// 无敌帧：临时使玩家免疫所有伤害。
        /// </summary>
        Invincibility = 6,
    }
}
