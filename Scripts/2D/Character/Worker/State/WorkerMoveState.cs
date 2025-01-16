using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerMoveState : WorkerState
    {
        private float recordTime = 0.0f;

        public WorkerMoveState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            recordTime = 0.0f;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(Character.transform.position);
            Character.WorkerState.text = preString + $"Move\n" +
                $"Target: {Character.TargetMap.x},{Character.TargetMap.y}\n" +
                $"Position: {posMap.x},{posMap.y}\n" + $"Hungry:{Mathf.RoundToInt(Character.CurHungry)}\n";
            bool isTarget = Character.moveByPath();
            if (isTarget)
            {
                if (Character.Manager.Task == null)
                {
                    recordTime += Time.deltaTime;
                    // 休息2秒
                    if (recordTime < 2) return;
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
