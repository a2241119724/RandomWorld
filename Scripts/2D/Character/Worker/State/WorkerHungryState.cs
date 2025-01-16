using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// �ֿ�û�гԵ�,��һֱ�ڸ�״̬,��������������
    /// </summary>
    public class WorkerHungryState : WorkerState
    {
        public WorkerHungryState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Character.WorkerState.text = $"<color=red>����</color>";
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            // ����ӵ��˼���������ȥ�Է�
            if(Character.Manager.Task != null && Character.Manager.Task.TaskType == TaskType.Hungry)
            {
                Character.Manager.changeState(WorkerStateType.Seek);
            }
        }
    }
}
