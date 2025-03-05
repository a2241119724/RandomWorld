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
        /// Worker在工作时的位置（上下左右）
        /// </summary>
        public List<Vector3Int> AvailableNeighborPos;
        public Vector3Int TargetMap { get; set; }
        public TaskType TaskType { get; set; }
        public string Name { set; get; }
        
        protected int stage;

        protected static readonly List<Vector3Int> neighbors = new List<Vector3Int>(){
            new Vector3Int(0,1,0), // 上
            new Vector3Int(1,0,0), // 右
            new Vector3Int(0,-1,0), // 下
            new Vector3Int(-1,0,0), // 左
            new Vector3Int(1,1,0), // 右上
            new Vector3Int(1,-1,0), // 右下
            new Vector3Int(-1,-1,0), // 左下
            new Vector3Int(-1,1,0), // 左上
            new Vector3Int(0,0,0), // 自身
        };
        /// <summary>
        /// 当前经过时间
        /// </summary>
        protected float curProgress = 0.0f;
        /// <summary>
        /// 任务需要总的时间
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
                LogManager.Instance.log("没有该阶段", LogManager.LogLevel.Error);
                return;
            }
            this.stage = stage;
            stageInit[stage].Invoke(worker);
        }


        /// <summary>
        /// 不需要重写
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public bool execute(Worker worker)
        {
            _execute();
            // 工作扣减疲劳值
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
        /// 是否真的完成，为多阶段任务服务（Carry）
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        protected virtual bool isFinish(Worker worker) {
            return true;
        }

        /// <summary>
        /// 选择到最近的任务之后执行
        /// </summary>
        /// <param name="worker"></param>
        public virtual void start(Worker worker) { }

        /// <summary>
        /// Worker是否可以接该任务
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        public virtual bool isCanWork(Worker worker) {
            return worker.TaskToggle[((int)TaskType)];
        }

        public virtual void giveUpTask(Worker worker) {
            LogManager.Instance.log("放弃任务", LogManager.LogLevel.Warning);
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
        /// 是否满足前提条件（Build需要材料，Carry需要Inventory）
        /// </summary>
        /// <returns></returns>
        bool isCanWork(Worker worker);
    }

    /// <summary>
    /// 任务优先级，越靠前优先级越高
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
