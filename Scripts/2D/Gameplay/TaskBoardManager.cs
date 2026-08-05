namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using UnityEngine;

    /// <summary>
    /// 任务发布栏管理器 — 管理悬赏物品的集中交付和取回。
    ///
    /// 职责：
    /// - 存储任务栏世界坐标
    /// - 在任务栏周围搜索可放置物品的空地（用于 CarryToBoard 交付）
    /// - 检查任务栏周围是否有属于某 Worker 的掉落物（用于 PickUpFromBoard 决策）
    ///
    /// 任务栏位置在地图初始化完成后自动选择（地图中心附近第一个可到达空地）。
    /// </summary>
    public class TaskBoardManager : Singleton<TaskBoardManager>
    {
        /// <summary>扫描任务栏周围掉落物的最大半径</summary>
        private const int ScanRadius = 10;

        /// <summary>搜索交付空地的最大半径</summary>
        private const int DeliverySearchRadius = 15;

        /// <summary>任务栏世界坐标（地图格子坐标）</summary>
        public Vector3Int BoardPosition { get; private set; }

        /// <summary>任务栏是否已初始化</summary>
        public bool IsInitialized => this.BoardPosition != default;

        /// <summary>
        /// 初始化任务栏位置。
        /// 在地图初始化完成后调用，通过 AvailablePositionProvider 在地图中心附近搜索可用位置。
        /// </summary>
        /// <param name="mapCenter">地图中心坐标（地图格子坐标）</param>
        public void InitPosition(Vector3Int mapCenter)
        {
            if (this.IsInitialized)
            {
                return;
            }

            Vector3Int found = Character.Worker.Task.AWorkerTask.AvailablePositionProvider(mapCenter, DeliverySearchRadius, false);
            if (found != default)
            {
                this.BoardPosition = found;
                this.PlaceBoardIcon(found);
                Debug.Log($"[TaskBoard] 任务栏位置初始化完成: ({found.x}, {found.y})");
                Character.Worker.Task.AWorkerTask.LogProvider(
                    $"[TaskBoard] 任务栏位置初始化完成: ({found.x}, {found.y})",
                    LogManager.LogLevelEnum.Info);
            }
            else
            {
                // 退而求其次：使用地图中心
                this.BoardPosition = mapCenter;
                this.PlaceBoardIcon(mapCenter);
                Debug.LogWarning($"[TaskBoard] 未找到空闲位置，使用地图中心: ({mapCenter.x}, {mapCenter.y})");
                Character.Worker.Task.AWorkerTask.LogProvider(
                    $"[TaskBoard] 未找到空闲位置，使用地图中心: ({mapCenter.x}, {mapCenter.y})",
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 在地图上放置任务栏图标（BuildMap 层，不会被拾取）。
        /// 同时注册到 BuildMapDataLAB，确保寻路等系统正确识别该位置。
        /// </summary>
        private void PlaceBoardIcon(Vector3Int pos)
        {
            var tile = (UnityEngine.Tilemaps.TileBase)Character.Worker.Task.AWorkerTask.ResourceLoadProvider("Bounty");
            if (tile != null)
            {
                var buildMap = Core.ServiceLocator.Get<BuildMap>();
                buildMap.DirectBuild(pos, tile);

                // DirectBuild 不会写入 BuildMapDataLAB，手动注册以让寻路等系统感知
                var posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                if (!buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                {
                    buildMap.BuildMapDataLAB.PosMap[posLAB] = new BuildMap.BuildTileData("Bounty", true);
                }

                Debug.Log($"[TaskBoard] 任务栏图标已放置到 BuildMap: ({pos.x}, {pos.y})");
            }
        }

        /// <summary>
        /// 获取一个适合放置交付物品的空地（任务栏周围）。
        /// 通过 AvailablePositionProvider 搜索（isDrop=true 确保适合放置掉落物）。
        /// </summary>
        /// <returns>空地坐标，default 表示无可用位置</returns>
        public Vector3Int GetDeliveryPosition()
        {
            if (!this.IsInitialized)
            {
                return default;
            }

            return Character.Worker.Task.AWorkerTask.AvailablePositionProvider(this.BoardPosition, DeliverySearchRadius, true);
        }

        /// <summary>
        /// 检查任务栏周围是否有属于指定 Worker 的掉落物。
        /// </summary>
        /// <param name="workerId">Worker 的 instance ID</param>
        /// <returns>有物品返回 true</returns>
        public bool HasOwnedItemsNearBoard(int workerId)
        {
            if (!this.IsInitialized)
            {
                return false;
            }

            DropManager dropManager = Core.ServiceLocator.Get<DropManager>();
            if (dropManager == null)
            {
                return false;
            }

            for (int dx = -ScanRadius; dx <= ScanRadius; dx++)
            {
                for (int dy = -ScanRadius; dy <= ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(
                        this.BoardPosition.x + dx,
                        this.BoardPosition.y + dy,
                        0);

                    ResourceInfo drop = dropManager.GetDropByAll(pos);
                    if (drop != null && drop.Count > 0 && drop.OwnerId == workerId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取任务栏周围属于指定 Worker 的第一个掉落物位置。
        /// </summary>
        /// <param name="workerId">Worker 的 instance ID</param>
        /// <returns>掉落物位置和资源信息，找不到返回 (default, null)</returns>
        public (Vector3Int position, ResourceInfo resource) FindOwnedItemNearBoard(int workerId)
        {
            if (!this.IsInitialized)
            {
                return (default, null);
            }

            DropManager dropManager = Core.ServiceLocator.Get<DropManager>();
            if (dropManager == null)
            {
                return (default, null);
            }

            // 从近到远搜索
            for (int r = 0; r <= ScanRadius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        // 只检查环边界上的格子（优化性能）
                        if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        {
                            continue;
                        }

                        Vector3Int pos = new Vector3Int(
                            this.BoardPosition.x + dx,
                            this.BoardPosition.y + dy,
                            0);

                        ResourceInfo drop = dropManager.GetDropByAll(pos);
                        if (drop != null && drop.Count > 0 && drop.OwnerId == workerId)
                        {
                            return (pos, drop);
                        }
                    }
                }
            }

            return (default, null);
        }
    }
}
