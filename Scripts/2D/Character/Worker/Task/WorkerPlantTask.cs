namespace LAB2D
{
    /// <summary>
    /// 种植任务
    /// </summary>
    public class WorkerPlantTask : WorkerTask
    {
        private ResourceInfo resourceInfo;

        public WorkerPlantTask()
            : base(TaskType.Plant)
        {
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = InventoryManager.Instance.IsContainSeedAndPreTake(worker, true);
                if (this.TargetMap == default)
                {
                    this.GiveUpTask(worker);
                    return;
                }

                // 进入工作状态
                worker.Manager.ChangeState(WorkerStateType.Seek);
            });
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = FarmlandManager.Instance.IsEnoughAndPrePlant(worker, this.resourceInfo, true);
                if (this.TargetMap == default)
                {
                    this.GiveUpTask(worker);
                    return;
                }

                // 进入工作状态
                worker.Manager.ChangeState(WorkerStateType.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            base.Finish(worker);

            // TODO 可以将种子放回
        }

        /// <inheritdoc/>
        public override bool IsCanWork(Worker worker)
        {
            if (!base.IsCanWork(worker))
            {
                return false;
            }

            return FarmlandManager.Instance.IsEnoughAndPrePlant(worker, null) != default &&
                InventoryManager.Instance.IsContainSeedAndPreTake(worker) != default;
        }

        /// <inheritdoc/>
        protected override bool IsFinish(Worker worker)
        {
            switch (this.stage)
            {
                case 0:
                    this.resourceInfo = InventoryManager.Instance.SubAllItemByPos(this.TargetMap);
                    worker.AddResource(this.resourceInfo);
                    this.ChangeStage(worker, 1);
                    return false;
                case 1:
                    // 可以继续种植
                    if (this.IsCanWork(worker) && this.resourceInfo.Count > 0)
                    {
                        FarmlandManager.Instance.PlantByPrePlant(worker, this.TargetMap);
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
