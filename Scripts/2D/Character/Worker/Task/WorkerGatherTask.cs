namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 采集任务
    /// </summary>
    [Serializable]
    public class WorkerGatherTask : AWorkerTask
    {
        /// <summary>
        /// Worker携带的资源
        /// </summary>
        private ResourceInfo resourceInfo;

        public WorkerGatherTask()
            : base(WorkerTaskType.Gather)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.GatherSeconds;
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
            ResourceMapProvider().CutTree(Vector3IntLAB.ToVector3Int(this.TargetMap));
            List<DropItem> dropItems = DropDataProvider(this.resourceInfo.Id);

            // 悬赏任务：OwnerId=发布者（!=0）；普通任务：OwnerId=采集者
            int workerId = AWorkerTask.BountyOwnerOverride != 0
                ? AWorkerTask.BountyOwnerOverride
                : worker.GetInstanceID();

            bool isBounty = AWorkerTask.BountyOwnerOverride != 0;

            AWorkerTask.LogProvider(
                $"[GatherOwner] executor={worker.name}({worker.GetInstanceID()}) override={AWorkerTask.BountyOwnerOverride} finalOwner={workerId} isBounty={isBounty}",
                LogManager.LogLevelEnum.Info);

            // 采摘掉落木头,苹果
            for (int i = 0; i < dropItems.Count; i++)
            {
                Vector3Int targetPos = Vector3IntLAB.ToVector3Int(this.TargetMap);

                // 设置所有权：采集所得归采集者
                dropItems[i].ResourceInfo.OwnerId = workerId;

                if (isBounty)
                {
                    // 悬赏产出物：放置到地上 + 创建 CarryToBoardTask（搬运到任务栏）
                    this.PlaceBountyDropAndCreateCarry(targetPos, dropItems[i], worker);
                }
                else
                {
                    // 普通掉落物：使用原有流程（合并+放置+自动创建 CarryTask）
                    Vector3Int pos = TryMergeOrPlaceDrop(targetPos, dropItems[i].ResourceInfo, dropItems[i].Name);

                    if (pos == default)
                    {
                        // 地图满到极限，放进背包不丢物品
                        worker.AddResource(dropItems[i].ResourceInfo);
                    }
                }
            }

            // 删除采摘图标
            GatherMapProvider().CancelGather(Vector3IntLAB.ToVector3Int(this.TargetMap));
        }

        /// <summary>
        /// 放置悬赏掉落物并创建搬运到任务栏的任务。
        /// 与普通掉落物的区别：不调用 PutDownToDrop（它会创建普通 CarryTask），
        /// 而是直接放置物品并创建 CarryToBoardTask。
        /// </summary>
        private void PlaceBountyDropAndCreateCarry(Vector3Int dropPos, DropItem dropItem, AWorker executor)
        {
            // 复用现有掉落物放置逻辑：先尝试合并到周围同类，否则环形搜索空地放置
            Vector3Int placePos = TryMergeOrPlaceDrop(dropPos, dropItem.ResourceInfo, dropItem.Name);

            if (placePos == default)
            {
                LogProvider("[BountyDrop] 周围无可用位置，物品丢失", LogManager.LogLevelEnum.Error);
                return;
            }

            // TryMergeOrPlaceDrop → PutDownToDrop 自动创建了普通 CarryTask，
            // 悬赏物品需要替换为 CarryToBoardTask（搬运到任务栏）
            Core.ServiceLocator.Get<WorkerTaskManager>().RemoveCarryTaskAt(placePos);

            // 创建搬运到任务栏的任务（只允许执行悬赏的 Worker 接取）
            WorkerCarryToBoardTask carryToBoard = new WorkerCarryToBoardTask.CarryToBoardTaskBuilder()
                .SetStartTarget(placePos)
                .SetResourceInfo(dropItem.ResourceInfo)
                .SetExecutor(executor.GetInstanceID())
                .Build();

            TaskAddProvider(
                carryToBoard,
                new GameGridPosition(placePos.x, placePos.y, placePos.z),
                1); // 高优先级

            LogProvider(
                $"[BountyDrop] 悬赏掉落物放置: pos=({placePos.x},{placePos.y}) → 创建 CarryToBoardTask",
                LogManager.LogLevelEnum.Info);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return true;
        }

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.TrackPositions;

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class GatherTaskBuilder
        {
            private readonly WorkerGatherTask task;

            public GatherTaskBuilder()
            {
                this.task = new WorkerGatherTask();
            }

            public GatherTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);

                // 显示正在采摘图标
                GatherMapProvider().AddGather(targetMap);
                return this;
            }

            public GatherTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            public WorkerGatherTask Build()
            {
                return this.task;
            }
        }
    }
}
