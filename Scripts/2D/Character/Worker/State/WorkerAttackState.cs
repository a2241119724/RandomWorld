using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerAttackState : WorkerState
    {
        public WorkerAttackState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            Character.WorkerState.text = $"<color=red>¹¥»÷</color>";
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if(Character.WearData.weapon == null)
            {
                Character.Manager.changeState(WorkerStateType.Escape);
                return;
            }
            // ÄÃ³öÎäÆ÷
            // ¹¥»÷
        }
    }
}
