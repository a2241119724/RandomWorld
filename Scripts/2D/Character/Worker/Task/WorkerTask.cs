using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LAB2D
{
    public abstract class WorkerTask : IWorkerTask
    {
        /// <summary>
        /// Worker�ڹ���ʱ��λ�ã��������ң�
        /// </summary>
        public List<Vector3Int> AvailableNeighborPos;
        public Vector3Int TargetMap { get; set; }
        public TaskType TaskType { get; set; }
        public string Name { set; get; }

        protected static readonly List<Vector3Int> neighbors = new List<Vector3Int>(){
            new Vector3Int(0,1,0), // ��
            new Vector3Int(1,0,0), // ��
            new Vector3Int(0,-1,0), // ��
            new Vector3Int(-1,0,0), // ��
            new Vector3Int(1,1,0), // ����
            new Vector3Int(1,-1,0), // ����
            new Vector3Int(-1,-1,0), // ����
            new Vector3Int(-1,1,0), // ����
            new Vector3Int(0,0,0), // ����
        };
        protected int curProgress = 0;
        protected int maxProgress = 1000;
        protected List<UnityAction<Worker>> stageInit;

        public WorkerTask() {
            AvailableNeighborPos = new List<Vector3Int>();
            stageInit = new List<UnityAction<Worker>>();
        }

        public bool execute(Worker worker)
        {
            if (curProgress == 0)
            {
                workerStart(worker);
            }
            _execute();
            if (curProgress++ > maxProgress)
            {
                curProgress = 0;
                worker.setProgress(.0f, false);
                if (isFinish(worker))
                {
                    finish(worker);
                    complete();
                    return true;
                }
                return false;
            }
            worker.setProgress((float)curProgress / maxProgress, true);
            return false;
        }

        public virtual void _execute() { 
        }

        /// <summary>
        /// �Ƿ������ɣ�Ϊ��׶��������Carry��
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        protected virtual bool isFinish(Worker worker) {
            return true;
        }

        /// <summary>
        /// ѡ�����������֮��ִ��
        /// </summary>
        /// <param name="worker"></param>
        public virtual void start(Worker worker) { }

        protected virtual void workerStart(Worker worker) { }

        protected virtual void finish(Worker worker) { }

        public virtual void complete() { 
            WorkerTaskManager.Instance.completeTask(this);
        }

        public virtual bool isCanWork(Worker worker) {
            return worker.TaskToggle[((int)TaskType)];
        }

        public virtual void giveUpTask(Worker worker) {
            worker.giveUpTask();
        }
    }

    public interface IWorkerTask
    {
        bool execute(Worker worker);

        void complete();

        /// <summary>
        /// �Ƿ�����ǰ��������Build��Ҫ���ϣ�Carry��ҪInventory��
        /// </summary>
        /// <returns></returns>
        bool isCanWork(Worker worker);
    }

    /// <summary>
    /// ���ȼ���Խ��ǰ���ȼ�Խ��
    /// </summary>
    public enum TaskType { 
        Build,
        Carry,
        CutTree,
        Hungry
    }
}
