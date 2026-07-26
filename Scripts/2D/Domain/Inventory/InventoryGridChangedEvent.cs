namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 库存网格变更事件（纯数据，不含 UI 格式化字符串）。
    /// 替代 InventoryCellChangedEvent 中携带 CellInfo 字符串的模式，
    /// 让展示层自行决定如何格式化显示。
    ///
    /// 与 InventoryCellChangedEvent 的关系：
    ///   - 本事件由 InventoryService 发布，携带结构化数据
    ///   - InventoryCellChangedEvent 保留兼容，由 InventoryManager 在适配层发布
    ///   - 新订阅者应优先使用本事件
    /// </summary>
    public sealed class InventoryGridChangedEvent : IGameEvent
    {
        /// <summary>变更类型</summary>
        public InventoryChangeType ChangeType;

        /// <summary>格子位置（Domain 坐标）</summary>
        public GameGridPosition Position;

        /// <summary>物品 ID（移除时可能为 -1）</summary>
        public int ItemId;

        /// <summary>变更后的数量</summary>
        public int Count;

        /// <summary>变更后的格子总容量</summary>
        public int Capacity;

        /// <summary>来源管理器名称（用于 ItemInfoUI 匹配面板）</summary>
        public string ManagerName;
    }

    /// <summary>
    /// 库存变更类型枚举。
    /// </summary>
    public enum InventoryChangeType
    {
        /// <summary>物品添加</summary>
        Added,

        /// <summary>物品移除</summary>
        Removed,

        /// <summary>物品数量变化（堆叠增减）</summary>
        CountChanged,

        /// <summary>格子被清空</summary>
        Cleared,
    }
}
