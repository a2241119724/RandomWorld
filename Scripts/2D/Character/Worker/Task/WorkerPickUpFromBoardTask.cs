namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 从任务栏拾取 — 单阶段任务：走到任务栏邻居位置 → 从内部存储取回属于自己的物品。
    ///
    /// 悬赏发布者使用此任务取回任务栏处属于自己的物品。
    /// 物品存储在 TaskBoardManager 内部字典中，不在地面。
    /// 此任务不是悬赏类型，不展示在任务栏中。
    /// </summary>
    [Serializable]
    public class WorkerPickUpFromBoardTask : AWorkerTask
    {
        /// <summary>此任务的物主 ID（只有该 Worker 可接）</summary>
        private int targetOwnerId;

        public WorkerPickUpFromBoardTask()
            : base(WorkerTaskType.PickUpFromBoard)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = 0.5f;
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
                LogManager.LogLevelEnum.Info);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            if (worker.GetInstanceID() != this.targetOwnerId) return false;

            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            return board != null && board.IsInitialized && board.HasDeliveredItems(this.targetOwnerId);
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]); // 自身位置
        }

        // ---- Builder ----

        public class PickUpFromBoardTaskBuilder
        {
            private readonly WorkerPickUpFromBoardTask task;

            public PickUpFromBoardTaskBuilder()
            {
                this.task = new WorkerPickUpFromBoardTask();
            }

            /// <summary>设置任务栏邻居位置为目标</summary>
            public PickUpFromBoardTaskBuilder SetBoardNeighbor(Vector3Int neighborPos)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(neighborPos);
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
                return this.task;
            }
        }
    }
}
