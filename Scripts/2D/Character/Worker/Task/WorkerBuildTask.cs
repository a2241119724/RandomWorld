namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 任务2阶段：拿材料，建造
    /// Build在第一个阶段预留资源
    /// </summary>
    public class WorkerBuildTask : WorkerTask
    {
        private Dictionary<int, ResourceInfo> needs;
        private Dictionary<int, ResourceInfo> temp;

        /// <summary>
        /// 建造的位置
        /// </summary>
        private Vector3Int buildPos;

        public WorkerBuildTask()
            : base(TaskType.Build)
        {
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 1.0f;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(neighbors[8]);
                this.TargetMap = InventoryManager.Instance.getPosByPreTake(worker);
                if (this.TargetMap == default)
                {
                    this.giveUpTask(worker);
                }

                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 2.0f;

                // 建造
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(neighbors[0]);
                this.AvailableNeighborPos.Add(neighbors[1]);
                this.AvailableNeighborPos.Add(neighbors[2]);
                this.AvailableNeighborPos.Add(neighbors[3]);
                this.TargetMap = this.buildPos;
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        /// <summary>
        /// 没用
        /// </summary>
        public BuildItem BuildItem { get; private set; }

        /// <inheritdoc/>
        public override void start(Worker worker)
        {
            // 自身携带资源足够
            if (worker.IsEnough(this.needs))
            {
                // LogManager.Instance.log("携带资源充足", LogManager.LogLevel.Info);
                this.changeStage(worker, 1);
                return;
            }

            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            InventoryManager.Instance.isEnoughAndPreTake(worker, remaining, true);

            // 不够就取资源
            this.changeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void finish(Worker worker)
        {
            base.finish(worker);

            // 减少worker携带的资源
            worker.SubResource(this.needs);

            // 将建造完成的Tile从Building变为Build中
            BuildMap.Instance.setComplete(this.buildPos);
        }

        /// <inheritdoc/>
        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }

            // 如果worker携带的资源已经满足建造
            if (worker.IsEnough(this.needs))
            {
                return true;
            }

            // 按照单个任务的资源取看是否足够
            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            return InventoryManager.Instance.isEnoughAndPreTake(worker, remaining);
        }

        /// <inheritdoc/>
        public override void giveUpTask(Worker worker)
        {
            base.giveUpTask(worker);

            // 恢复资源
            this.temp = Tool.DeepCopyByBinary(this.needs);
        }

        /// <inheritdoc/>
        protected override bool isFinish(Worker worker)
        {
            // 只worker携带的资源不够时,取建筑材料
            switch (this.stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryManager.Instance.subItemByPreTake(worker, this.TargetMap);
                    worker.AddResource(resourceInfo);

                    // 减少需求的数量
                    foreach (KeyValuePair<int, ResourceInfo> pair in this.temp)
                    {
                        if (pair.Key == resourceInfo.id)
                        {
                            pair.Value.count -= resourceInfo.count;
                            if (pair.Value.count <= 0)
                            {
                                this.temp.Remove(resourceInfo.id);
                            }

                            break;
                        }
                    }

                    // 获取完成所有的材料
                    if (this.temp.Count == 0)
                    {
                        this.changeStage(worker, 1);
                        return false;
                    }

                    this.changeStage(worker, 0);
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class BuildTaskBuilder
        {
            private WorkerBuildTask task;

#pragma warning disable SA1600 // Elements should be documented
            public BuildTaskBuilder()
            {
                this.task = new WorkerBuildTask();
            }

            public BuildTaskBuilder SetBuild(BuildItem buildItem)
            {
                this.task.BuildItem = buildItem;
                return this;
            }

            public BuildTaskBuilder SetBuildPos(Vector3Int pos)
            {
                this.task.buildPos = pos;
                return this;
            }

            public BuildTaskBuilder SetNeedResource(Dictionary<int, ResourceInfo> needResource)
            {
                this.task.temp = Tool.DeepCopyByBinary(needResource);
                this.task.needs = Tool.DeepCopyByBinary(needResource);
                return this;
            }

            public WorkerBuildTask Build()
            {
                return this.task;
            }
#pragma warning restore SA1600 // Elements should be documented
        }
    }
}
