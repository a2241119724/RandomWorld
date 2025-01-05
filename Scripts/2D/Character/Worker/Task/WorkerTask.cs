using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public abstract class WorkerTask : IWorkerTask
    {
        /// <summary>
        /// Worker���Ա߽����λ�ã��������ң�
        /// </summary>
        public List<Vector3Int> BuildAvailableNeighborPos;

        protected static readonly List<Vector3Int> neighbors = new List<Vector3Int>(){
            new Vector3Int(0,1,0), // ��
            new Vector3Int(1,0,0), // ��
            new Vector3Int(0,-1,0), // ��
            new Vector3Int(-1,0,0), // ��
            new Vector3Int(1,1,0), // ����
            new Vector3Int(1,-1,0), // ����
            new Vector3Int(-1,-1,0), // ����
            new Vector3Int(-1,1,0), // ����
        };
        protected int curProgress = 0;
        protected int maxProgress = 1000;

        public WorkerTask() {
            BuildAvailableNeighborPos = new List<Vector3Int>();
        }

        public Vector3Int TargetMap { get; set; }

        public bool execute(Worker worker)
        {
            if (curProgress++ > maxProgress)
            {
                curProgress = 0;
                finish(worker);
                complete();
                worker.setProgress(.0f, false);
                return true;
            }
            worker.setProgress((float)curProgress / maxProgress, true);
            return false;
        }

        public abstract void finish(Worker worker);

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
