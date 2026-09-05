namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;

    /// <summary>
    /// 种植任务
    /// </summary>
    [Serializable]
    public class WorkerPlantTask : AWorkerTask
    {
        private ResourceInfo resourceInfo;

        public WorkerPlantTask()
            : base(WorkerTaskType.Plant)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.PlantFetchSeedSeconds;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryProvider().IsContainSeedAndPreTake(worker, true));
                if (this.TargetMap == default)
                {
                    LogProvider(
                        $"[TaskDiag] {worker.name} 种植任务失败: 仓库无种子可取",
                        LogManager.LogLevelEnum.Debug);
                    this.GiveUpTask(worker);
                    return;
                }
            });
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.PlantOneSeedSeconds;
                this.Init();
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(FarmlandManagerProvider().IsEnoughAndPrePlant(worker, this.resourceInfo, true));
                if (this.TargetMap == default)
                {
                    LogProvider(
                        $"[TaskDiag] {worker.name} 种植任务失败: 无可用耕地",
                        LogManager.LogLevelEnum.Debug);
                    this.GiveUpTask(worker);
                    return;
                }
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
            return FarmlandManagerProvider().IsEnoughAndPrePlant(worker, null) != default &&
                InventoryProvider().IsContainSeedAndPreTake(worker) != default;
        }

        /// <summary>
        /// 种植任务的生活技能：农耕。
        /// </summary>
        public override LAB2D.Enum.LifeSkillType? GrantedLifeSkill => LAB2D.Enum.LifeSkillType.Farming;

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.LightWorkTiredCostPerSecond;

        /// <inheritdoc/>
        protected override bool StageChangeRule(AWorker worker)
        {
            switch (this.stage)
            {
                case 0:
                    this.resourceInfo = InventoryProvider().SubAllItemByPos(Vector3IntLAB.ToVector3Int(this.TargetMap));
                    worker.AddResource(this.resourceInfo);
                    this.ChangeStage(worker, 1);
                    return false;
                case 1:
                    // 可以继续种植
                    if (this.IsCanWork(worker) && this.resourceInfo.Count > 0)
                    {
                        FarmlandManagerProvider().PlantByPrePlant(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));
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

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        /// <summary>
        /// 建造者 — 已接线：WorkerDecisionService.CreateSelfPlantTask（自主决策路径）经此构造。
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
    }
}
