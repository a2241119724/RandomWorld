namespace LAB2D
{
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
        /// 任务需要总的时间
        /// </summary>
        protected static float maxProgress = 2.0f;

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
            this.DoExecute();

            // 工作扣减疲劳值
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.CurTired -= Time.deltaTime * 0.1f;
            this.curProgress += Time.deltaTime;
            if (this.curProgress > AWorkerTask.maxProgress)
            {
                this.curProgress = 0;
                worker.SetProgress(this.curProgress, false);
                if (this.IsFinishAllStage(worker))
                {
                    this.Finish(worker);
                    return true;
                }

                return false;
            }

            worker.SetProgress((float)this.curProgress / AWorkerTask.maxProgress, true);
            return false;
        }

        /// <summary>
        /// 执行
        /// </summary>
        public virtual void DoExecute()
        {
        }

        /// <summary>
        /// 选择到最近的任务之后执行
        /// </summary>
        /// <param name="worker">Worker</param>
        public virtual void Start(AWorker worker)
        {
            this.curProgress = 0.0f;
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
            WorkerTaskManager.Instance.CompleteTask(this);
        }

        /// <summary>
        /// Worker是否可以接该任务,具体实现
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected virtual bool DoIsCanWork(AWorker worker)
        {
            return false;
        }

        /// <summary>
        /// 是否真的完成，为多阶段任务服务（Carry）
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>是否</returns>
        protected virtual bool IsFinishAllStage(AWorker worker)
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
        }
    }
}
