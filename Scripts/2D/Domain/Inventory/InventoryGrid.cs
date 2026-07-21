namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;
    using System.Collections.Generic;

    /// <summary>
    /// 库存网格 — 纯 C# 数据模型，管理所有库存格子的位置索引。
    /// 提供按位置、按物品 ID、按物品类型的快速查询。
    /// 不依赖 UnityEngine，不依赖 ItemMap 或 TileMap。
    /// </summary>
    public sealed class InventoryGrid
    {
        private readonly Dictionary<GameGridPosition, InventoryCell> cellsByPos;
        private readonly Dictionary<int, List<GameGridPosition>> positionsById;
        private readonly Dictionary<int, List<GameGridPosition>> positionsByType;
        private readonly System.Func<int, int> itemTypeResolver;

        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; private set; }

        public InventoryGrid(int width, int height, int cellCapacity = 1000, System.Func<int, int> itemTypeResolver = null)
        {
            this.Width = width;
            this.Height = height;
            this.itemTypeResolver = itemTypeResolver;
            this.cellsByPos = new Dictionary<GameGridPosition, InventoryCell>();
            this.positionsById = new Dictionary<int, List<GameGridPosition>>();
            this.positionsByType = new Dictionary<int, List<GameGridPosition>>();
        }

        public InventoryCell AddCell(GameGridPosition position, int capacity = 1000)
        {
            if (this.cellsByPos.ContainsKey(position))
            {
                return this.cellsByPos[position];
            }

            InventoryCell cell = new InventoryCell(capacity);
            this.cellsByPos[position] = cell;
            this.CellCount++;
            return cell;
        }

        public bool RemoveCell(GameGridPosition position)
        {
            InventoryCell cell = this.GetCell(position);
            if (cell == null)
            {
                return false;
            }

            if (!cell.IsEmpty)
            {
                this.RemoveFromIndex(cell.ItemId, position);
            }

            this.cellsByPos.Remove(position);
            this.CellCount--;
            return true;
        }

        public InventoryCell GetCell(GameGridPosition position)
        {
            this.cellsByPos.TryGetValue(position, out InventoryCell cell);
            return cell;
        }

        public bool AddItem(GameGridPosition position, int itemId, int amount)
        {
            InventoryCell cell = this.GetCell(position);
            if (cell == null)
            {
                return false;
            }

            bool wasEmpty = cell.IsEmpty;
            int oldItemId = cell.ItemId;

            if (!cell.Add(itemId, amount))
            {
                return false;
            }

            if (wasEmpty)
            {
                this.AddToIndex(itemId, position);
            }
            else if (oldItemId != itemId)
            {
                this.RemoveFromIndex(oldItemId, position);
                this.AddToIndex(itemId, position);
            }

            return true;
        }

        public int TakeItem(GameGridPosition position, int amount)
        {
            InventoryCell cell = this.GetCell(position);
            if (cell == null)
            {
                return 0;
            }

            int taken = cell.Take(amount);
            if (taken > 0 && cell.IsEmpty)
            {
                this.RemoveFromIndex(cell.ItemId < 0 ? -1 : -1, position);
            }

            return taken;
        }

        public IReadOnlyList<GameGridPosition> GetPositionsById(int itemId)
        {
            if (this.positionsById.TryGetValue(itemId, out List<GameGridPosition> list))
            {
                return list;
            }

            return System.Array.Empty<GameGridPosition>();
        }

        public IReadOnlyList<GameGridPosition> GetPositionsByType(int itemType)
        {
            if (this.positionsByType.TryGetValue(itemType, out List<GameGridPosition> list))
            {
                return list;
            }

            return System.Array.Empty<GameGridPosition>();
        }

        public int GetTotalCount(int itemId)
        {
            int total = 0;
            IReadOnlyList<GameGridPosition> positions = this.GetPositionsById(itemId);
            foreach (GameGridPosition pos in positions)
            {
                InventoryCell cell = this.GetCell(pos);
                if (cell != null && cell.ItemId == itemId)
                {
                    total += cell.Count;
                }
            }

            return total;
        }

        public GameGridPosition FindBestPositionForItem(int itemId, int amount)
        {
            // 优先查找已有同物品的格子
            IReadOnlyList<GameGridPosition> positions = this.GetPositionsById(itemId);
            foreach (GameGridPosition pos in positions)
            {
                InventoryCell cell = this.GetCell(pos);
                if (cell != null && cell.CanAdd(itemId, amount))
                {
                    return pos;
                }
            }

            // 查找空格子
            foreach (KeyValuePair<GameGridPosition, InventoryCell> pair in this.cellsByPos)
            {
                if (pair.Value.IsEmpty && pair.Value.CanAdd(itemId, amount))
                {
                    return pair.Key;
                }
            }

            return default;
        }

        public bool HasCell(GameGridPosition position)
        {
            return this.cellsByPos.ContainsKey(position);
        }

        private void AddToIndex(int itemId, GameGridPosition position)
        {
            if (!this.positionsById.TryGetValue(itemId, out List<GameGridPosition> list))
            {
                list = new List<GameGridPosition>();
                this.positionsById[itemId] = list;
            }

            if (!list.Contains(position))
            {
                list.Add(position);
            }

            if (this.itemTypeResolver != null)
            {
                int type = this.itemTypeResolver(itemId);
                if (!this.positionsByType.TryGetValue(type, out List<GameGridPosition> typeList))
                {
                    typeList = new List<GameGridPosition>();
                    this.positionsByType[type] = typeList;
                }

                if (!typeList.Contains(position))
                {
                    typeList.Add(position);
                }
            }
        }

        private void RemoveFromIndex(int itemId, GameGridPosition position)
        {
            if (itemId >= 0 && this.positionsById.TryGetValue(itemId, out List<GameGridPosition> list))
            {
                list.Remove(position);
                if (list.Count == 0)
                {
                    this.positionsById.Remove(itemId);
                }
            }

            if (this.itemTypeResolver != null && itemId >= 0)
            {
                int type = this.itemTypeResolver(itemId);
                if (this.positionsByType.TryGetValue(type, out List<GameGridPosition> typeList))
                {
                    typeList.Remove(position);
                    if (typeList.Count == 0)
                    {
                        this.positionsByType.Remove(type);
                    }
                }
            }
        }
    }
}
