using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class WorkerState : CharacterState<Worker>
    {
        protected string preString = "";

        public WorkerState(Worker worker) : base(worker) { }

        public override void OnEnter()
        {
            base.OnEnter();
            preString = "";
            if (Character.Manager.Task != null)
            {
                preString = $"<color=red>{Character.Manager.Task.Name}</color>\n";
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
