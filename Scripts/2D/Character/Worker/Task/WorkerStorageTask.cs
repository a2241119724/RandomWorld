namespace LAB2D.Character.Worker.Task
{
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Core.Seek;
    using LAB2D.Domain.Common;
    using LAB2D.Enum;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 存取个人四格仓库任务 — 单阶段任务：走到家里仓库瓦片邻居格 → 执行存取。
    /// Store：把身上"现在不需要"的物品存入仓库（DepositToStorage）。
    /// Withdraw：把仓库中的任务材料取到身上（WithdrawFromStorage）。
    /// 物理位置约束：必须站到仓库瓦片旁才能存取（复用 WorkerSeekState 的邻居寻路）。
    /// 支持 chainCompleteTask 链式接力（如存完回来继续拾取）。
    /// </summary>
    [Serializable]
    public class WorkerStorageTask : AWorkerTask
    {
        public enum StorageMode
        {
            /// <summary>存入仓库（身上→仓库）</summary>
            Store,

            /// <summary>从仓库取出（仓库→身上）</summary>
            Withdraw,
        }

        private StorageMode mode;

        /// <summary>Store：要存入仓库的物品列表</summary>
        private List<ResourceInfo> depositResources;

        /// <summary>Withdraw：要取出的物品（id→数量）</summary>
        private Dictionary<int, int> withdrawNeeds;

        /// <summary>完成后链式启动的任务（如回来继续拾取）</summary>
        private AWorkerTask chainCompleteTask;

        public WorkerStorageTask()
            : base(WorkerTaskType.Storage)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.CarryTakeSeconds;
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

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            Vector3Int posMap = Vector3IntLAB.ToVector3Int(this.TargetMap);

            switch (this.mode)
            {
                case StorageMode.Store:
                    if (this.depositResources != null)
                    {
                        foreach (ResourceInfo r in this.depositResources)
                        {
                            bool ok = worker.DepositToStorage(r);
                            LogProvider(
                                $"[TaskDiag] {worker.name} 存入仓库 id={r.Id} x{r.Count} pos=({posMap.x},{posMap.y}) 成功={ok}",
                                LogManager.LogLevelEnum.Debug);
                        }
                    }

                    break;

                case StorageMode.Withdraw:
                    if (this.withdrawNeeds != null)
                    {
                        foreach (KeyValuePair<int, int> kv in this.withdrawNeeds)
                        {
                            int took = worker.WithdrawFromStorage(kv.Key, kv.Value);
                            LogProvider(
                                $"[TaskDiag] {worker.name} 取回仓库 id={kv.Key} x{took} pos=({posMap.x},{posMap.y})",
                                LogManager.LogLevelEnum.Debug);
                        }
                    }

                    break;
            }

            // 刷新仓库格图标，反映存取后的仓库现状（参考 WorkerCarryTask.FinishToInventory）
            WorkerStorageTask.RefreshStorageIcons(worker);

            // 链式接力：存取完成后启动后续任务（如回来继续拾取）。
            // base.Finish 已置空 Task，这里覆盖为新任务。
            if (this.chainCompleteTask != null && wd != null)
            {
                wd.Task = this.chainCompleteTask;
                this.chainCompleteTask.Start(worker);
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.PlannedHomePosition == null) return false;

            var layout = WorkerBrain.GetRoomLayout(wd);
            if (wd.HomeBuildStage < layout.CompleteStage) return false; // 家未建完

            switch (this.mode)
            {
                case StorageMode.Store:
                    return this.depositResources != null && this.depositResources.Count > 0;

                case StorageMode.Withdraw:
                    if (this.withdrawNeeds == null || this.withdrawNeeds.Count == 0) return false;
                    foreach (KeyValuePair<int, int> kv in this.withdrawNeeds)
                    {
                        if (!worker.HasInStorage(kv.Key, kv.Value)) return false;
                    }

                    return true;

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
            // 仓库瓦片是碰撞体（不可通行），Worker 站到其正交邻居格存取
            this.AvailableNeighborPos.Add(Neighbors[0]); // 上
            this.AvailableNeighborPos.Add(Neighbors[1]); // 右
            this.AvailableNeighborPos.Add(Neighbors[2]); // 下
            this.AvailableNeighborPos.Add(Neighbors[3]); // 左
        }

        // ---- 静态助手 ----

        /// <summary>
        /// 选一个可达的仓库瓦片绝对坐标（PlannedHomePosition + StorageOffsets[i]）。
        /// 任一正交邻居可通行即视为可达；无家/家未建完/全不可达返回 default。
        /// 与 DoIsCanWork 的 CompleteStage 判断保持一致，避免决策层发出启动即失败的任务。
        /// </summary>
        public static Vector3Int PickStorageTile(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.PlannedHomePosition == null) return default;

            var layout = WorkerBrain.GetRoomLayout(wd);
            if (wd.HomeBuildStage < layout.CompleteStage) return default; // 家未建完
            Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
            foreach (Vector3Int so in layout.StorageOffsets)
            {
                Vector3Int tile = center + so;
                for (int k = 0; k < 4; k++) // 4 正交邻居
                {
                    // 坐标转置：Neighbors 的 (X,Y) 在 tile 空间对应 (Y,X)，
                    // 与 WorkerSeekState.OnEnter 的邻居坐标一致（见 bug-fixes.md 2026-08-14 轴语义）。
                    Vector3Int nb = tile + new Vector3Int(Neighbors[k].Y, Neighbors[k].X, 0);
                    if (ASeek.IsCanReach(nb))
                    {
                        return tile;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// 刷新仓库格物品图标：按 Storage 当前内容在 4 个仓库格绘制/清除物品图标。
        /// 参考 WorkerCarryTask.FinishToInventory 的 ItemMapProvider().AddTile 图标机制。
        /// 仓库格是碰撞体瓦片（InventoryWall），图标画在 ItemMap（地面道具层），随存档保存并网络同步。
        /// </summary>
        public static void RefreshStorageIcons(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.PlannedHomePosition == null || wd.Storage == null)
            {
                LogProvider(
                    $"[TaskDiag] {worker.name} 刷新仓库图标跳过: 无家位置/无仓库数据",
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            var layout = WorkerBrain.GetRoomLayout(wd);
            if (wd.HomeBuildStage < layout.CompleteStage)
            {
                LogProvider(
                    $"[TaskDiag] {worker.name} 刷新仓库图标跳过: 家未建完 stage={wd.HomeBuildStage}/{layout.CompleteStage}",
                    LogManager.LogLevelEnum.Debug);
                return; // 家未建完，无仓库格
            }

            Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);

            // 清空 4 格旧图标
            int cleared = 0;
            foreach (Vector3Int so in layout.StorageOffsets)
            {
                Vector3Int pos = center + so;
                if (ItemMapProvider().HasTile(pos))
                {
                    ItemMapProvider().DeleteTile(pos);
                    cleared++;
                }
            }

            // 按仓库现状重建（最多 4 种，一种一格）
            // 防御旧存档失效物品：id 不在 ItemDataManager / tile 资源缺失时跳过该格，
            // 避免 NRE 中断 Finish（RefreshStorageIcons 在 Finish 中调用，异常会连带
            // 断掉 chainCompleteTask 接力）。与 Carry 不同，Storage 键来自持久化存档，可能失效。
            int index = 0;
            int drawn = 0;
            int skipped = 0;
            foreach (KeyValuePair<int, ResourceInfo> kv in wd.Storage)
            {
                if (index >= layout.StorageOffsets.Count) break;
                if (kv.Value == null || kv.Value.Count <= 0) continue;
                ItemData itemData = ItemDataProvider(kv.Key);
                if (itemData == null)
                {
                    LogProvider(
                        $"[TaskDiag] {worker.name} 仓库图标跳过: id={kv.Key} 物品数据缺失(旧存档失效)",
                        LogManager.LogLevelEnum.Debug);
                    skipped++;
                    continue;
                }

                TileBase tile = ResourceLoadProvider(itemData.Name) as TileBase;
                if (tile == null)
                {
                    LogProvider(
                        $"[TaskDiag] {worker.name} 仓库图标跳过: {itemData.Name}(id={kv.Key}) 无对应 Tile 资源",
                        LogManager.LogLevelEnum.Warning);
                    skipped++;
                    continue;
                }

                Vector3Int pos = center + layout.StorageOffsets[index];
                ItemMapProvider().AddTile(pos, tile);
                LogProvider(
                    $"[TaskDiag] {worker.name} 仓库图标绘制: {itemData.Name}(id={kv.Key}) pos=({pos.x},{pos.y})",
                    LogManager.LogLevelEnum.Trace);
                index++;
                drawn++;
            }

            LogProvider(
                $"[TaskDiag] {worker.name} 仓库图标刷新完成: 清除{cleared} 绘制{drawn} 跳过{skipped} (仓库{wd.Storage.Count}种) center=({center.x},{center.y})",
                LogManager.LogLevelEnum.Debug);
        }

        // ---- Builder ----

        public class StorageTaskBuilder
        {
            private readonly WorkerStorageTask task;

            public StorageTaskBuilder()
            {
                this.task = new WorkerStorageTask();
            }

            /// <summary>设置存取模式（Store/Withdraw）</summary>
            public StorageTaskBuilder SetMode(StorageMode mode)
            {
                this.task.mode = mode;
                return this;
            }

            /// <summary>设置仓库瓦片绝对坐标为目标</summary>
            public StorageTaskBuilder SetTarget(Vector3Int tile)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(tile);
                return this;
            }

            /// <summary>Store 模式：要存入仓库的物品列表</summary>
            public StorageTaskBuilder SetDepositResources(List<ResourceInfo> list)
            {
                this.task.depositResources = list != null
                    ? new List<ResourceInfo>(list)
                    : null;
                return this;
            }

            /// <summary>Withdraw 模式：要取出的物品（id→数量）</summary>
            public StorageTaskBuilder SetWithdrawNeeds(Dictionary<int, int> needs)
            {
                this.task.withdrawNeeds = needs != null
                    ? new Dictionary<int, int>(needs)
                    : null;
                return this;
            }

            /// <summary>设置完成后链式启动的任务（如回来继续拾取）</summary>
            public StorageTaskBuilder SetChainCompleteTask(AWorkerTask chainTask)
            {
                this.task.chainCompleteTask = chainTask;
                return this;
            }

            public WorkerStorageTask Build()
            {
                return this.task;
            }
        }
    }
}
