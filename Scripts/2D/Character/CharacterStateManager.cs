namespace LAB2D.Character
{
    using LAB2D;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 角色状态管理
    /// </summary>
    /// <typeparam name="CS">ICharacterState</typeparam>
    /// <typeparam name="CST">ICharacterStateType</typeparam>
    /// <typeparam name="C">Character</typeparam>
    public abstract class CharacterStateManager<CS, CST, C> : ICharacterStateManager<CS, CST>
        where CS : ICharacterState
        where CST : Enum
        where C : Character
    {
        public CharacterStateManager(C character)
        {
            this.States = new Dictionary<CST, CS>();
            this.Character = character;
        }

        /// <summary>
        /// 当前处于的状态类
        /// </summary>
        public CS CurrentState { get; private set; }

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public CST CurrentStateType { get; private set; }

        /// <summary>
        /// 角色
        /// </summary>
        public C Character { get; set; }

        /// <summary>
        /// 存储所有的状态类型与对应的状态类
        /// </summary>
        public Dictionary<CST, CS> States { get; private set; }

        /// <inheritdoc/>
        public virtual void AddState(CST type, CS state)
        {
            if (state == null)
            {
                AWorkerTask.LogProvider("state is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            if (!this.States.ContainsKey(type))
            {
                this.States.Add(type, state);
            }
        }

        /// <summary>
        /// 转换敌人当前状态为type
        /// </summary>
        /// <param name="type">所要转换的状态</param>
        public virtual void ChangeState(CST type)
        {
            if (this.CurrentState != null)
            {
                this.CurrentState.OnExit();
            }

            if (!this.States.ContainsKey(type))
            {
                AWorkerTask.LogProvider("states Not Contain type!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.CurrentStateType = type;
            this.CurrentState = this.States[type];
            this.CurrentState.OnEnter();
        }
    }
}
