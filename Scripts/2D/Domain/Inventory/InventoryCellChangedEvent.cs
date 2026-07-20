namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 仓库格子变更事件。
    /// InventoryManager 在物品添加、移除、预占变更后发布此事件。
    /// ItemInfoUI 等展示层订阅此事件以刷新实时信息面板。
    /// </summary>
    public sealed class InventoryCellChangedEvent : IGameEvent
    {
        /// <summary>
        /// 变更来源管理器名称（用于 ItemInfoUI 匹配当前选中面板）。
        /// </summary>
        public string ManagerName;

        /// <summary>
        /// 格子 X 坐标（地图网格）。
        /// </summary>
        public int GridX;

        /// <summary>
        /// 格子 Y 坐标（地图网格）。
        /// </summary>
        public int GridY;

        /// <summary>
        /// 格子的格式化信息文本。
        /// </summary>
        public string CellInfo;
    }
}
