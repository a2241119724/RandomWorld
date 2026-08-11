namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 任务发布栏管理器 — 管理悬赏物品的集中交付和取回。
    ///
    /// 职责：
    /// - 存储任务栏世界坐标（BuildMap 上有 Bounty 图标）
    /// - 内部存储交付物品（按物主 Worker ID 分组），不在地面创建图标
    /// - 提供任务栏四周的相邻位置（用于 Carry(ToBoard) / PickUp 寻路目标）
    /// - 提供物品存取接口
    /// </summary>
    public class TaskBoardManager : ASingletonSaveData<TaskBoardManager>
    {
        /// <summary>搜索初始化位置的最大半径</summary>
        private const int DeliverySearchRadius = 15;

        /// <summary>任务栏四个方向（上右下左）</summary>
        private static readonly Vector3Int[] NeighborDirs =
        {
            new Vector3Int(0, 1, 0),   // 上
            new Vector3Int(1, 0, 0),   // 右
            new Vector3Int(0, -1, 0),  // 下
            new Vector3Int(-1, 0, 0),  // 左
        };

        /// <summary>任务栏世界坐标（地图格子坐标）</summary>
        public Vector3Int BoardPosition { get; private set; }

        /// <summary>任务栏是否已初始化</summary>
        public bool IsInitialized => this.BoardPosition != default;

        /// <summary>
        /// 交付物品存储 — key: 物主 Worker instance ID, value: 待取回的物品列表。
        /// 不在地面创建 DropManager/ItemMap 图标，发布者直接从此字典取回。
        /// </summary>
        private readonly Dictionary<int, List<ResourceInfo>> deliveredItems = new Dictionary<int, List<ResourceInfo>>();
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        // ---- 初始化 ----

        public void InitPosition(Vector3Int mapCenter)
        {
            if (this.IsInitialized) return;

            Vector3Int found = Character.Worker.Task.AWorkerTask.AvailablePositionProvider(mapCenter, DeliverySearchRadius, false);
            if (found != default)
            {
                this.BoardPosition = found;
            }
            else
            {
                this.BoardPosition = mapCenter;
            }

            this.PlaceBoardIcon(this.BoardPosition);
            this.GameLogger.Log($"[TaskBoard] 任务栏初始化完成: ({this.BoardPosition.x}, {this.BoardPosition.y})");
        }

        private void PlaceBoardIcon(Vector3Int pos)
        {
            var tile = (UnityEngine.Tilemaps.TileBase)Character.Worker.Task.AWorkerTask.ResourceLoadProvider("Bounty");
            if (tile == null) return;

            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            buildMap.DirectBuild(pos, tile);

            var posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (!buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
            {
                buildMap.BuildMapDataLAB.PosMap[posLAB] = new BuildMap.BuildTileData("Bounty", true);
            }
        }

        // ---- 相邻位置 ----

        /// <summary>
        /// 获取任务栏四周第一个可到达的相邻位置，作为寻路目标。
        /// 优先返回最近的可到达邻居。
        /// </summary>
        public Vector3Int GetNeighborPosition()
        {
            foreach (var dir in NeighborDirs)
            {
                Vector3Int pos = this.BoardPosition + dir;
                if (Core.ServiceLocator.Get<TileMap>().IsCanReach(pos))
                {
                    return pos;
                }
            }
            return this.BoardPosition; // 退而求其次
        }

        // ---- 物品存取 ----

        /// <summary>
        /// 将物品交付到任务栏（由 CarryTask(ToBoard) 调用）。
        /// </summary>
        public void DeliverItem(int ownerId, ResourceInfo resource)
        {
            if (!this.deliveredItems.TryGetValue(ownerId, out var list))
            {
                list = new List<ResourceInfo>();
                this.deliveredItems[ownerId] = list;
            }

            // 同 ID 合并
            var existing = list.Find(r => r.Id == resource.Id);
            if (existing != null)
            {
                existing.Count += resource.Count;
            }
            else
            {
                list.Add(new ResourceInfo(resource.Id, resource.Count, ownerId));
            }

            AWorkerTask.LogProvider($"[TaskBoard] 物品已交付: ownerId={ownerId} id={resource.Id} count={resource.Count}", LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 检查任务栏是否有属于指定 Worker 的物品。
        /// </summary>
        public bool HasDeliveredItems(int workerId)
        {
            return this.deliveredItems.TryGetValue(workerId, out var list) && list.Count > 0;
        }

        /// <summary>
        /// 取回属于自己的所有物品（由 PickUpTask 调用）。
        /// </summary>
        public List<ResourceInfo> RetrieveItems(int workerId)
        {
            if (this.deliveredItems.TryGetValue(workerId, out var list))
            {
                this.deliveredItems.Remove(workerId);
                return list;
            }
            return new List<ResourceInfo>();
        }

        /// <summary>
        /// 获取任务栏列表的展示文本（悬赏任务 + 已交付物品）。
        /// </summary>
        public string GetDisplayText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("━━━ 任务栏 ━━━");
            sb.AppendLine($"位置: ({this.BoardPosition.x}, {this.BoardPosition.y})");
            sb.AppendLine();

            // ---- 悬赏任务 ----
            var taskManager = Core.ServiceLocator.Get<WorkerTaskManager>();
            var bountyTasks = taskManager?.GetBountyTasks();
            if (bountyTasks != null && bountyTasks.Count > 0)
            {
                sb.AppendLine("【悬赏任务】");
                foreach (var (task, isRunning) in bountyTasks)
                {
                    string state = isRunning ? "进行中" : "等待中";
                    string issuer = GetOwnerName(task.BountyInfo.IssuerWorkerId);
                    sb.AppendLine($"  {task.Name} {state}");
                    sb.AppendLine($"    发布者: {issuer}  赏金: {task.BountyInfo.Reward.Gold}G");
                }
                sb.AppendLine();
            }

            // ---- 已交付物品 ----
            if (this.deliveredItems.Count > 0)
            {
                sb.AppendLine("【待取回物品】");
                foreach (var kv in this.deliveredItems)
                {
                    string ownerName = GetOwnerName(kv.Key);
                    sb.AppendLine($"  [{ownerName}]");
                    foreach (var ri in kv.Value)
                    {
                        string itemName = GetItemName(ri.Id);
                        sb.AppendLine($"    {itemName}(id={ri.Id}) x{ri.Count}");
                    }
                }
                sb.AppendLine();
            }

            if (bountyTasks?.Count == 0 && this.deliveredItems.Count == 0)
            {
                sb.AppendLine("(空)");
            }

            return sb.ToString();
        }

        private static string GetOwnerName(int ownerId)
        {
            if (ownerId == 0) return "Player";

            var cm = Core.ServiceLocator.Get<CurrencyManager>();
            var worker = cm?.FindWorker(ownerId);
            return worker != null ? worker.name : $"Worker#{ownerId}";
        }

        private static string GetItemName(int itemId)
        {
            try
            {
                var itemData = Core.ServiceLocator.Get<ItemDataManager>().GetById(itemId);
                return itemData?.CnName ?? $"item_{itemId}";
            }
            catch
            {
                return $"item_{itemId}";
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            TaskBoardManagerData data = new TaskBoardManagerData
            {
                BoardPosX = this.BoardPosition.x,
                BoardPosY = this.BoardPosition.y,
                BoardPosZ = this.BoardPosition.z,
            };

            foreach (KeyValuePair<int, List<ResourceInfo>> kv in this.deliveredItems)
            {
                foreach (ResourceInfo ri in kv.Value)
                {
                    data.DeliveredItems.Add(new DeliveredItemEntry
                    {
                        OwnerId = kv.Key,
                        ItemId = ri.Id,
                        Count = ri.Count,
                    });
                }
            }

            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            TaskBoardManagerData data = DataTool.LoadDataByBinary<TaskBoardManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            // 恢复任务栏位置
            if (data.BoardPosX != 0 || data.BoardPosY != 0)
            {
                this.BoardPosition = new Vector3Int(data.BoardPosX, data.BoardPosY, data.BoardPosZ);
                // 重建任务栏图标（InitPosition 会因 IsInitialized=true 跳过）
                this.PlaceBoardIcon(this.BoardPosition);
            }

            // 恢复已交付物品
            this.deliveredItems.Clear();
            if (data.DeliveredItems != null)
            {
                foreach (DeliveredItemEntry entry in data.DeliveredItems)
                {
                    if (!this.deliveredItems.TryGetValue(entry.OwnerId, out List<ResourceInfo> list))
                    {
                        list = new List<ResourceInfo>();
                        this.deliveredItems[entry.OwnerId] = list;
                    }

                    // 同 ID 合并
                    ResourceInfo existing = list.Find(r => r.Id == entry.ItemId);
                    if (existing != null)
                    {
                        existing.Count += entry.Count;
                    }
                    else
                    {
                        list.Add(new ResourceInfo(entry.ItemId, entry.Count, entry.OwnerId));
                    }
                }
            }
        }

        [Serializable]
        public class TaskBoardManagerData
        {
            public int BoardPosX;
            public int BoardPosY;
            public int BoardPosZ;
            public List<DeliveredItemEntry> DeliveredItems = new List<DeliveredItemEntry>();
        }

        [Serializable]
        public class DeliveredItemEntry
        {
            public int OwnerId;
            public int ItemId;
            public int Count;
        }
    }
}
