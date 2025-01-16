using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public abstract class CharacterStateManager<CS,CST,C> : ICharacterStateManager<CS, CST> where CS : ICharacterState where CST : Enum where C : Character
    {
        /// <summary>
        /// ��ǰ���ڵ�״̬��
        /// </summary>
        public CS CurrentState { get; private set; }
        public CST CurrentStateType { get; private set; }
        public C Character { set; get; }

        /// <summary>
        /// �洢���е�״̬�������Ӧ��״̬��
        /// </summary>
        private Dictionary<CST, CS> states;

        public CharacterStateManager(C character)
        {
            states = new Dictionary<CST, CS>();
            this.Character = character;
        }

        /// <summary>
        /// ��������������е���״̬
        /// </summary>
        /// <param name="enemy">��Ҫ��ӵ��ĵ�����</param>
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
        /// ת�����˵�ǰ״̬Ϊtype
        /// </summary>
        /// <param name="type">��Ҫת����״̬</param>
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
    /// ����״̬:��֪(����������)����ҽ������״̬
    /// ����״̬:һ��ʱ����û�и�֪����ҽ�������״̬,��֪����ҽ������״̬
    /// ����״̬:���ܸ�֪����ҽ�������״̬,���빥����Χ���빥��״̬
    /// ����״̬:�ܸ�֪�������ڹ�����Χ�������״̬,���ܸ�֪����ҽ�������״̬
    /// ����״̬:��������(Ѫ��Ϊ0����)
    /// ע:���������Χ���ڸ�֪��Χ,���Զ��,ֱ�ӻ��������״̬
    /// ���������ΧС�ڸ�֪��Χ,���Զ��,���Ƚ������״̬,Ȼ���������״̬
    /// �ܵ���ҹ�����������״̬
    /// </summary>
    public enum EnemyStateType
    {
        Wander, // ����״̬ 	
        Seek, // ����״̬ 	
        Chase, // ׷��״̬ 	
        Attack, // ����״̬ 	
        Dead, // ����״̬ 
    }

    public enum WorkerStateType
    {
        Move, // ����״̬	
        Work,
        Hungry,
        Dead,
        Seek
    }
}
