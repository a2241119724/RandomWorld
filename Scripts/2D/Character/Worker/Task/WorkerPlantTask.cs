using System;

namespace LAB2D
{
    /// <summary>
    /// 种植任务
    /// </summary>
    [Serializable]
    public class WorkerPlantTask : AWorkerTask
    {
        private ResourceInfo resourceInfo;

        public WorkerPlantTask()
            : base(WorkerTaskTypeEnum.Plant)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                AWorkerTask.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryManager.Instance.IsContainSeedAndPreTake(worker, true));
                if (this.TargetMap == default)
                {
                    this.GiveUpTask(worker);
                    return;
                }

                // 进入工作状态
                worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
            });
            this.stageInit.Add((AWorker worker) =>
            {
                AWorkerTask.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(FarmlandManager.Instance.IsEnoughAndPrePlant(worker, this.resourceInfo, true));
                if (this.TargetMap == default)
                {
                    this.GiveUpTask(worker);
                    return;
                }

                // 进入工作状态
                worker.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);

            // TODO 可以将种子放回
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return FarmlandManager.Instance.IsEnoughAndPrePlant(worker, null) != default &&
                InventoryManager.Instance.IsContainSeedAndPreTake(worker) != default;
        }

        /// <inheritdoc/>
        protected override bool IsFinishAllStage(AWorker worker)
        {
            switch (this.stage)
            {
                case 0:
                    this.resourceInfo = InventoryManager.Instance.SubAllItemByPos(Vector3IntLAB.ToVector3Int(this.TargetMap));
                    worker.AddResource(this.resourceInfo);
                    this.ChangeStage(worker, 1);
                    return false;
                case 1:
                    // 可以继续种植
                    if (this.IsCanWork(worker) && this.resourceInfo.Count > 0)
                    {
                        FarmlandManager.Instance.PlantByPrePlant(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));
                        this.resourceInfo.Count--;
                        this.ChangeStage(worker, 1);
                        return false;
                    }

                    this.ChangeStage(worker, 0);
                    return false;
                default:
                    return true;
            }
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// 建造者
        /// </summary>
        public class PlantTaskBuilder
        {
            private readonly WorkerPlantTask task;

            public PlantTaskBuilder()
            {
                this.task = new WorkerPlantTask();
            }

            public WorkerPlantTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
