namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
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

        /// <summary>批量搬运时多个待拾取位置（含首个位置，与 batchResources 一一对应）</summary>
        private List<Vector3Int> batchPickupPositions;

        /// <summary>批量搬运时多个待拾取资源（与 batchPickupPositions 一一对应）</summary>
        private List<ResourceInfo> batchResources;

        /// <summary>批量搬运时已拾取的资源汇总（用于 Finish 时统一交付）</summary>
        private List<ResourceInfo> carriedResources;

        /// <summary>预收集资源：物品已提前由 PickUpTask 链拾取到 Worker 背包，跳过地面拾取阶段，直接送货到 Board</summary>
        private List<ResourceInfo> preCollectedResources;

        /// <summary>是否启用批量搬运模式</summary>
        private bool IsBatchMode => this.batchPickupPositions != null && this.batchPickupPositions.Count > 1;

        /// <summary>是否启用预收集模式（物品已在 Worker 身上，无需从地面拾取）</summary>
        private bool IsPreCollected => this.preCollectedResources != null && this.preCollectedResources.Count > 0;

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
                // 先确定搬运目标位置并验证可达性，再捡起物品，避免捡了运不走
                Vector3Int destTarget;
                switch (this.mode)
                {
                    case CarryMode.ToBoard:
                        destTarget = this.GetBoardNeighbor();
                        break;

                    case CarryMode.ToInventory:
                    default:
                        destTarget = InventoryProvider().GetPosByPrePlace(worker);
                        if (destTarget == default)
                        {
                            LogProvider("仓库没有位置了，搬运取消", LogManager.LogLevelEnum.Error);
                            Core.ServiceLocator.Get<WorkerTaskManager>().RemoveTask(this);
                            worker.GiveUpTask();
                            return;
                        }

                        break;
                }

                // 验证目的地是否可达（在捡起物品前检查，防止捡了运不走）
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]); // 自身位置
                Vector3Int checkPos = new Vector3Int(
                    destTarget.x + Neighbors[8].Y,
                    destTarget.y + Neighbors[8].X, 0);
                if (!ASeek.IsCanReach(checkPos))
                {
                    LogProvider(
                        $"搬运目标不可达: dest=({destTarget.x},{destTarget.y}), 进入冷却期",
                        LogManager.LogLevelEnum.Error);

                    // 标记冷却而非永久删除：障碍物消失后其他 Worker 可能可达
                    this.LastFailedTime = UnityEngine.Time.time;
                    worker.GiveUpTask();
                    return;
                }

                // 计算交付阶段耗时（基于首个物品类型）
                ItemData firstItemData = this.IsPreCollected
                    ? ItemDataProvider(this.preCollectedResources[0].Id)
                    : this.batchResources != null && this.batchResources.Count > 0
                        ? ItemDataProvider(this.batchResources[0].Id)
                        : ItemDataProvider(this.resourceInfo.Id);
                this.maxProgress = WorkerTaskTimeConfig.ResolveCarryPutDownSeconds(firstItemData);

                // 预收集模式：物品已在 Worker 背包中，跳过地面拾取
                if (this.IsPreCollected)
                {
                    this.TargetMap = Vector3IntLAB.ToVector3IntLAB(destTarget);
                    return;
                }

                // 从地上捡起掉落物
                if (this.IsBatchMode)
                {
                    // 批量模式：拾取所有位置的物品
                    this.carriedResources = new List<ResourceInfo>();
                    for (int i = 0; i < this.batchPickupPositions.Count; i++)
                    {
                        Vector3Int pos = this.batchPickupPositions[i];
                        ResourceInfo ri = this.batchResources[i];
                        ItemMapProvider().PickUpFromDrop(pos, ri);
                        worker.AddResource(ri);

                        if (this.mode == CarryMode.ToInventory)
                        {
                            EquipmentBeamProvider().TryRemoveBeamAt(pos);
                            EnemyLootProvider().RemoveDropByMapPosition(pos);
                        }

                        this.carriedResources.Add(ri);
                    }

                    LogProvider(
                        $"{worker.name} 批量拾取 {this.carriedResources.Count} 个物品",
                        LogManager.LogLevelEnum.Debug);
                }
                else
                {
                    // 单物品模式：拾取一个物品
                    Vector3Int pickUpPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
                    ItemMapProvider().PickUpFromDrop(pickUpPos, this.resourceInfo);
                    worker.AddResource(this.resourceInfo);

                    this.carriedResources = new List<ResourceInfo> { this.resourceInfo };

                    // 模式特定的副作用处理
                    switch (this.mode)
                    {
                        case CarryMode.ToInventory:
                        default:
                            this.carriedBeamRarity = EquipmentBeamProvider().TryRemoveBeamAt(pickUpPos);
                            EnemyLootProvider().RemoveDropByMapPosition(pickUpPos);
                            break;
                    }
                }

                // 切换目标到已验证的目的地
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(destTarget);
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);

            // 重置批量搬运状态（任务可能因 GiveUp 后重新接取而需要清理）
            this.carriedResources = null;

            // 预收集模式：物品已通过 PickUpTask 链拾取到 Worker 背包，直接跳转到送货阶段
            if (this.IsPreCollected)
            {
                this.carriedResources = new List<ResourceInfo>(this.preCollectedResources);
                this.ChangeStage(worker, 1);
                return;
            }

            if (this.mode == CarryMode.ToInventory)
            {
                if (this.IsBatchMode)
                {
                    // 批量模式：为所有资源预占仓库空间
                    foreach (var ri in this.batchResources)
                    {
                        InventoryProvider().IsEnoughAndPrePlace(worker, ri, true);
                    }
                }
                else
                {
                    InventoryProvider().IsEnoughAndPrePlace(worker, this.resourceInfo, true);
                }
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
            int totalCount = 0;

            if (this.carriedResources != null && this.carriedResources.Count > 0)
            {
                // 批量/单物品统一交付
                int ownerId = this.carriedResources[0].OwnerId;
                foreach (var ri in this.carriedResources)
                {
                    board.DeliverItem(ownerId, ri);
                    worker.SubResource(ri);
                    totalCount += ri.Count;
                }

                LogProvider(
                    $"{worker.name} 已将 {this.carriedResources.Count} 种物品(共{totalCount}个, owner={ownerId}) 交付到任务栏",
                    LogManager.LogLevelEnum.Debug);
            }
            else
            {
                // 回退：单物品（无 carriedResources 时）
                int ownerId = this.resourceInfo.OwnerId;
                board.DeliverItem(ownerId, this.resourceInfo);
                worker.SubResource(this.resourceInfo);

                LogProvider(
                    $"{worker.name} 已将物品(id={this.resourceInfo.Id}, count={this.resourceInfo.Count}, owner={ownerId}) 交付到任务栏",
                    LogManager.LogLevelEnum.Debug);
            }
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

                    // 批量模式：验证所有拾取位置仍有掉落物（防止物品已被其他 Worker 捡走）
                    if (this.IsBatchMode)
                    {
                        for (int i = 0; i < this.batchPickupPositions.Count; i++)
                        {
                            if (Core.ServiceLocator.Get<DropManager>().GetDropByAll(
                                this.batchPickupPositions[i]) == null)
                            {
                                LogProvider(
                                    $"批量搬运位置 {this.batchPickupPositions[i]} 已无掉落物，任务取消",
                                    LogManager.LogLevelEnum.Debug);
                                return false;
                            }
                        }
                    }

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

            /// <summary>
            /// 设置预收集资源列表（预收集模式）。
            /// 物品已通过 PickUpTask 链拾取到 Worker 背包，CarryTask 跳过地面拾取直接送货到 Board。
            /// </summary>
            public CarryTaskBuilder SetPreCollectedResources(List<ResourceInfo> resources)
            {
                this.task.preCollectedResources = resources != null
                    ? new List<ResourceInfo>(resources)
                    : null;
                return this;
            }

            /// <summary>
            /// 设置批量搬运的资源列表和对应取货位置（批量模式）。
            /// 调用此方法后，第一个位置自动成为任务的初始 TargetMap。
            /// positions 和 resources 必须一一对应且长度一致。
            /// </summary>
            public CarryTaskBuilder SetBatchResources(
                List<Vector3Int> positions, List<ResourceInfo> resources)
            {
                if (positions == null || resources == null
                    || positions.Count == 0 || resources.Count == 0
                    || positions.Count != resources.Count)
                {
                    AWorkerTask.LogProvider(
                        "SetBatchResources: positions and resources must be non-empty and equal length",
                        LogManager.LogLevelEnum.Error);
                    return this;
                }

                this.task.batchPickupPositions = new List<Vector3Int>(positions);
                this.task.batchResources = new List<ResourceInfo>(resources.Count);
                foreach (var r in resources)
                {
                    this.task.batchResources.Add(DataTool.DeepCopyByBinary(r));
                }

                // 第一个位置作为初始 TargetMap
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(positions[0]);

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
