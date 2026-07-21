namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;
    using UnityEngine.Tilemaps;

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

        /// <summary>
        /// 搬运时暂存的光束稀有度（取货时从原位置移除光束并记录，放货时在新位置重新生成）
        /// </summary>
        private EquipmentRarityType? carriedBeamRarity;

        public WorkerCarryTask()
            : base(WorkerTaskType.Carry)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                ItemData itemData = ItemDataProvider(this.resourceInfo.Id);
                this.maxProgress = WorkerTaskTimeConfig.ResolveCarryTakeSeconds(itemData);
                this.Init();
            });
            this.stageInit.Add((AWorker worker) =>
            {
                ItemData itemData = ItemDataProvider(this.resourceInfo.Id);
                this.maxProgress = WorkerTaskTimeConfig.ResolveCarryPutDownSeconds(itemData);
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 取货
                Vector3Int pickUpPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
                ItemMapProvider().PickUpFromDrop(pickUpPos, this.resourceInfo);
                worker.AddResource(this.resourceInfo);

                // 移除品质光束并记录稀有度（用于放下时重新生成）
                this.carriedBeamRarity = EquipmentBeamProvider().TryRemoveBeamAt(pickUpPos);
                // 同时清理待处理掉落记录（敌人装备掉落）
                EnemyLootProvider().RemoveDropByMapPosition(pickUpPos);

                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryProvider().GetPosByPrePlace(worker));
                if (this.TargetMap == default)
                {
                    LogProvider("仓库没有位置了", LogManager.LogLevelEnum.Error);
                }
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            InventoryProvider().IsEnoughAndPrePlace(worker, this.resourceInfo, true);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);
            AItem.ItemTypeEnum itemType = ItemTypeProvider(this.resourceInfo.Id);

            Vector3Int targetPos = Vector3IntLAB.ToVector3Int(this.TargetMap);

            // 放下拿起来的东西
            ItemMapProvider().AddTile(targetPos, (TileBase)ResourceLoadProvider(ItemDataProvider(this.resourceInfo.Id).EnName));
            worker.SubResource(this.resourceInfo);
            InventoryProvider().AddItemByPrePlace(worker, targetPos);

            // 如果搬运前有品质光束，在新位置重新生成光束
            if (this.carriedBeamRarity.HasValue)
            {
                Vector3 beamWorldPos = TileMapPositionProvider(targetPos);
                EquipmentBeamProvider().SpawnBeam(targetPos, beamWorldPos, this.carriedBeamRarity.Value);
            }
            else
            {
                // 兼容旧逻辑：尝试从 pendingDrops 获取光束信息（非主要路径）
                EnemyLootProvider().TrySpawnBeamForInventory(targetPos, this.resourceInfo.Id);
            }

            // 如果是食物,添加饥饿任务
            if (itemType == AItem.ItemTypeEnum.Food)
            {
                TaskAddProvider(
                    new WorkerHungryTask.HungryTaskBuilder()
                    .SetTarget(targetPos).Build(), this.TargetMap,
                    0);
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return InventoryProvider().IsEnoughAndPrePlace(worker, this.resourceInfo);
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
