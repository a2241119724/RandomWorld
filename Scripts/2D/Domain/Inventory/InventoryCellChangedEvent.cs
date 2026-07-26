namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// [已废弃] 仓库格子变更事件 — 由 InventoryGridChangedEvent 替代。
    /// 此事件携带 UI 格式化字符串 CellInfo，违反"核心事件不含 UI 字符串"原则。
    /// 自 2026-07 起，InventoryManager 已停止发布此事件，ItemInfoUI 已迁移至 InventoryGridChangedEvent。
    /// 保留此类仅用于向后兼容和现有测试引用。
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
