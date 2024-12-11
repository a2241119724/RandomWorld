using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerMoveState : CharacterState<Worker>
    {
        private string preString = "";
        private float recordTime = 0.0f;

        public WorkerMoveState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            preString = "";
            recordTime = 0.0f;
            if (Character.Manager.Task != null)
            {
                preString = "<color=red>Worker</color>\n";
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Character.WorkerState.text = preString + $"Move\n" +
                $"Target: {Character.TargetMap.y},{Character.TargetMap.x}\n" +
                $"Position: {Mathf.RoundToInt(Character.transform.position.x)},{Mathf.RoundToInt(Character.transform.position.y)}";
            bool isTarget = Character.moveByPath();
            if (isTarget)
            {
                recordTime += Time.deltaTime;
                if (recordTime < 2) return;
                if (Character.Manager.Task == null)
                {
                    // 没有任务就进入寻路状态
                    Character.Manager.changeState(WorkerStateType.Seek);
                }
                else
                {
                    // 有任务就进入工作状态
                    Character.Manager.changeState(WorkerStateType.Work);
                }
            }
        }
    }
}
