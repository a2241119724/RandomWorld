namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 任务2阶段：取货，放货
    /// Carry在第二个阶段预留资源
    /// </summary>
    public class WorkerCarryTask : WorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerCarryTask() : base(TaskType.Carry)
        {
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
            stageInit.Add((Worker worker) =>
            {
                maxProgress = 1.0f;
                AvailableNeighborPos.Clear();
                AvailableNeighborPos.Add(neighbors[8]);
                TargetMap = InventoryManager.Instance.GetPosByPrePlace(worker);
                if (TargetMap == default)
                {
                    LogManager.Instance.Log("仓库没有位置了", LogManager.LogLevel.Error);
                }
                // 进入工作状态
                worker.Manager.changeState(WorkerStateType.Seek);
            });
        }

        public override void start(Worker worker)
        {
            base.start(worker);
            InventoryManager.Instance.IsEnoughAndPrePlace(worker, resourceInfo, true);
            changeStage(worker, 0);
        }

        protected override bool isFinish(Worker worker)
        {
            switch (stage)
            {
                case 0:
                    ItemMap.Instance.PickUpFromDrop(TargetMap, resourceInfo);
                    worker.AddResource(resourceInfo);
                    changeStage(worker, 1);
                    return false;
                default:
                    return true;
            }
        }

        public override void finish(Worker worker)
        {
            base.finish(worker);
            ItemType itemType = ItemDataManager.Instance.GetTypeById(resourceInfo.Id);
            // 放下拿起来的东西
            ItemMap.Instance.ShowTile(TargetMap, (TileBase)ResourcesManager.Instance
                .GetAsset(ItemDataManager.Instance.GetById(resourceInfo.Id).ImageName));
            worker.SubResource1(resourceInfo);
            InventoryManager.Instance.AddItemByPrePlace(worker, TargetMap);
            // 如果是食物,添加饥饿任务
            if (itemType == ItemType.Food)
            {
                WorkerTaskManager.Instance.AddTask(new WorkerHungryTask.HungryTaskBuilder().setTarget(TargetMap).build(), 0);
            }
        }

        public override bool isCanWork(Worker worker)
        {
            if (!base.isCanWork(worker))
            {
                return false;
            }
            return InventoryManager.Instance.IsEnoughAndPrePlace(worker, resourceInfo);
        }

        public class CarryTaskBuilder
        {
            private WorkerCarryTask task;

            public CarryTaskBuilder()
            {
                task = new WorkerCarryTask();
            }

            public CarryTaskBuilder setStartTarget(Vector3Int targetMap)
            {
                task.TargetMap = targetMap;
                return this;
            }

            public CarryTaskBuilder setResourceInfo(ResourceInfo resourceInfo)
            {
                task.resourceInfo = Tool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerCarryTask build()
            {
                return task;
            }
        }
    }
}

