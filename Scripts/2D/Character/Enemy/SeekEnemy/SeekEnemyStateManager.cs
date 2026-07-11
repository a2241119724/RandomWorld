namespace LAB2D.Character.Enemy.SeekEnemy
{
    using LAB2D;
    using System;

    /// <summary>
    /// 敌人状态机管理器.
    /// </summary>
    /// <typeparam name="CS">敌人状态.</typeparam>
    /// <typeparam name="CST">敌人状态类型.</typeparam>
    /// <typeparam name="C">敌人.</typeparam>
    public class SeekEnemyStateManager<CS, CST, C> : CharacterStateManager<CS, CST, C>
        where CS : ICharacterState
        where CST : Enum
        where C : ASeekEnemy
    {
        public SeekEnemyStateManager(C character)
            : base(character)
        {
        }
    }
}
