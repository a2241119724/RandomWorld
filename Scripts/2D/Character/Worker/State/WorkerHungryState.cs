using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// 仓库没有吃的,就一直在该状态,不能做其他事情
    /// </summary>
    public class WorkerHungryState : WorkerState
    {
        public WorkerHungryState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Character.WorkerState.text = $"<color=red>饿了</color>";
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            // 如果接到了饥饿任务，则去吃饭
            if(Character.Manager.Task != null && Character.Manager.Task.TaskType == TaskType.Hungry)
            {
                Character.Manager.changeState(WorkerStateType.Seek);
            }
        }
    }
}
