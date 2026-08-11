namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Constant;
    using LAB2D.Data;
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Item;
    using LAB2D.Map;
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

        /// <summary>
        /// 是否为地形挖掘（而非资源采集）。true 时 Finish() 调用 TileMap.DigTerrain。
        /// </summary>
        private bool isTerrainDig;

        /// <summary>
        /// 要挖掘的地形 ID（仅 isTerrainDig=true 时有效）。
        /// </summary>
        private int terrainIdToDig;

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

            Vector3Int posMap = Vector3IntLAB.ToVector3Int(this.TargetMap);

            // 地形挖掘分支：替换 TileMap 瓦片而非移除 ResourceMap 资源
            if (this.isTerrainDig)
            {
                int oldTerrainId = this.terrainIdToDig;
                int newTerrainId = TileMapProvider().DigTerrain(posMap);

                LogProvider(
                    $"地形挖掘完成: pos=({posMap.x},{posMap.y}) terrainId {oldTerrainId} -> {newTerrainId}",
                    LogManager.LogLevelEnum.Trace);

                // 通过地形配置的 tileResourceName 查找掉落（如 "Mountain"）
                string terrainResourceName = Core.ServiceLocator.Get<TerrainConfigDatabase>()
                    .GetTileResourceName(oldTerrainId);
                List<DropItem> dropItems = !string.IsNullOrEmpty(terrainResourceName)
                    ? Core.ServiceLocator.Get<DropDataManager>().GetDropItemsByResourceName(terrainResourceName)
                    : new List<DropItem>();

                // 地形挖掘的掉落处理（复用下方公共逻辑）
                this.ProcessDrops(worker, dropItems, posMap);

                // 删除采集/挖掘标记
                GatherMapProvider().CancelGather(posMap);
                return;
            }

            // 资源采集分支（原有逻辑）
            ResourceMapProvider().CutTree(posMap);
            List<DropItem> resourceDropItems = DropDataProvider(this.resourceInfo.Id);

            this.ProcessDrops(worker, resourceDropItems, posMap);

            // 删除采摘图标
            GatherMapProvider().CancelGather(posMap);
        }

        /// <summary>
        /// 统一处理掉落物（资源采集和地形挖掘共用）。
        /// Worker 悬赏的掉落物会被收集起来，在循环结束后统一创建一个批量 CarryTask(ToBoard)，
        /// 让 Worker 一次性搬运所有物品到任务栏，避免逐个搬运的低效往返。
        /// </summary>
        private void ProcessDrops(AWorker worker, List<DropItem> dropItems, Vector3Int targetPos)
        {
            if (dropItems == null || dropItems.Count == 0)
            {
                return;
            }

            // 悬赏任务：OwnerId=发布者；普通任务：OwnerId=采集者
            bool isBounty = AWorkerTask.IsBountyExecution;
            int workerId = isBounty
                ? AWorkerTask.BountyOwnerOverride
                : worker.GetInstanceID();

            AWorkerTask.LogProvider(
                $"[GatherOwner] executor={worker.name}({worker.GetInstanceID()}) override={AWorkerTask.BountyOwnerOverride} finalOwner={workerId} isBounty={isBounty}",
                LogManager.LogLevelEnum.Debug);

            // 收集 Worker 悬赏掉落位置和资源（用于批量创建 CarryTask）
            List<Vector3Int> bountyPositions = null;
            List<ResourceInfo> bountyResources = null;

            // 记录自我采集掉落的所有位置和资源（用于链式拾取）
            List<Vector3Int> selfDropPositions = null;
            List<ResourceInfo> selfDropResources = null;

            // 采摘掉落木头,苹果
            for (int i = 0; i < dropItems.Count; i++)
            {
                // 设置所有权：采集所得归采集者
                dropItems[i].ResourceInfo.OwnerId = workerId;

                // 统一放置掉落物到地面
                Vector3Int pos = TryMergeOrPlaceDrop(targetPos, dropItems[i].ResourceInfo, dropItems[i].Name);

                if (pos == default)
                {
                    // 地图满到极限，放进背包不丢物品
                    worker.AddResource(dropItems[i].ResourceInfo);
                    continue;
                }

                // TryMergeOrPlaceDrop → PutDownToDrop 自动创建了普通 CarryTask，
                // 移除之（悬赏物品走批量搬运，普通物品走链式拾取）
                Core.ServiceLocator.Get<WorkerTaskManager>().RemoveCarryTaskAt(pos);

                if (isBounty)
                {
                    if (AWorkerTask.BountyOwnerOverride != 0) // Worker 悬赏
                    {
                        if (bountyPositions == null)
                        {
                            bountyPositions = new List<Vector3Int>();
                            bountyResources = new List<ResourceInfo>();
                        }

                        bountyPositions.Add(pos);
                        bountyResources.Add(dropItems[i].ResourceInfo);
                    }

                    // Player 悬赏（BountyOwnerOverride == 0）：物品留在地上，Player 自行拾取
                }
                else
                {
                    // 记录掉落位置，稍后链式拾取
                    if (selfDropPositions == null)
                    {
                        selfDropPositions = new List<Vector3Int>();
                        selfDropResources = new List<ResourceInfo>();
                    }

                    selfDropPositions.Add(pos);
                    selfDropResources.Add(dropItems[i].ResourceInfo);
                }
            }

            // Worker 悬赏：统一创建一个批量 CarryTask(ToBoard)，一次性搬运所有物品
            if (bountyPositions != null && bountyPositions.Count > 0)
            {
                this.CreateBatchCarryToBoard(bountyPositions, bountyResources, worker);
            }

            // 自我采集完成后，立即链式拾取所有掉落物
            if (!isBounty && selfDropPositions != null && selfDropPositions.Count > 0)
            {
                // 取出首个拾取目标，剩余的作为待拾取链
                Vector3Int firstPos = selfDropPositions[0];
                ResourceInfo firstResource = selfDropResources[0];
                selfDropPositions.RemoveAt(0);
                selfDropResources.RemoveAt(0);

                // 创建 FromGround 模式拾取任务，直接分配给自己
                WorkerPickUpTask pickUpTask = new WorkerPickUpTask.PickUpTaskBuilder()
                    .SetMode(WorkerPickUpTask.PickUpMode.FromGround)
                    .SetTargetPosition(firstPos)
                    .SetGroundResource(firstResource)
                    .SetOwnerId(workerId)
                    .SetPendingPickups(selfDropPositions, selfDropResources)
                    .Build();

                AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
                workerData.Task = pickUpTask;
                pickUpTask.Start(worker);

                int totalDrops = 1 + (selfDropPositions.Count > 0 ? selfDropPositions.Count : 0);
                LogProvider(
                    $"{worker.name} 采集完成，开始链式拾取 {totalDrops} 个掉落物: 首个 id={firstResource.Id} pos=({firstPos.x},{firstPos.y})",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 为 Worker 悬赏掉落物创建批量 CarryTask(ToBoard)。
        /// 多个掉落物合并为一个任务，Worker 一次性搬运全部物品到任务栏。
        /// </summary>
        /// <param name="positions">所有掉落物的地图位置列表</param>
        /// <param name="resources">所有掉落物的资源信息列表（与 positions 一一对应）</param>
        /// <param name="executor">执行悬赏的 Worker</param>
        private void CreateBatchCarryToBoard(
            List<Vector3Int> positions, List<ResourceInfo> resources, AWorker executor)
        {
            WorkerCarryTask carryToBoard = new WorkerCarryTask.CarryTaskBuilder()
                .SetMode(WorkerCarryTask.CarryMode.ToBoard)
                .SetResourceInfo(resources[0])          // 首个资源用于兼容
                .SetBatchResources(positions, resources) // 批量资源（自动设置首个位置为 TargetMap）
                .SetExecutor(executor.GetInstanceID())
                .Build();

            TaskAddProvider(
                carryToBoard,
                new GameGridPosition(positions[0].x, positions[0].y, 0),
                WorkerTaskPriority.WorkerBounty);

            LogProvider(
                $"[BountyDrop] Worker 悬赏批量掉落: {positions.Count} 个物品 → 1 个批量 CarryTask(ToBoard) pos=({positions[0].x},{positions[0].y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return true;
        }

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.HeavyWorkTiredCostPerSecond;

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
            private bool claimFailed;

            public GatherTaskBuilder()
            {
                this.task = new WorkerGatherTask();
            }

            public GatherTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                this.task.isTerrainDig = false;

                // 认领资源（防止多个 Worker 同时采集同一目标）
                if (!GatherMapProvider().AddGather(targetMap))
                {
                    LogProvider($"资源已被其他Worker认领: pos=({targetMap.x},{targetMap.y})", LogManager.LogLevelEnum.Warning);
                    this.claimFailed = true;
                }

                return this;
            }

            /// <summary>
            /// 设置目标为可挖掘地形（如山）。
            /// 地形不在 ResourceMap 中，认领复用 GatherMap。
            /// </summary>
            public GatherTaskBuilder SetTerrainTarget(Vector3Int targetMap, int terrainId)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                this.task.isTerrainDig = true;
                this.task.terrainIdToDig = terrainId;

                // 认领位置（复用 GatherMap，防止多个 Worker 同时挖掘同一位置）
                if (!GatherMapProvider().AddGather(targetMap))
                {
                    LogProvider($"挖掘位置已被其他Worker认领: pos=({targetMap.x},{targetMap.y})", LogManager.LogLevelEnum.Warning);
                    this.claimFailed = true;
                }

                return this;
            }

            public GatherTaskBuilder SetResourceInfo(ResourceInfo resourceInfo)
            {
                this.task.resourceInfo = DataTool.DeepCopyByBinary(resourceInfo);
                return this;
            }

            /// <summary>
            /// 构建采集任务。如果资源已被其他 Worker 认领，返回 null。
            /// </summary>
            public WorkerGatherTask Build()
            {
                return this.claimFailed ? null : this.task;
            }
        }
    }
}
