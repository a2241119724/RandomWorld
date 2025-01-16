using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public abstract class CharacterStateManager<CS,CST,C> : ICharacterStateManager<CS, CST> where CS : ICharacterState where CST : Enum where C : Character
    {
        /// <summary>
        /// 当前处于的状态类
        /// </summary>
        public CS CurrentState { get; private set; }
        public CST CurrentStateType { get; private set; }
        public C Character { set; get; }

        /// <summary>
        /// 存储所有的状态类型与对应的状态类
        /// </summary>
        private Dictionary<CST, CS> states;

        public CharacterStateManager(C character)
        {
            states = new Dictionary<CST, CS>();
            this.Character = character;
        }

        /// <summary>
        /// 给敌人类添加所有敌人状态
        /// </summary>
        /// <param name="enemy">索要添加到的敌人类</param>
        public virtual void addStates(CST type, CS enemyState)
        {
            if (enemyState == null)
            {
                Debug.LogError("enemyState is null!!!");
                return;
            }
            if (!states.ContainsKey(type))
            {
                states.Add(type, enemyState);
            }
        }

        /// <summary>
        /// 转换敌人当前状态为type
        /// </summary>
        /// <param name="type">所要转换的状态</param>
        public virtual void changeState(CST type)
        {
            if (!states.ContainsKey(type))
            {
                Debug.Log("states Not Contain type!!!");
                return;
            }
            if (CurrentState != null)
            {
                CurrentState.OnExit();
            }
            CurrentStateType = type;
            CurrentState = states[type];
            CurrentState.OnEnter();
        }
    }

    public interface ICharacterStateManager<CS, CST>
    {
        void addStates(CST type, CS enemyState);
        void changeState(CST type);
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
        Wander, // 漫游状态 	
        Seek, // 搜索状态 	
        Chase, // 追踪状态 	
        Attack, // 攻击状态 	
        Dead, // 死亡状态 
    }

    public enum WorkerStateType
    {
        Move, // 漫游状态	
        Work,
        Hungry,
        Dead,
        Seek
    }
}
