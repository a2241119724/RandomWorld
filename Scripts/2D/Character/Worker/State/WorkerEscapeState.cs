using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerEscapeState : WorkerState
    {
        private const float recordTime = 5.0f;
        private float _recordTime = 0.0f;

        public WorkerEscapeState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _recordTime = 0.0f;
            Character.WorkerState.text = $"<color=red>Ã”≈‹</color>";
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            _recordTime += Time.deltaTime;
            if(_recordTime >= recordTime)
            {
                Character.Manager.changeState(WorkerStateType.Seek);
            }
            Character.LineRenderer.positionCount = 0;
            Character.transform.Translate(Vector3.up * Time.deltaTime * Character.moveSpeed,Space.World);
        }
    }
}
