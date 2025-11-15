namespace LAB2D
{
    /// <summary>
    /// 敌人状态
    /// </summary>
    public abstract class ACommonEnemyState : CharacterState<ACommonEnemy>
    {
        public ACommonEnemyState(ACommonEnemy enemy)
            : base(enemy)
        {
        }

        /// <summary>
        /// 漫游状态:感知(看到或听到)到玩家进入跟踪状态
        /// 搜索状态:一段时间内没有感知到玩家进入漫游状态,感知到玩家进入跟踪状态
        /// 跟踪状态:不能感知到玩家进入搜索状态,进入攻击范围进入攻击状态
        /// 攻击状态:能感知到但大于攻击范围进入跟踪状态,不能感知到玩家进入搜索状态
        /// 死亡状态:死亡操作(血量为0进入)
        /// 注:如果攻击范围大于感知范围,玩家远离,直接会进入搜索状态
        /// 如果攻击范围小于感知范围,玩家远离,会先进入跟踪状态,然后进入搜索状态
        /// 受到玩家攻击进入搜索状态
        /// </summary>
        public enum TypeEnum
        {
            /// <summary>
            /// 漫游状态
            /// </summary>
            Wander,

            /// <summary>
            /// 搜索状态
            /// </summary>
            Seek,

            /// <summary>
            /// 追踪状态
            /// </summary>
            Chase,

            /// <summary>
            /// 攻击状态
            /// </summary>
            Attack,

            /// <summary>
            /// 死亡状态
            /// </summary>
            Dead,
        }
    }
}
