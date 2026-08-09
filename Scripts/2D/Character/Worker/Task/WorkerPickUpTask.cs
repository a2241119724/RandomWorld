namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Item.Backpack.Equipment;
    using LAB2D.Item.Backpack.Equipment.Weapon;
    using LAB2D.Serializable;
    using LAB2D.Tool;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 通用拾取任务 — 单阶段任务：走到目标位置 → 拾取物品到背包。
    ///
    /// 两种模式：
    /// - FromBoard：走到任务栏邻居位置 → 从 TaskBoardManager 取回属于自己的悬赏物品
    /// - FromGround：走到掉落物位置 → 从地面捡起物品直接放入背包
    ///
    /// 此任务不是悬赏类型，不展示在任务栏中。
    /// </summary>
    [Serializable]
    public class WorkerPickUpTask : AWorkerTask
    {
        public enum PickUpMode
        {
            /// <summary>从任务栏取回物品（悬赏产出物）</summary>
            FromBoard,

            /// <summary>从地面捡起物品直接放入背包（自我搬运）</summary>
            FromGround,
        }

        private PickUpMode mode;
        private int targetOwnerId;

        /// <summary>FromGround 模式下要拾取的资源信息</summary>
        private ResourceInfo groundResource;

        /// <summary>FromGround 链式拾取：当前物品捡完后，剩余的待拾取位置</summary>
        private List<Vector3Int> pendingPositions;

        /// <summary>FromGround 链式拾取：当前物品捡完后，剩余的待拾取资源</summary>
        private List<ResourceInfo> pendingResources;

        public WorkerPickUpTask()
            : base(WorkerTaskType.PickUp)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = this.mode == PickUpMode.FromGround
                    ? WorkerTaskTimeConfig.CarryTakeSeconds
                    : 0.5f;
                this.Init();
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

            switch (this.mode)
            {
                case PickUpMode.FromBoard:
                    this.FinishFromBoard(worker);
                    break;

                case PickUpMode.FromGround:
                    this.FinishFromGround(worker);
                    break;
            }
        }

        /// <summary>
        /// FromBoard 模式完成：从任务栏取回属于自己的悬赏物品。
        /// </summary>
        private void FinishFromBoard(AWorker worker)
        {
            int workerId = worker.GetInstanceID();
            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();

            List<ResourceInfo> items = board.RetrieveItems(workerId);
            if (items.Count == 0)
            {
                LogProvider($"{worker.name} 任务栏中没有属于自己的物品", LogManager.LogLevelEnum.Warning);
                return;
            }

            int totalCount = 0;
            foreach (var ri in items)
            {
                worker.AddResource(ri);
                totalCount += ri.Count;
            }

            LogProvider(
                $"{worker.name} 从任务栏取回 {items.Count} 种物品，共 {totalCount} 个",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// FromGround 模式完成：从地面捡起物品直接放入背包。
        /// 如果还有待拾取物品，立即链式创建下一个拾取任务。
        /// </summary>
        private void FinishFromGround(AWorker worker)
        {
            Vector3Int posMap = Vector3IntLAB.ToVector3Int(this.TargetMap);

            // 从地面移除掉落物
            ItemMapProvider().PickUpFromDrop(posMap, this.groundResource);

            // 清除掉落物光束特效和待处理记录
            EquipmentBeamProvider().TryRemoveBeamAt(posMap);
            EnemyLootProvider().RemoveDropByMapPosition(posMap);

            // 装备/武器智能穿戴：对比身上穿的，哪个好穿哪个，没有直接穿
            bool isEquipped = TryEquipIfBetter(worker, this.groundResource, posMap);

            if (!isEquipped)
            {
                // 非装备或不如身上穿的：放入背包
                worker.AddResource(this.groundResource);
            }

            LogProvider(
                $"{worker.name} 从地面捡起物品(id={this.groundResource.Id}, count={this.groundResource.Count}) pos=({posMap.x},{posMap.y})",
                LogManager.LogLevelEnum.Debug);

            // 链式拾取：还有待拾取物品时，立即创建下一个拾取任务
            if (this.pendingPositions != null && this.pendingPositions.Count > 0)
            {
                Vector3Int nextPos = this.pendingPositions[0];
                ResourceInfo nextResource = this.pendingResources[0];
                this.pendingPositions.RemoveAt(0);
                this.pendingResources.RemoveAt(0);

                WorkerPickUpTask nextTask = new PickUpTaskBuilder()
                    .SetMode(PickUpMode.FromGround)
                    .SetTargetPosition(nextPos)
                    .SetGroundResource(nextResource)
                    .SetOwnerId(this.targetOwnerId)
                    .SetPendingPickups(this.pendingPositions, this.pendingResources)
                    .Build();

                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                workerData.Task = nextTask;
                nextTask.Start(worker);

                LogProvider(
                    $"{worker.name} 链式拾取下一个: id={nextResource.Id} pos=({nextPos.x},{nextPos.y}) 剩余{this.pendingPositions.Count}个",
                    LogManager.LogLevelEnum.Trace);
            }
        }

        /// <summary>
        /// 如果是装备/武器，对比身上穿的，哪个好穿哪个。没有直接穿。
        /// </summary>
        /// <param name="worker">执行拾取的 Worker</param>
        /// <param name="resource">拾取的资源信息</param>
        /// <param name="posMap">拾取位置（旧装备交换时放回此位置）</param>
        /// <returns>true 表示已装备处理（不需要再放入背包）</returns>
        private bool TryEquipIfBetter(AWorker worker, ResourceInfo resource, Vector3Int posMap)
        {
            AItem.ItemTypeEnum itemType = ItemTypeProvider(resource.Id);
            if (itemType != AItem.ItemTypeEnum.Equipment && itemType != AItem.ItemTypeEnum.Weapon)
                return false;

            ItemData itemData = ItemDataProvider(resource.Id);
            ABackpackItem item = ItemFactoryProvider(itemData.EnName);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            if (item is AWeapon newWeapon)
            {
                AWeapon currentWeapon = workerData.Weapon;
                if (currentWeapon == null)
                {
                    // 没有武器，直接装备
                    workerData.Weapon = newWeapon;
                    LogProvider(
                        $"{worker.name} 装备武器: {itemData.CnName}",
                        LogManager.LogLevelEnum.Debug);
                    return true;
                }

                if (EquipmentLootTool.CountUpgrades(currentWeapon.Attribute, newWeapon.Attribute) >= 5)
                {
                    // 新武器更好，替换。旧武器放回地面
                    workerData.Weapon = newWeapon;
                    DropEquipmentToGround(currentWeapon, posMap);
                    LogProvider(
                        $"{worker.name} 替换更好的武器: {itemData.CnName} → 旧武器放回地面",
                        LogManager.LogLevelEnum.Debug);
                    return true;
                }

                // 不如身上的武器，放入背包
                return false;
            }

            if (item is AEquipment newEquip)
            {
                var equipments = workerData.GetEquipments();
                if (!equipments.TryGetValue(newEquip.Type, out AEquipment currentEquip) || currentEquip == null)
                {
                    // 该槽位为空，直接装备
                    workerData.AddEquipment(newEquip, posMap);
                    LogProvider(
                        $"{worker.name} 装备{EquipmentLootTool.GetSlotName(newEquip.Type)}: {itemData.CnName}",
                        LogManager.LogLevelEnum.Debug);
                    return true;
                }

                if (EquipmentLootTool.CountUpgrades(currentEquip.Attribute, newEquip.Attribute) >= 5)
                {
                    // 新装备更好，替换（AddEquipment 内部将旧装备放回地图）
                    workerData.AddEquipment(newEquip, posMap);
                    LogProvider(
                        $"{worker.name} 替换更好的{EquipmentLootTool.GetSlotName(newEquip.Type)}: {itemData.CnName}",
                        LogManager.LogLevelEnum.Debug);
                    return true;
                }

                // 不如身上的装备，放入背包
                return false;
            }

            return false;
        }

        /// <summary>
        /// 将旧武器放回地面（武器通过 Weapon 属性管理，不经过 AddEquipment/EquipmentSwapDropProvider）。
        /// </summary>
        /// <param name="oldEquipment">旧武器</param>
        /// <param name="posMap">放置位置</param>
        private static void DropEquipmentToGround(AEquipment oldEquipment, Vector3Int posMap)
        {
            ItemData itemData = ItemDataProvider(oldEquipment.Id);
            TileBase tile = Core.ServiceLocator.Get<ResourceManager>().GetAsset(itemData.EnName);
            // 旧武器放回地面，设为无主（任何人可拾取）
            ItemMapProvider().PutDownToDrop(posMap, tile, new ResourceInfo(oldEquipment.Id, 1, ownerId: 0));
        }

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.targetOwnerId;

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // targetOwnerId == 0 表示公开任务（Player 击杀掉落），任何 Worker 可接取
            if (this.targetOwnerId != 0 && worker.GetInstanceID() != this.targetOwnerId) return false;

            switch (this.mode)
            {
                case PickUpMode.FromBoard:
                {
                    TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
                    return board != null && board.IsInitialized && board.HasDeliveredItems(this.targetOwnerId);
                }

                case PickUpMode.FromGround:
                    // 检查地面物品是否仍然存在
                    return this.groundResource != null && this.TargetMap != default;

                default:
                    return false;
            }
        }

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.LightWorkTiredCostPerSecond;

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]); // 自身位置
        }

        // ---- Builder ----

        public class PickUpTaskBuilder
        {
            private readonly WorkerPickUpTask task;

            public PickUpTaskBuilder()
            {
                this.task = new WorkerPickUpTask();
            }

            /// <summary>设置拾取模式（默认为 FromBoard）</summary>
            public PickUpTaskBuilder SetMode(PickUpMode mode)
            {
                this.task.mode = mode;
                return this;
            }

            /// <summary>设置任务栏邻居位置为目标（FromBoard 模式）</summary>
            public PickUpTaskBuilder SetBoardNeighbor(Vector3Int neighborPos)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(neighborPos);
                return this;
            }

            /// <summary>设置地面物品位置为目标（FromGround 模式）</summary>
            public PickUpTaskBuilder SetTargetPosition(Vector3Int targetPos)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetPos);
                return this;
            }

            /// <summary>设置目标物主 ID（只有该 Worker 可接）</summary>
            public PickUpTaskBuilder SetOwnerId(int ownerId)
            {
                this.task.targetOwnerId = ownerId;
                return this;
            }

            /// <summary>设置地面资源信息（FromGround 模式）</summary>
            public PickUpTaskBuilder SetGroundResource(ResourceInfo resourceInfo)
            {
                this.task.groundResource = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            /// <summary>设置链式拾取的待拾取列表（FromGround 模式）</summary>
            public PickUpTaskBuilder SetPendingPickups(
                List<Vector3Int> positions, List<ResourceInfo> resources)
            {
                this.task.pendingPositions = positions != null
                    ? new List<Vector3Int>(positions)
                    : null;
                this.task.pendingResources = resources != null
                    ? new List<ResourceInfo>(resources)
                    : null;
                return this;
            }

            public WorkerPickUpTask Build()
            {
                return this.task;
            }
        }
    }
}
