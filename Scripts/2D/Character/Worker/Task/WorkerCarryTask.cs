namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 任务2阶段：取货，放货
    /// Carry在第二个阶段预留资源
    /// </summary>
    [Serializable]
    public class WorkerCarryTask : WorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerCarryTask()
            : base(WorkerTaskTypeEnum.Carry)
        {
            this.stageInit.Add((Worker worker) =>
            {
                WorkerTask.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 进入工作状态
                worker.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            });
            this.stageInit.Add((Worker worker) =>
            {
                WorkerTask.maxProgress = 1.0f;
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = InventoryManager.Instance.GetPosByPrePlace(worker);
                if (this.TargetMap == default)
                {
                    LogManager.Instance.Log("仓库没有位置了", LogManager.LogLevelEnum.Error);
                }

                // 进入工作状态
                worker.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            InventoryManager.Instance.IsEnoughAndPrePlace(worker, this.resourceInfo, true);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            base.Finish(worker);
            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(this.resourceInfo.Id);

            // 放下拿起来的东西
            ItemMap.Instance.AddTile(this.TargetMap, ResourceManager.Instance
                .GetAsset(ItemDataManager.Instance.GetById(this.resourceInfo.Id).EnName));
            worker.SubResource(this.resourceInfo);
            InventoryManager.Instance.AddItemByPrePlace(worker, this.TargetMap);

            // 如果是食物,添加饥饿任务
            if (itemType == AItem.ItemTypeEnum.Food)
            {
                WorkerTaskManager.Instance.AddTask(new WorkerHungryTask.HungryTaskBuilder().SetTarget(this.TargetMap).Build(), 0);
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(Worker worker)
        {
            return InventoryManager.Instance.IsEnoughAndPrePlace(worker, this.resourceInfo);
        }

        /// <inheritdoc/>
        protected override bool IsFinish(Worker worker)
        {
            switch (this.stage)
            {
                case 0:
                    ItemMap.Instance.PickUpFromDrop(this.TargetMap, this.resourceInfo);
                    worker.AddResource(this.resourceInfo);
                    this.ChangeStage(worker, 1);
                    return false;
                default:
                    return true;
            }
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// 建造者
        /// </summary>
        public class CarryTaskBuilder
        {
            private readonly WorkerCarryTask task;

            public CarryTaskBuilder()
            {
                this.task = new WorkerCarryTask();
            }

            public CarryTaskBuilder SetStartTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = targetMap;
                return this;
            }

            public CarryTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerCarryTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
