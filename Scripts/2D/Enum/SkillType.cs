namespace LAB2D.Enum
{
    using LAB2D;
    /// <summary>
    /// 主动技能类型枚举，定义技能的释放方式和目标选择逻辑。
    /// 用于 SkillData 的类型标记和 SkillManager 的激活分支路由。
    /// 新增技能类型时追加到末尾，不得修改已有值的语义。
    /// </summary>
    public enum SkillType
    {
        /// <summary>
        /// 自身为中心的范围技能：对玩家周围一定半径内的所有敌人造成伤害。
        /// 典型技能：旋风斩。
        /// </summary>
        SelfAOE = 0,

        /// <summary>
        /// 指向性投射技能：向鼠标或朝向方向发射投射物。
        /// 预留类型，当前版本暂无对应技能实现。
        /// </summary>
        Projectile = 1,

        /// <summary>
        /// 自身增益技能：为玩家自身附加临时属性 Buff（攻击力、防御力、速度等）。
        /// 典型技能：力量爆发。
        /// </summary>
        SelfBuff = 2,

        /// <summary>
        /// 位移技能：快速移动一段距离，通常附带短暂无敌帧。
        /// 典型技能：冲刺。
        /// </summary>
        Movement = 3,

        /// <summary>
        /// 自身回复技能：立即回复玩家生命值。
        /// 典型技能：治疗之光。
        /// </summary>
        SelfHeal = 4,
    }
}
