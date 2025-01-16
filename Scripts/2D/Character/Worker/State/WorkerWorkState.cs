using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerWorkState : WorkerState {
        public WorkerWorkState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            if (Character.Manager.Task == null) return;
            Character.WorkerState.text = preString +
                $"Target: {Character.Manager.Task.TargetMap.x},{Character.Manager.Task.TargetMap.y}";
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (Character.Manager.Task == null) return;
            bool isComplete = Character.Manager.Task.execute(Character);
            if (!isComplete) return;
            // �������
            Character.Manager.Task = null;
            Character.Manager.changeState(WorkerStateType.Seek);
        }
    }
}