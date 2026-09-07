namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    /// <summary>
    /// 敌人状态
    /// </summary>
    public abstract class ASeekEnemyState : ACharacterState<ASeekEnemy>
    {
        public ASeekEnemyState(ASeekEnemy enemy)
            : base(enemy)
        {
        }

        public enum TypeEnum
        {
            /// <summary>
            /// 寻路状态
            /// </summary>
            Seek,

            /// <summary>
            /// 移动状态
            /// </summary>
            Move,

            /// <summary>
            /// 攻击状态
            /// </summary>
            Attack,

            /// <summary>
            /// 攻城状态 — 夜间长时间追不到角色时攻向最近阵营核心（P4）
            /// </summary>
            Siege,

            /// <summary>
            /// 死亡状态
            /// </summary>
            Dead,
        }
    }
}
