namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 任务2阶段：拿材料，建造
    /// Build在第一个阶段预留资源
    /// </summary>
    [Serializable]
    public class WorkerBuildTask : AWorkerTask
    {
        private Dictionary<int, ResourceInfo> needs;
        private Dictionary<int, ResourceInfo> temp;

        /// <summary>
        /// 建造的位置
        /// </summary>
        private Vector3IntLAB buildPos;

        public WorkerBuildTask()
            : base(WorkerTaskTypeEnum.Build)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.BuildFetchResourceSeconds;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryManager.Instance.GetPosByPreTake(worker));
                if (this.TargetMap == default)
                {
                    this.GiveUpTask(worker);
                }

                // 进入工作状态
            });
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.GetBuildConstructionSeconds(this.needs);

                // 建造
                this.Init();
            });
        }

        /// <summary>
        /// 没用
        /// </summary>
        public ABuildItem BuildItem { get; private set; }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            // 自身携带资源足够
            if (worker.IsEnough(this.needs))
            {
                // LogManager.Instance.log("携带资源充足", LogManager.LogLevel.Info);
                this.ChangeStage(worker, 1);
                return;
            }

            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            InventoryManager.Instance.IsEnoughAndPreTake(worker, remaining, true);

            // 不够就取资源
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);

            // 减少worker携带的资源
            worker.SubResource(this.needs);

            // 将建造完成的Tile从Building变为Build中
            BuildMap.Instance.SetComplete(this.buildPos);
        }

        /// <inheritdoc/>
        public override void GiveUpTask(AWorker worker)
        {
            base.GiveUpTask(worker);

            // 恢复资源
            this.temp = DataTool.DeepCopyByBinary(this.needs);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 如果worker携带的资源已经满足建造
            if (worker.IsEnough(this.needs))
            {
                return true;
            }

            // 按照单个任务的资源取看是否足够
            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            return InventoryManager.Instance.IsEnoughAndPreTake(worker, remaining);
        }

        /// <inheritdoc/>
        protected override bool StageChangeRule(AWorker worker)
        {
            // 只worker携带的资源不够时,取建筑材料
            switch (this.stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.SubItemByPreTake(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));
                    worker.AddResource(resourceInfo);

                    // 减少需求的数量
                    foreach (KeyValuePair<int, ResourceInfo> pair in this.temp)
                    {
                        if (pair.Key == resourceInfo.Id)
                        {
                            pair.Value.Count -= resourceInfo.Count;
                            if (pair.Value.Count <= 0)
                            {
                                this.temp.Remove(resourceInfo.Id);
                            }

                            break;
                        }
                    }

                    // 获取完成所有的材料
                    if (this.temp.Count == 0)
                    {
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
            this.AvailableNeighborPos.Add(Neighbors[0]);
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[2]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
            this.TargetMap = this.buildPos;
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class BuildTaskBuilder
        {
            private readonly WorkerBuildTask task;

            public BuildTaskBuilder()
            {
                this.task = new WorkerBuildTask();
            }

            public BuildTaskBuilder SetBuild(ABuildItem buildItem)
            {
                this.task.BuildItem = buildItem;
                return this;
            }

            public BuildTaskBuilder SetBuildPos(Vector3Int pos)
            {
                this.task.TargetMap = this.task.buildPos = Vector3IntLAB.ToVector3IntLAB(pos);
                return this;
            }

            public BuildTaskBuilder SetNeedResource(Dictionary<int, ResourceInfo> needResource)
            {
                this.task.temp = DataTool.DeepCopyByBinary(needResource);
                this.task.needs = DataTool.DeepCopyByBinary(needResource);
                return this;
            }

            public WorkerBuildTask Build()
            {
                return this.task;
            }
        }
    }
}
