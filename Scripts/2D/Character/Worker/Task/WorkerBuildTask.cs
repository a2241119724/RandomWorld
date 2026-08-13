namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Worker;
    using LAB2D.Item;
    using LAB2D.Item.Build;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 任务2阶段：拿材料，建造
    /// Build在第一个阶段预留资源
    /// </summary>
    [Serializable]
    public class WorkerBuildTask : AWorkerTask
    {
        private Dictionary<int, ResourceInfo> needs;
        private Dictionary<int, ResourceInfo> temp;

        /// <summary>
        /// 建造的位置
        /// </summary>
        private Vector3IntLAB buildPos;

        /// <summary>
        /// 建造任务的专属 Owner（0 = 公开任务，任何 Worker 可接）。
        /// VerifyBuildTasks 恢复 Self-Build 任务时设置，防止其他 Worker 抢建别人的墙。
        /// </summary>
        private int ownerWorkerId = 0;

        public WorkerBuildTask()
            : base(WorkerTaskType.Build)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.BuildFetchResourceSeconds;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(InventoryProvider().GetPosByPreTake(worker));
                if (this.TargetMap == default)
                {
                    LogProvider(
                        $"[TaskDiag] {worker.name} 建造任务失败: 仓库无取料位置",
                        LogManager.LogLevelEnum.Debug);
                    this.GiveUpTask(worker);
                }

                // 进入工作状态
            });
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.GetBuildConstructionSeconds(this.needs);

                // 建造
                this.Init();
            });
        }

        /// <summary>
        /// 建造的瓦片名称（用于完成时推进建家阶段）
        /// </summary>
        public string BuildTileName { get; private set; }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            // [TaskDiag] 开始任务（Build 不走 base.Start，需在此补记）
            LogProvider(
                $"[TaskDiag] {worker.name} 开始任务 type={this.TaskType} target=({this.TargetMap.X},{this.TargetMap.Y})",
                LogManager.LogLevelEnum.Debug);

            // 自身携带资源足够
            if (worker.IsEnough(this.needs))
            {
                // LogManager.Instance.log("携带资源充足", LogManager.LogLevel.Info);
                this.ChangeStage(worker, 1);
                return;
            }

            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            InventoryProvider().IsEnoughAndPreTake(worker, remaining, true);

            // 不够就取资源
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);

            // 减少worker携带的资源
            worker.SubResource(this.needs);

            // 确定建造者和所属者
            string builderName = worker.name;
            string ownerName = builderName;

            if (AWorkerTask.IsBountyExecution)
            {
                if (BountyOwnerOverride == 0)
                {
                    // Player 发布的建造悬赏，所有者是 Player
                    ownerName = "Player";
                }
                else
                {
                    var workers = Core.ServiceLocator.Get<WorkerManager>().Characters;
                    var owner = workers.Find(w => w.GetInstanceID() == BountyOwnerOverride);
                    if (owner != null)
                    {
                        ownerName = owner.name;
                    }
                }
            }

            // 将建造完成的Tile从Building变为Build中
            Core.ServiceLocator.Get<BuildMap>().SetComplete(this.buildPos, builderName, ownerName);

            // 推进建家阶段（在任务完成时而非创建时推进，防止任务中断导致墙壁跳过）
            this.AdvanceHomeBuildStageOnComplete(worker);
        }

        /// <summary>
        /// 建造完成后推进 Worker 的建家阶段。
        /// 从 WorkerSeekState.AdvanceHomeBuildStage 迁移至此，
        /// 确保只有在建造真正完成时才推进阶段，避免任务中断导致墙壁被跳过。
        /// </summary>
        private void AdvanceHomeBuildStageOnComplete(AWorker worker)
        {
            if (string.IsNullOrEmpty(this.BuildTileName)) return;

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.HomePosition != null) return;

            var layout = LAB2D.AI.Worker.WorkerBrain.GetRoomLayout(wd);
            int wallCount = layout.WallCount;
            int completeStage = layout.CompleteStage;
            int prevStage = wd.HomeBuildStage;
            wd.HomeBuildStage++;

            if (this.BuildTileName.StartsWith("CustomRoomWall") && wd.HomeBuildStage < wallCount)
            {
                LogProvider(
                    $"{worker.name} 建家: 墙壁{prevStage + 1}/{wallCount} → 下一块",
                    LogManager.LogLevelEnum.Debug);
            }
            else if (this.BuildTileName.StartsWith("CustomRoomWall") && wd.HomeBuildStage >= wallCount)
            {
                LogProvider(
                    $"{worker.name} 建家: 墙壁完成 → 接下来建门",
                    LogManager.LogLevelEnum.Debug);
            }
            else if (this.BuildTileName == "CustomDoor")
            {
                // 墙壁和门建完 → 立即注册为房间，之后再建床和仓库
                this.RegisterWorkerRoom(wd, worker.name);
                LogProvider(
                    $"{worker.name} 建家: 门完成 → 房间已注册 → 接下来建床和仓库",
                    LogManager.LogLevelEnum.Debug);
            }
            else if (this.BuildTileName == "SingleBed")
            {
                LogProvider(
                    $"{worker.name} 建家: 床完成 → 接下来建仓库",
                    LogManager.LogLevelEnum.Debug);

                // 自动绑定床到当前 Worker（床位置 = 房间中心 + BedOffset）
                if (wd.PlannedHomePosition != null)
                {
                    Vector3Int roomCenter = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
                    Vector3Int bedPos = roomCenter + layout.BedOffset;
                    var fm = Core.ServiceLocator.Get<Item.FurnitureManager>();
                    fm.AddBed(bedPos);
                    fm.AddWorkerToBed(bedPos, worker);
                    LogProvider(
                        $"{worker.name} 床已自动绑定: pos=({bedPos.x},{bedPos.y})",
                        LogManager.LogLevelEnum.Debug);
                }
            }
            else if (this.BuildTileName.StartsWith("InventoryWall"))
            {
                if (wd.HomeBuildStage == layout.StorageStage4 + 1)
                {
                    // 第四个仓库完成 → 建家全部完成
                    wd.HomeBuildStage = completeStage;
                    wd.LifeStage = LAB2D.Domain.Worker.WorkerLifeStage.Settled;
                    LogProvider(
                        $"{worker.name} 建家: 仓库4完成 → 建家全部完成! → Settled 阶段",
                        LogManager.LogLevelEnum.Info);
                }
                else if (wd.HomeBuildStage == layout.StorageStage3 + 1)
                {
                    LogProvider(
                        $"{worker.name} 建家: 仓库3完成 → 接下来建仓库4",
                        LogManager.LogLevelEnum.Debug);
                }
                else if (wd.HomeBuildStage == layout.StorageStage2 + 1)
                {
                    LogProvider(
                        $"{worker.name} 建家: 仓库2完成 → 接下来建仓库3",
                        LogManager.LogLevelEnum.Debug);
                }
                else
                {
                    LogProvider(
                        $"{worker.name} 建家: 仓库1完成 → 接下来建仓库2",
                        LogManager.LogLevelEnum.Debug);
                }
            }
        }

        /// <summary>
        /// 墙壁和门建完后，将所有墙壁和门位置注册到 RoomManager 形成房间。
        /// </summary>
        private void RegisterWorkerRoom(AWorker.WorkerData wd, string ownerName)
        {
            if (wd?.PlannedHomePosition == null) return;

            Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
            var layout = LAB2D.AI.Worker.WorkerBrain.GetRoomLayout(wd);
            var roomInfo = new LAB2D.Item.RoomInfo();

            for (int i = 0; i < layout.WallCount; i++)
            {
                roomInfo.Points.Add(center + layout.WallOffsets[i]);
            }

            roomInfo.Points.Add(center + layout.DoorOffset);
            roomInfo.Progress = 0;
            roomInfo.Temperature = 25.0f;
            roomInfo.Humidity = 25.0f;
            roomInfo.OwnerName = ownerName;

            Core.ServiceLocator.Get<LAB2D.Item.RoomManager>().AddRoom(
                System.Guid.NewGuid().ToString(), roomInfo);

            LogProvider(
                $"{ownerName} 房间已注册: {roomInfo.Points.Count} 个墙壁/门位置",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void GiveUpTask(AWorker worker)
        {
            base.GiveUpTask(worker);

            // 恢复资源
            this.temp = DataTool.DeepCopyByBinary(this.needs);
        }

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.ownerWorkerId;

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // ownerWorkerId != 0 表示专属建造任务（Self-Build 恢复），只有原 Builder 可接
            if (this.ownerWorkerId != 0 && worker.GetInstanceID() != this.ownerWorkerId)
            {
                return false;
            }

            // 如果worker携带的资源已经满足建造
            if (worker.IsEnough(this.needs))
            {
                return true;
            }

            // 按照单个任务的资源取看是否足够
            // 获得剩余不够的数量
            Dictionary<int, ResourceInfo> remaining = worker.GetRemaining(this.needs);
            return InventoryProvider().IsEnoughAndPreTake(worker, remaining);
        }

        /// <inheritdoc/>
        protected override float TiredCostPerSecond => WorkerTaskTimeConfig.MediumWorkTiredCostPerSecond;

        /// <inheritdoc/>
        protected override bool StageChangeRule(AWorker worker)
        {
            // 只worker携带的资源不够时,取建筑材料
            switch (this.stage)
            {
                case 0:
                    ResourceInfo resourceInfo = InventoryProvider().SubItemByPreTake(worker, Vector3IntLAB.ToVector3Int(this.TargetMap));
                    worker.AddResource(resourceInfo);

                    // 减少需求的数量
                    foreach (KeyValuePair<int, ResourceInfo> pair in this.temp)
                    {
                        if (pair.Key == resourceInfo.Id)
                        {
                            pair.Value.Count -= resourceInfo.Count;
                            if (pair.Value.Count <= 0)
                            {
                                this.temp.Remove(resourceInfo.Id);
                            }

                            break;
                        }
                    }

                    // 获取完成所有的材料
                    if (this.temp.Count == 0)
                    {
                        this.ChangeStage(worker, 1);
                        return false;
                    }

                    this.ChangeStage(worker, 0);
                    return false;
                default:
                    return true;
            }
        }

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[0]);
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[2]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
            this.TargetMap = this.buildPos;
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class BuildTaskBuilder
        {
            private readonly WorkerBuildTask task;

            public BuildTaskBuilder()
            {
                this.task = new WorkerBuildTask();
            }

            public BuildTaskBuilder SetBuild(ABuildItem buildItem)
            {
                this.task.BuildTileName = buildItem?.TileName;
                return this;
            }

            public BuildTaskBuilder SetBuildTileName(string tileName)
            {
                this.task.BuildTileName = tileName;
                return this;
            }

            public BuildTaskBuilder SetBuildPos(Vector3Int pos)
            {
                this.task.TargetMap = this.task.buildPos = Vector3IntLAB.ToVector3IntLAB(pos);
                return this;
            }

            public BuildTaskBuilder SetNeedResource(Dictionary<int, ResourceInfo> needResource)
            {
                this.task.temp = DataTool.DeepCopyByBinary(needResource);
                this.task.needs = DataTool.DeepCopyByBinary(needResource);
                return this;
            }

            /// <summary>设置建造任务的专属 Owner（0 = 公开任务）。Self-Build 恢复时使用。</summary>
            public BuildTaskBuilder SetOwnerWorkerId(int workerId)
            {
                this.task.ownerWorkerId = workerId;
                return this;
            }

            public WorkerBuildTask Build()
            {
                return this.task;
            }
        }
    }
}
