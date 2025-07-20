namespace LAB2D
{
    using System;

    /// <summary>
    /// Worker状态管理器
    /// </summary>
    /// <typeparam name="CS">角色状态</typeparam>
    /// <typeparam name="CST">角色状态类型</typeparam>
    /// <typeparam name="C">角色</typeparam>
    public class WorkerStateManager<CS, CST, C> : CharacterStateManager<CS, CST, C>
        where CS : ICharacterState
        where CST : Enum
        where C : Worker
    {
        /// <summary>
        /// 任务
        /// </summary>
        public WorkerTask Task { get; set; }

        public WorkerStateManager(C character)
            : base(character)
        {
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="type">切换的状态</param>
        public override void changeState(CST type)
        {
            // 先执行,可以在Enter中更改,不然会被覆盖
            this.Character.WorkerState.text = this.CurrentStateType.ToString();
            base.changeState(type);
        }
    }
}
