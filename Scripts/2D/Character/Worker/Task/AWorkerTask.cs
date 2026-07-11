namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.Serializable;
    using LAB2D.Domain.Worker;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Worker任务
    /// </summary>
    [Serializable]
    public abstract class AWorkerTask : IWorkerTask
    {
        /// <summary>
        /// Worker在工作时的位置（上下左右）
        /// </summary>
        public List<Vector3IntLAB> AvailableNeighborPos;

        /// <summary>
        /// 临近的位置
        /// </summary>
        protected static readonly List<Vector3IntLAB> Neighbors = new ()
        {
            new Vector3IntLAB(0, 1, 0), // 上
            new Vector3IntLAB(1, 0, 0), // 右
            new Vector3IntLAB(0, -1, 0), // 下
            new Vector3IntLAB(-1, 0, 0), // 左
            new Vector3IntLAB(1, 1, 0), // 右上
            new Vector3IntLAB(1, -1, 0), // 右下
            new Vector3IntLAB(-1, -1, 0), // 左下
            new Vector3IntLAB(-1, 1, 0), // 左上
            new Vector3IntLAB(0, 0, 0), // 自身
        };

        /// <summary>
        /// 任务需要的时间
        /// </summary>
        protected float maxProgress = WorkerTaskTimeConfig.DefaultTaskSeconds;
        private readonly WorkerTaskProgressService progressService = new WorkerTaskProgressService();

        /// <summary>
        /// 任务阶段
        /// </summary>
        protected int stage;

        /// <summary>
        /// 当前经过时间
        /// </summary>
        protected float curProgress = 0.0f;

        /// <summary>
        /// 任务阶段上下文
        /// </summary>
        protected List<UnityAction<AWorker>> stageInit;

        public AWorkerTask(WorkerTaskTypeEnum taskType)
        {
            this.TaskType = taskType;
            this.Name = taskType.ToString();
            this.AvailableNeighborPos = new List<Vector3IntLAB>();
            this.stageInit = new List<UnityAction<AWorker>>();
            this.Init();
        }

        public enum RectType
        {
            /// <summary>
            /// 建造的Rect以鼠标为中心(房间)
            /// </summary>
            Center,

            /// <summary>
            /// 建造的Rect以鼠标为左下, Tile大于1格的(床)
            /// </summary>
            BottomLeft,

            /// <summary>
            /// 建造的Rect以鼠标为左上, 可自定义大小的建造(房间)
            /// </summary>
            TopLeft,
        }

        /// <summary>
        /// 任务优先级，越靠前优先级越高
        /// </summary>
        public enum WorkerTaskTypeEnum
        {
            /// <summary>
            /// 建造
            /// </summary>
            Build,

            /// <summary>
            /// 搬运
            /// </summary>
            Carry,

            /// <summary>
            /// 采集
            /// </summary>
            Gather,

            /// <summary>
            /// 吃饭
            /// </summary>
            Eat,

            /// <summary>
            /// 锻炼
            /// </summary>
            Exercise,

            /// <summary>
            /// 穿戴
            /// </summary>
            Wear,

            /// <summary>
            /// 睡觉
            /// </summary>
            Sleep,

            /// <summary>
            /// 种植
            /// </summary>
            Plant,
        }

        /// <summary>
        /// 任务ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// 目标位置, 仅用于阶段性目标
        /// </summary>
        public Vector3IntLAB TargetMap { get; protected set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public WorkerTaskTypeEnum TaskType { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 不需要重写
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否成功</returns>
        public bool Execute(AWorker worker)
        {
            // 工作扣减疲劳值
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            // 吃饭和睡觉任务不消耗疲劳
            if (this.TaskType != WorkerTaskTypeEnum.Eat && this.TaskType != WorkerTaskTypeEnum.Sleep)
            {
                workerData.CurTired = this.progressService.ApplyTiredCost(
                    workerData.CurTired,
                    Time.deltaTime,
                    WorkerTaskTimeConfig.WorkTiredCostPerSecond);
            }

            float progressMultiplier = WeatherGameplayEffect.Instance.GetWorkerTaskProgressMultiplier(this.TaskType);
            progressMultiplier *= WorkerConditionManager.Instance.GetWorkerTaskProgressMultiplier(worker, this.TaskType);
            WorkerTaskProgressResult progressResult = this.progressService.AdvanceProgress(
                this.curProgress,
                this.maxProgress,
                Time.deltaTime,
                progressMultiplier);
            this.curProgress = progressResult.CurrentProgress;
            if (progressResult.Completed)
            {
                worker.SetProgress(this.curProgress, false);
                if (this.StageChangeRule(worker))
                {
                    this.Finish(worker);
                    return true;
                }

                return false;
            }

            worker.SetProgress(this.progressService.GetProgressRatio(this.curProgress, this.maxProgress), true);
            return false;
        }

        /// <summary>
        /// 选择到最近的任务之后执行
        /// </summary>
        /// <param name="worker">Worker</param>
        public virtual void Start(AWorker worker)
        {
            this.curProgress = 0.0f;
            WorkerEfficiencyTracker.Instance.RecordTaskStarted(worker, this);
        }

        /// <summary>
        /// Worker是否可以接该任务
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        public bool IsCanWork(AWorker worker)
        {
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (!workerData.TaskToggle[(int)this.TaskType])
            {
                return false;
            }

            // 饥饿时候不能接任务
            if (workerData.CurHungry < AWorker.ThresholdHungry && this.TaskType != WorkerTaskTypeEnum.Eat)
            {
                return false;
            }

            // 是否有做任务的位置, 并且不是锻炼任务(由于目标位置不确定, 并且一定可以有位置做)
            if (this.TaskType != WorkerTaskTypeEnum.Exercise && this.AvailableNeighborPos.TrueForAll(pos =>
            {
                return !BuildMap.Instance.IsCanReach(Vector3IntLAB.ToVector3Int(pos + this.TargetMap));
            }))
            {
                return false;
            }

            return this.DoIsCanWork(worker);
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        /// <param name="worker">Worker</param>
        public virtual void GiveUpTask(AWorker worker)
        {
            LogManager.Instance.Log("放弃任务", LogManager.LogLevelEnum.Warning);
            worker.GiveUpTask();
        }

        /// <inheritdoc/>
        public virtual void Finish(AWorker worker)
        {
            // TODO 仅执行一次
            WorkerTaskManager.Instance.CompleteTask(this);
            WorkerEfficiencyTracker.Instance.RecordTaskCompleted(worker, this);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = null;
        }

        /// <summary>
        /// Worker是否可以接该任务,具体实现
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected abstract bool DoIsCanWork(AWorker worker);

        /// <summary>
        /// 初始化可用位置, 用于判断是否接受任务
        /// </summary>
        protected abstract void Init();

        /// <summary>
        /// 是否真的完成，为多阶段任务服务（Carry）
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected virtual bool StageChangeRule(AWorker worker)
        {
            return true;
        }

        /// <summary>
        /// 任务进入不同阶段,切换上下文
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="stage">任务所处阶段</param>
        protected void ChangeStage(AWorker worker, int stage)
        {
            if (this.stageInit.Count < stage + 1)
            {
                LogManager.Instance.Log("没有该阶段", LogManager.LogLevelEnum.Error);
                return;
            }

            this.stage = stage;
            this.stageInit[stage].Invoke(worker);
            worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
        }
    }
}
