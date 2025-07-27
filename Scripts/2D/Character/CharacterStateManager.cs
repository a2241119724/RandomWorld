namespace LAB2D
{
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
                LogManager.Instance.Log("state is null!!!", LogManager.LogLevel.Error);
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
            if (!this.States.ContainsKey(type))
            {
                LogManager.Instance.Log("states Not Contain type!!!", LogManager.LogLevel.Error);
                return;
            }

            if (this.CurrentState != null)
            {
                this.CurrentState.OnExit();
            }

            this.CurrentStateType = type;
            this.CurrentState = this.States[type];
            this.CurrentState.OnEnter();
        }
    }

    /// <summary>
    /// 角色状态管理
    /// </summary>
    /// <typeparam name="CS">CharacterState</typeparam>
    /// <typeparam name="CST">CharacterStateType</typeparam>
    public interface ICharacterStateManager<CS, CST>
    {
        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="type">状态类型</param>
        /// <param name="state">状态</param>
        void AddState(CST type, CS state);

        /// <summary>
        /// 改变状态
        /// </summary>
        /// <param name="type">状态类型</param>
        void ChangeState(CST type);
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
    public enum EnemyStateType
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

    /// <summary>
    /// Worker状态类型
    /// </summary>
    public enum WorkerStateType
    {
        /// <summary>
        /// 移动状态
        /// </summary>
        Move,

        /// <summary>
        /// 工作状态
        /// </summary>
        Work,

        /// <summary>
        /// 吃饭装她爱
        /// </summary>
        Eat,

        /// <summary>
        /// 死亡状态
        /// </summary>
        Dead,

        /// <summary>
        /// 寻路状态
        /// </summary>
        Seek,

        /// <summary>
        /// 攻击状态
        /// </summary>
        Attack,

        /// <summary>
        /// 逃跑状态
        /// </summary>
        Escape,
    }
}
