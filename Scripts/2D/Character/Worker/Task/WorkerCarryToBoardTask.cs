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
    /// 搬运到任务栏 — 两阶段任务：取货 → 交付到任务栏。
    ///
    /// Stage 0: 走到掉落位置，从地面捡起物品
    /// Stage 1: 走到任务栏四周的相邻位置，将物品加入任务栏内部存储
    ///
    /// 物品存入 TaskBoardManager 内部字典，不在地面创建图标。
    /// 发布者通过 PickUpFromBoardTask 取回。
    /// 任何空闲 Worker 都可以接此任务（公共服务）。
    /// </summary>
    [Serializable]
    public class WorkerCarryToBoardTask : AWorkerTask
    {
        /// <summary>要搬运的资源</summary>
        private ResourceInfo resourceInfo;

        /// <summary>指定执行搬运的 Worker ID（0=任何人可接）</summary>
        private int targetWorkerId;

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

                // 从地上捡起掉落物
                Vector3Int pickUpPos = Vector3IntLAB.ToVector3Int(this.TargetMap);
                ItemMapProvider().PickUpFromDrop(pickUpPos, this.resourceInfo);
                worker.AddResource(this.resourceInfo);

                // 目标：任务栏四周的相邻位置（上下左右第一个可到达的）
                Vector3Int neighbor = this.GetBoardNeighbor();
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(neighbor);

                // 直接走到目标点（与 CarryTask 一致）
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]); // 自身
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

            // 将物品加入任务栏内部存储（不创地面图标）
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
            // 只有指定的执行者才能接此搬运任务（targetWorkerId=0 时允许任何人）
            if (this.targetWorkerId != 0 && worker.GetInstanceID() != this.targetWorkerId)
            {
                return false;
            }
            return true;
        }

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
            this.AvailableNeighborPos.Add(Neighbors[8]); // 自身位置，用于 Stage 0 取货
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
            // 回退
            return Vector3IntLAB.ToVector3Int(this.TargetMap);
        }

        // ---- Builder ----

        public class CarryToBoardTaskBuilder
        {
            private readonly WorkerCarryToBoardTask task;

            public CarryToBoardTaskBuilder()
            {
                this.task = new WorkerCarryToBoardTask();
            }

            public CarryToBoardTaskBuilder SetStartTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            public CarryToBoardTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            /// <summary>设置执行搬运的 Worker ID（0=任何人可接）</summary>
            public CarryToBoardTaskBuilder SetExecutor(int workerId)
            {
                this.task.targetWorkerId = workerId;
                return this;
            }

            public WorkerCarryToBoardTask Build()
            {
                return this.task;
            }
        }
    }
}
