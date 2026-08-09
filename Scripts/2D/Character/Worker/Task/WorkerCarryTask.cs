namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Constant;
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 搬运任务 — 两阶段：取货 → 放货。
    ///
    /// CarryMode.ToInventory (默认): 从地面取货 → 搬运到仓库/库存，放下时在地面创建图标
    /// CarryMode.ToBoard: 从地面取货 → 搬运到任务栏，物品存入 TaskBoardManager 内部存储
    ///
    /// ToBoard 模式下物品不在地面创建图标，发布者通过 WorkerPickUpTask (FromBoard) 取回。
    /// </summary>
    [Serializable]
    public class WorkerCarryTask : AWorkerTask
    {
        /// <summary>搬运模式</summary>
        public enum CarryMode
        {
            /// <summary>搬运到仓库/库存（默认）</summary>
            ToInventory,

            /// <summary>搬运到任务栏</summary>
            ToBoard,
        }

        /// <summary>当前搬运模式</summary>
        private CarryMode mode;

        /// <summary>Worker 携带的资源</summary>
        private ResourceInfo resourceInfo;

        /// <summary>搬运时暂存的光束稀有度（ToInventory 模式：取货时移除光束并记录，放货时重新生成）</summary>
        private EquipmentRarityType? carriedBeamRarity;

        /// <summary>指定执行搬运的 Worker ID（ToBoard 模式使用，0=任何人可接）</summary>
        private int targetWorkerId;

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

                // 从地上捡起掉落物（两种模式共有逻辑）
                Vector3Int pickUpPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
                ItemMapProvider().PickUpFromDrop(pickUpPos, this.resourceInfo);
                worker.AddResource(this.resourceInfo);

                switch (this.mode)
                {
                    case CarryMode.ToBoard:
                        // 目标切换为任务栏四周的相邻位置
                        Vector3Int neighbor = this.GetBoardNeighbor();
                        this.TargetMap = Vector3IntLAB.ToVector3IntLAB(neighbor);
                        this.AvailableNeighborPos.Clear();
                        this.AvailableNeighborPos.Add(Neighbors[8]); // 自身
                        break;

                    case CarryMode.ToInventory:
                    default:
                        // 移除品质光束并记录稀有度（用于放下时重新生成）
                        this.carriedBeamRarity = EquipmentBeamProvider().TryRemoveBeamAt(pickUpPos);
                        // 清理待处理掉落记录（敌人装备掉落）
                        EnemyLootProvider().RemoveDropByMapPosition(pickUpPos);

                        // 目标切换为仓库预留位置
                        this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryProvider().GetPosByPrePlace(worker));
                        if (this.TargetMap == default)
                        {
                            LogProvider("仓库没有位置了", LogManager.LogLevelEnum.Error);
                        }

                        this.AvailableNeighborPos.Clear();
                        this.AvailableNeighborPos.Add(Neighbors[8]);
                        break;
                }
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);

            if (this.mode == CarryMode.ToInventory)
            {
                InventoryProvider().IsEnoughAndPrePlace(worker, this.resourceInfo, true);
            }

            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);

            switch (this.mode)
            {
                case CarryMode.ToBoard:
                    this.FinishToBoard(worker);
                    break;

                case CarryMode.ToInventory:
                default:
                    this.FinishToInventory(worker);
                    break;
            }
        }

        /// <summary>ToInventory 模式完成：将物品放入仓库并在新位置创建地面图标。</summary>
        private void FinishToInventory(AWorker worker)
        {
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

            // 如果是食物，添加饥饿任务
            if (itemType == AItem.ItemTypeEnum.Food)
            {
                TaskAddProvider(
                    new WorkerHungryTask.HungryTaskBuilder()
                    .SetTarget(targetPos).Build(), new GameGridPosition(this.TargetMap.X, this.TargetMap.Y, this.TargetMap.Z),
                    WorkerTaskPriority.PlayerBounty);
            }
        }

        /// <summary>ToBoard 模式完成：将物品交付到任务栏内部存储（不创建地面图标）。</summary>
        private void FinishToBoard(AWorker worker)
        {
            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            int ownerId = this.resourceInfo.OwnerId;
            board.DeliverItem(ownerId, this.resourceInfo);

            worker.SubResource(this.resourceInfo);

            LogProvider(
                $"{worker.name} 已将物品(id={this.resourceInfo.Id}, count={this.resourceInfo.Count}, owner={ownerId}) 交付到任务栏",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            switch (this.mode)
            {
                case CarryMode.ToBoard:
                    // 只有指定的执行者才能接此搬运任务（targetWorkerId=0 时允许任何人）
                    if (this.targetWorkerId != 0 && worker.GetInstanceID() != this.targetWorkerId)
                        return false;
                    return true;

                case CarryMode.ToInventory:
                default:
                    return InventoryProvider().IsEnoughAndPrePlace(worker, this.resourceInfo);
            }
        }

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.mode == CarryMode.ToBoard ? this.targetWorkerId : 0;

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.LightWorkTiredCostPerSecond;

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

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        /// <summary>
        /// 获取任务栏四周第一个可到达的相邻位置。
        /// </summary>
        private Vector3Int GetBoardNeighbor()
        {
            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            if (board != null && board.IsInitialized)
            {
                return board.GetNeighborPosition();
            }

            // 回退：使用当前目标位置
            return Vector3IntLAB.ToVector3Int(this.TargetMap);
        }

        // ---- Builder ----

        /// <summary>
        /// 搬运任务建造者。默认模式为 ToInventory。
        /// </summary>
        public class CarryTaskBuilder
        {
            private readonly WorkerCarryTask task;

            public CarryTaskBuilder()
            {
                this.task = new WorkerCarryTask();
            }

            /// <summary>设置搬运模式（默认为 ToInventory）</summary>
            public CarryTaskBuilder SetMode(CarryMode mode)
            {
                this.task.mode = mode;
                return this;
            }

            /// <summary>设置起始目标位置（取货点）</summary>
            public CarryTaskBuilder SetStartTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            /// <summary>设置要搬运的资源信息</summary>
            public CarryTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            /// <summary>设置执行搬运的 Worker ID（ToBoard 模式使用，0=任何人可接）</summary>
            public CarryTaskBuilder SetExecutor(int workerId)
            {
                this.task.targetWorkerId = workerId;
                return this;
            }

            /// <summary>构建搬运任务</summary>
            public WorkerCarryTask Build()
            {
                return this.task;
            }
        }
    }
}
