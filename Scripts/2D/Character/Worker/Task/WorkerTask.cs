using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public abstract class WorkerTask : IWorkerTask
    {
        public Vector3Int TargetMap { get; set; }

        public abstract bool execute(Worker worker);

        public virtual void complete() { 
            WorkerTaskManager.Instance.completeTask(this);
        }
    }

    public interface IWorkerTask
    {
        bool execute(Worker worker);

        void complete();
    }
}
