namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 任务2阶段：取货，放货
    /// Carry在第二个阶段预留资源
    /// </summary>
    [Serializable]
    public class WorkerCarryTask : AWorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerCarryTask()
            : base(WorkerTaskTypeEnum.Carry)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                ItemData itemData = ItemDataManager.Instance.GetById(this.resourceInfo.Id);
                this.maxProgress = WorkerTaskTimeConfig.ResolveCarryTakeSeconds(itemData);
                this.Init();
            });
            this.stageInit.Add((AWorker worker) =>
            {
                ItemData itemData = ItemDataManager.Instance.GetById(this.resourceInfo.Id);
                this.maxProgress = WorkerTaskTimeConfig.ResolveCarryPutDownSeconds(itemData);
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 取货
                ItemMap.Instance.PickUpFromDrop(Vector3IntLAB.ToVector3Int(this.TargetMap), this.resourceInfo);
                worker.AddResource(this.resourceInfo);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryManager.Instance.GetPosByPrePlace(worker));
                if (this.TargetMap == default)
                {
                    LogManager.Instance.Log("仓库没有位置了", LogManager.LogLevelEnum.Error);
                }
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            InventoryManager.Instance.IsEnoughAndPrePlace(worker, this.resourceInfo, true);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);
            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(this.resourceInfo.Id);

            // 放下拿起来的东西
            ItemMap.Instance.AddTile(Vector3IntLAB.ToVector3Int(this.TargetMap), ResourceManager.Instance
                .GetAsset(ItemDataManager.Instance.GetById(this.resourceInfo.Id).EnName));
            worker.SubResource(this.resourceInfo);
            InventoryManager.Instance.AddItemByPrePlace(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));

            // 如果是食物,添加饥饿任务
            if (itemType == AItem.ItemTypeEnum.Food)
            {
                WorkerTaskManager.Instance.AddTask(
                    new WorkerHungryTask.HungryTaskBuilder()
                    .SetTarget(Vector3IntLAB.ToVector3Int(this.TargetMap)).Build(), this.TargetMap,
                    0);
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return InventoryManager.Instance.IsEnoughAndPrePlace(worker, this.resourceInfo);
        }

        /// <inheritdoc/>
        protected override bool StageChangeRule(AWorker worker)
        {
            switch (this.stage)
            {
                case 0:
                    this.ChangeStage(worker, 1);
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
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
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
    }
}
