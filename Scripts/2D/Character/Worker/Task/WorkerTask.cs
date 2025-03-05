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
        
        protected int stage;

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
        /// <summary>
        /// ��ǰ����ʱ��
        /// </summary>
        protected float curProgress = 0.0f;
        /// <summary>
        /// ������Ҫ�ܵ�ʱ��
        /// </summary>
        protected float maxProgress = 2.0f;
        protected List<UnityAction<Worker>> stageInit;

        public WorkerTask(TaskType taskType) {
            TaskType = taskType;
            Name = taskType.ToString();
            AvailableNeighborPos = new List<Vector3Int>();
            stageInit = new List<UnityAction<Worker>>();
        }

        protected void changeStage(Worker worker, int stage)
        {
            if(stageInit.Count < stage + 1)
            {
                LogManager.Instance.log("û�иý׶�", LogManager.LogLevel.Error);
                return;
            }
            this.stage = stage;
            stageInit[stage].Invoke(worker);
        }


        /// <summary>
        /// ����Ҫ��д
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public bool execute(Worker worker)
        {
            _execute();
            // �����ۼ�ƣ��ֵ
            worker.CurTired -= Time.deltaTime * 0.1f;
            curProgress += Time.deltaTime;
            if (curProgress > maxProgress)
            {
                curProgress = 0;
                worker.setProgress(.0f, false);
                if (isFinish(worker))
                {
                    finish(worker);
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

        /// <summary>
        /// Worker�Ƿ���ԽӸ�����
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public virtual bool isCanWork(Worker worker) {
            return worker.TaskToggle[((int)TaskType)];
        }

        public virtual void giveUpTask(Worker worker) {
            LogManager.Instance.log("��������", LogManager.LogLevel.Warning);
            worker.giveUpTask();
        }

        public virtual void finish(Worker worker)
        {
            WorkerTaskManager.Instance.completeTask(this);
        }
    }

    public interface IWorkerTask
    {
        bool execute(Worker worker);

        void finish(Worker worker);

        /// <summary>
        /// �Ƿ�����ǰ��������Build��Ҫ���ϣ�Carry��ҪInventory��
        /// </summary>
        /// <returns></returns>
        bool isCanWork(Worker worker);
    }

    /// <summary>
    /// �������ȼ���Խ��ǰ���ȼ�Խ��
    /// </summary>
    public enum TaskType { 
        Build,
        Carry,
        Gather,
        Hungry,
        Exercise,
        Wear,
        Sleep,
        Plant,
    }
}
