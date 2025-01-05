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
            Vector3Int posMap = TileMap.Instance.worldPosToMapPos(Character.transform.position);
            Character.WorkerState.text = preString + $"Move\n" +
                $"Target: {Character.TargetMap.y},{Character.TargetMap.x}\n" +
                $"Position: {posMap.y},{posMap.x}";
            bool isTarget = Character.moveByPath();
            if (isTarget)
            {
                if (Character.Manager.Task == null)
                {
                    recordTime += Time.deltaTime;
                    // ��Ϣ2��
                    if (recordTime < 2) return;
                    // û������ͽ���Ѱ·״̬
                    Character.Manager.changeState(WorkerStateType.Seek);
                }
                else
                {
                    // ������ͽ��빤��״̬
                    Character.Manager.changeState(WorkerStateType.Work);
                }
            }
        }
    }
}
