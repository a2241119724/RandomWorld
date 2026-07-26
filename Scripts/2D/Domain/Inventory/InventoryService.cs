namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 库存服务 — 纯 C# 领域服务，封装库存网格的所有数据操作。
    ///
    /// 职责：
    ///   - 管理 InventoryGrid（物品存储、位置索引、类型索引）
    ///   - 委托 InventoryStackingService 处理堆叠计算
    ///   - 委托 InventoryFoodReservationService 处理食物预留
    ///   - 委托 InventoryTakeReservationService 处理取物预留
    ///   - 通过可注入的 eventPublisher 发布 InventoryGridChangedEvent
    ///
    /// 不依赖 UnityEngine，不依赖 AWorker、ItemMap、TileMap。
    /// 所有位置使用 GameGridPosition。
    ///
    /// 用法：
    ///   var service = new InventoryService("InventoryManager", gridWidth, gridHeight);
    ///   service.AddItem(position, itemId, count);
    ///   int total = service.GetTotalCount(itemId);
    /// </summary>
    public sealed class InventoryService
    {
        private readonly InventoryGrid grid;
        private readonly InventoryStackingService stackingService;
        private readonly InventoryFoodReservationService foodReservationService;
        private readonly InventoryTakeReservationService takeReservationService;
        private readonly string managerName;
        private readonly Action<IGameEvent> eventPublisher;

        /// <summary>
        /// 创建库存服务实例。
        /// </summary>
        /// <param name="managerName">来源管理器名称（用于事件中的 ManagerName 字段）</param>
        /// <param name="gridWidth">网格宽度（通常与地图宽度一致）</param>
        /// <param name="gridHeight">网格高度（通常与地图高度一致）</param>
        /// <param name="cellCapacity">单个格子的容量上限</param>
        /// <param name="itemTypeResolver">物品 ID → 物品类型枚举值的转换函数（可选，用于类型索引）</param>
        /// <param name="eventPublisher">事件发布委托（可选，默认发布到 EventBus.Instance）</param>
        public InventoryService(
            string managerName,
            int gridWidth,
            int gridHeight,
            int cellCapacity = 1000,
            Func<int, int> itemTypeResolver = null,
            Action<IGameEvent> eventPublisher = null)
        {
            this.managerName = managerName ?? "InventoryService";
            this.grid = new InventoryGrid(gridWidth, gridHeight, cellCapacity, itemTypeResolver);
            this.stackingService = new InventoryStackingService();
            this.foodReservationService = new InventoryFoodReservationService();
            this.takeReservationService = new InventoryTakeReservationService();
            this.eventPublisher = eventPublisher ?? (e => EventBus.Instance.PublishInternal(e));
        }

        // ---- 内部访问器（供 InventoryManager 适配层使用） ----

        internal InventoryGrid Grid
        {
            get { return this.grid; }
        }

        internal InventoryStackingService Stacking
        {
            get { return this.stackingService; }
        }

        internal InventoryFoodReservationService FoodReservation
        {
            get { return this.foodReservationService; }
        }

        internal InventoryTakeReservationService TakeReservation
        {
            get { return this.takeReservationService; }
        }

        // ---- 格子管理 ----

        /// <summary>
        /// 确保指定位置存在格子（如果不存在则创建）。
        /// </summary>
        public InventoryCell EnsureCell(GameGridPosition position, int capacity = 1000)
        {
            return this.grid.AddCell(position, capacity);
        }

        /// <summary>
        /// 获取指定位置的格子，不存在返回 null。
        /// </summary>
        public InventoryCell GetCell(GameGridPosition position)
        {
            return this.grid.GetCell(position);
        }

        /// <summary>
        /// 检查指定位置是否有格子。
        /// </summary>
        public bool HasCell(GameGridPosition position)
        {
            return this.grid.HasCell(position);
        }

        // ---- 物品操作 ----

        /// <summary>
        /// 向指定位置添加物品。
        /// 如果格子不存在则自动创建。
        /// </summary>
        /// <returns>是否添加成功</returns>
        public bool AddItem(GameGridPosition position, int itemId, int count)
        {
            if (count <= 0)
            {
                return false;
            }

            InventoryCell cell = this.EnsureCell(position);

            if (!cell.CanAdd(itemId, count))
            {
                return false;
            }

            int oldCount = cell.Count;
            bool success = this.grid.AddItem(position, itemId, count);

            if (success)
            {
                cell = this.grid.GetCell(position);
                this.PublishChange(
                    oldCount == 0 ? InventoryChangeType.Added : InventoryChangeType.CountChanged,
                    position,
                    cell != null ? cell.ItemId : itemId,
                    cell != null ? cell.Count : oldCount + count,
                    cell != null ? cell.Capacity : 1000);
            }

            return success;
        }

        /// <summary>
        /// 从指定位置取走物品。
        /// </summary>
        /// <returns>实际取走的数量</returns>
        public int TakeItem(GameGridPosition position, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null)
            {
                return 0;
            }

            int taken = this.grid.TakeItem(position, count);

            if (taken > 0)
            {
                cell = this.grid.GetCell(position);
                bool isCleared = cell == null || cell.IsEmpty;
                this.PublishChange(
                    isCleared ? InventoryChangeType.Cleared : InventoryChangeType.CountChanged,
                    position,
                    isCleared ? -1 : (cell != null ? cell.ItemId : -1),
                    cell != null ? cell.Count : 0,
                    cell != null ? cell.Capacity : 1000);
            }

            return taken;
        }

        /// <summary>
        /// 清空指定位置的所有物品。
        /// </summary>
        public void ClearCell(GameGridPosition position)
        {
            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null || cell.IsEmpty)
            {
                return;
            }

            int clearedItemId = cell.ItemId;
            int count = cell.Count;
            int capacity = cell.Capacity;

            // 通过 TakeItem 清空格子（InventoryGrid 自动将空格子加入 id=-1 索引）
            this.grid.TakeItem(position, count);

            this.PublishChange(
                InventoryChangeType.Cleared,
                position,
                clearedItemId,
                0,
                capacity);
        }

        /// <summary>
        /// 将物品从一个 ID 转移到另一个 ID（同一位置）。
        /// 内部通过 TakeItem + AddItem 完成，InventoryGrid 自动维护所有索引。
        /// </summary>
        public void TransferItem(GameGridPosition position, int oldItemId, int newItemId)
        {
            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null || cell.IsEmpty)
            {
                return;
            }

            int count = cell.Count;
            int capacity = cell.Capacity;

            // 先清空（自动将位置移到 id=-1 索引）
            this.grid.TakeItem(position, count);
            // 再添加新物品（自动从 id=-1 移到新物品索引）
            this.grid.AddItem(position, newItemId, count);

            this.PublishChange(
                InventoryChangeType.Added,
                position,
                newItemId,
                count,
                capacity);
        }

        // ---- 查询操作 ----

        /// <summary>
        /// 获取指定物品 ID 的总数量。
        /// </summary>
        public int GetTotalCount(int itemId)
        {
            return this.grid.GetTotalCount(itemId);
        }

        /// <summary>
        /// 获取指定 ID 物品的所有位置。
        /// </summary>
        public IReadOnlyList<GameGridPosition> GetPositionsById(int itemId)
        {
            return this.grid.GetPositionsById(itemId);
        }

        /// <summary>
        /// 获取指定类型物品的所有位置。
        /// </summary>
        public IReadOnlyList<GameGridPosition> GetPositionsByType(int itemType)
        {
            return this.grid.GetPositionsByType(itemType);
        }

        /// <summary>
        /// 查找放置物品的最佳位置。
        /// 优先选择已有同物品的格子（堆叠），其次选择空格子。
        /// </summary>
        public GameGridPosition FindBestPositionForItem(int itemId, int count)
        {
            return this.grid.FindBestPositionForItem(itemId, count);
        }

        /// <summary>
        /// 获取指定位置的资源信息快照。
        /// </summary>
        public ResourceInfo GetResourceInfo(GameGridPosition position)
        {
            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null || cell.IsEmpty)
            {
                return new ResourceInfo(-1, 0);
            }

            return cell.GetResourceInfo();
        }

        // ---- 容量查询 ----

        /// <summary>
        /// 获取指定位置格子的可用容量。
        /// </summary>
        public int GetAvailableCapacity(GameGridPosition position, int reservedCount = 0)
        {
            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null)
            {
                return 0;
            }

            return this.stackingService.GetAvailableCapacity(cell.Capacity, cell.Count, reservedCount);
        }

        /// <summary>
        /// 检查指定位置是否可以放置指定数量的物品。
        /// </summary>
        public bool CanPlaceItem(GameGridPosition position, int itemId, int count, int reservedCount = 0)
        {
            InventoryCell cell = this.grid.GetCell(position);
            if (cell == null)
            {
                return false;
            }

            if (cell.IsEmpty)
            {
                return count <= this.stackingService.GetAvailableCapacity(cell.Capacity, 0, reservedCount);
            }

            if (cell.ItemId != itemId)
            {
                return false;
            }

            return this.stackingService.CanPlaceAll(count, this.GetAvailableCapacity(position, reservedCount));
        }

        // ---- 网格信息 ----

        /// <summary>
        /// 网格中的格子总数。
        /// </summary>
        public int CellCount
        {
            get { return this.grid.CellCount; }
        }

        /// <summary>
        /// 来源管理器名称。
        /// </summary>
        public string ManagerName
        {
            get { return this.managerName; }
        }

        // ---- 内部方法 ----

        private void PublishChange(
            InventoryChangeType changeType,
            GameGridPosition position,
            int itemId,
            int count,
            int capacity)
        {
            this.eventPublisher(new InventoryGridChangedEvent
            {
                ChangeType = changeType,
                Position = position,
                ItemId = itemId,
                Count = count,
                Capacity = capacity,
                ManagerName = this.managerName,
            });
        }
    }
}
