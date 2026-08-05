namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 搬运到任务栏 — 两阶段任务：取货 → 放到任务栏旁。
    ///
    /// 悬赏任务完成后，掉落物需要搬运到任务栏处交付给发布者。
    /// 与 WorkerCarryTask 的区别：Stage 1 目标不是仓库而是任务栏空地，
    /// 物品放到地上（保留 OwnerId），而非入库。
    ///
    /// 任何空闲 Worker 都可以接此任务（公共服务）。
    /// </summary>
    [Serializable]
    public class WorkerCarryToBoardTask : AWorkerTask
    {
        /// <summary>要搬运的资源</summary>
        private ResourceInfo resourceInfo;

        /// <summary>任务栏交付目标位置</summary>
        private Vector3Int boardDeliveryPos;

        public WorkerCarryToBoardTask()
            : base(WorkerTaskType.CarryToBoard)
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

                // 从地上捡起掉落物
                Vector3Int pickUpPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
                ItemMapProvider().PickUpFromDrop(pickUpPos, this.resourceInfo);
                worker.AddResource(this.resourceInfo);

                // 设置交付目标：任务栏旁空地
                this.boardDeliveryPos = this.GetOrFindDeliveryPosition();
                if (this.boardDeliveryPos == default)
                {
                    LogProvider("任务栏周围没有可用空地放置物品", LogManager.LogLevelEnum.Error);
                }

                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(this.boardDeliveryPos);
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

            if (this.boardDeliveryPos == default)
            {
                // 退而求其次：放回原位置
                this.boardDeliveryPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
            }

            // 将物品放到任务栏旁的地上（保留 OwnerId）
            // 注意：不能使用 TryMergeOrPlaceDrop，因为它会调用 PutDownToDrop 再创建一个 CarryTask
            ABackpackItem item = ItemFactoryByIdProvider(this.resourceInfo.Id);
            if (item != null)
            {
                UnityEngine.Tilemaps.TileBase tile =
                    (UnityEngine.Tilemaps.TileBase)ResourceLoadProvider(item.Tile.name);
                ItemMapProvider().AddTile(this.boardDeliveryPos, tile);
                AItem.ItemTypeEnum itemType = ItemTypeProvider(this.resourceInfo.Id);
                Core.ServiceLocator.Get<DropManager>().AddDrop(itemType, this.boardDeliveryPos, this.resourceInfo);
            }

            worker.SubResource(this.resourceInfo);

            LogProvider(
                $"{worker.name} 已将物品(id={this.resourceInfo.Id}, count={this.resourceInfo.Count}) 搬运到任务栏 ({this.boardDeliveryPos.x},{this.boardDeliveryPos.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 任何空闲 Worker 都可以搬运到任务栏（公共服务）
            return true;
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

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        /// <summary>
        /// 获取或搜索任务栏旁的空地作为交付位置。
        /// 优先搜索任务栏周围空地，失败则退回到任务栏位置本身（BuildMap Bounty tile 可通行）。
        /// </summary>
        private Vector3Int GetOrFindDeliveryPosition()
        {
            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            if (board != null && board.IsInitialized)
            {
                // 优先在任务栏周围找空地
                Vector3Int pos = board.GetDeliveryPosition();
                if (pos != default)
                {
                    return pos;
                }

                // 退而求其次：直接放到任务栏位置（Bounty tile 可通行，ItemMap 可叠加放置）
                LogProvider($"任务栏周围无空地，将物品直接放到任务栏位置 ({board.BoardPosition.x},{board.BoardPosition.y})",
                    LogManager.LogLevelEnum.Warning);
                return board.BoardPosition;
            }

            // TaskBoard 未初始化：在当前位置附近找空地
            Vector3Int currentPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
            return AWorkerTask.AvailablePositionProvider(currentPos, 10, false);
        }

        // ---- Builder ----

        public class CarryToBoardTaskBuilder
        {
            private readonly WorkerCarryToBoardTask task;

            public CarryToBoardTaskBuilder()
            {
                this.task = new WorkerCarryToBoardTask();
            }

            /// <summary>设置取货位置（掉落物所在位置）</summary>
            public CarryToBoardTaskBuilder SetStartTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            /// <summary>设置要搬运的资源信息</summary>
            public CarryToBoardTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerCarryToBoardTask Build()
            {
                return this.task;
            }
        }
    }
}
