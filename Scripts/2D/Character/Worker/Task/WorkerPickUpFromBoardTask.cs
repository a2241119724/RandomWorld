namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 从任务栏拾取 — 单阶段任务：走到任务栏 → 扫描捡起属于自己的物品。
    ///
    /// 悬赏发布者使用此任务取回任务栏处属于自己的掉落物。
    /// 此任务不是悬赏类型，不展示在任务栏中。
    ///
    /// 关键设计：DoIsCanWork 检查任务栏周围是否有属于该 Worker 的掉落物，
    /// 只有物主本人才能接此任务。
    /// </summary>
    [Serializable]
    public class WorkerPickUpFromBoardTask : AWorkerTask
    {
        private ResourceInfo resourceInfo;

        /// <summary>此任务的 OwnerId（只有该 Worker 可接）</summary>
        private int targetOwnerId;

        public WorkerPickUpFromBoardTask()
            : base(WorkerTaskType.PickUpFromBoard)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = 0.5f; // 快速拾取
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

            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            if (board == null || !board.IsInitialized)
            {
                LogProvider("任务栏未初始化，无法拾取", LogManager.LogLevelEnum.Error);
                return;
            }

            int workerId = worker.GetInstanceID();
            List<ResourceInfo> pickedItems = new List<ResourceInfo>();

            // 扫描任务栏周围，捡起所有属于自己的物品
            for (int dx = -5; dx <= 5; dx++)
            {
                for (int dy = -5; dy <= 5; dy++)
                {
                    Vector3Int pos = new Vector3Int(
                        board.BoardPosition.x + dx,
                        board.BoardPosition.y + dy,
                        0);

                    DropManager dropManager = Core.ServiceLocator.Get<DropManager>();
                    ResourceInfo drop = dropManager?.GetDropByAll(pos);
                    if (drop == null || drop.Count <= 0 || drop.OwnerId != workerId)
                    {
                        continue;
                    }

                    // 捡起
                    ItemMapProvider().PickUpFromDrop(pos, drop);
                    worker.AddResource(drop);
                    pickedItems.Add(drop);

                    LogProvider(
                        $"{worker.name} 从任务栏捡起物品(id={drop.Id}, count={drop.Count}, pos=({pos.x},{pos.y}))",
                        LogManager.LogLevelEnum.Info);
                }
            }

            if (pickedItems.Count == 0)
            {
                LogProvider(
                    $"{worker.name} 在任务栏周围未找到属于自己的物品",
                    LogManager.LogLevelEnum.Warning);
            }
            else
            {
                int totalCount = 0;
                foreach (var item in pickedItems) totalCount += item.Count;
                LogProvider(
                    $"{worker.name} 从任务栏拾取了 {pickedItems.Count} 种物品，共 {totalCount} 个",
                    LogManager.LogLevelEnum.Info);
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 只有物主本人才能接此任务
            if (worker.GetInstanceID() != this.targetOwnerId)
            {
                return false;
            }

            // 检查任务栏周围确实有属于自己的物品
            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            if (board == null || !board.IsInitialized)
            {
                return false;
            }

            return board.HasOwnedItemsNearBoard(this.targetOwnerId);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        // ---- Builder ----

        public class PickUpFromBoardTaskBuilder
        {
            private readonly WorkerPickUpFromBoardTask task;

            public PickUpFromBoardTaskBuilder()
            {
                this.task = new WorkerPickUpFromBoardTask();
            }

            /// <summary>设置任务栏位置为目标</summary>
            public PickUpFromBoardTaskBuilder SetBoardPosition(Vector3Int boardPos)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(boardPos);
                return this;
            }

            /// <summary>设置目标物主 ID（只有该 Worker 可接）</summary>
            public PickUpFromBoardTaskBuilder SetOwnerId(int ownerId)
            {
                this.task.targetOwnerId = ownerId;
                return this;
            }

            public WorkerPickUpFromBoardTask Build()
            {
                if (this.task.TargetMap == default)
                {
                    // 从 TaskBoardManager 获取位置
                    TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
                    if (board != null && board.IsInitialized)
                    {
                        this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(board.BoardPosition);
                    }
                }

                return this.task;
            }
        }
    }
}
