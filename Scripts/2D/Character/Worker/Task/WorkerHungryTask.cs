namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 先预取资源
    /// </summary>
    [Serializable]
    public class WorkerHungryTask : AWorkerTask
    {
        public WorkerHungryTask()
            : base(WorkerTaskType.Eat)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.EatSeconds;
                this.Init();
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (!InventoryManager.Instance.IsFoodAvailableAndPreTake(
                worker,
                Vector3IntLAB.ToVector3Int(this.TargetMap),
                workerData.MaxHungry - workerData.CurHungry,
                true))
            {
                this.GiveUpTask(worker);
                return;
            }

            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            // 再取食物，并且有可能会由于该位置的食物被取完，从而删除该饥饿任务
            ResourceInfo resourceInfo = InventoryManager.Instance.SubItemByPreTake(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));
            if (resourceInfo == null)
            {
                base.Finish(worker);
                return;
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.CurHungry = Mathf.Min(workerData.MaxHungry, workerData.CurHungry + (resourceInfo.Count * 10));

            // 将饥饿任务放回任务管理中
            base.Finish(worker);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 饥饿值小于一定值可以接收饥饿任务
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return workerData.CurHungry <= AWorker.ThresholdHungry
                && InventoryManager.Instance.IsFoodAvailableAndPreTake(
                    worker,
                    Vector3IntLAB.ToVector3Int(this.TargetMap),
                    workerData.MaxHungry - workerData.CurHungry);
        }

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class HungryTaskBuilder
        {
            private readonly WorkerHungryTask task;

            public HungryTaskBuilder()
            {
                this.task = new WorkerHungryTask();
            }

            public HungryTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            public WorkerHungryTask Build()
            {
                return this.task;
            }
        }
    }
}
