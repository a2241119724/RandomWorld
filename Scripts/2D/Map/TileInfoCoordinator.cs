namespace LAB2D.Map
{
    /// <summary>
    /// 瓦片信息显示协调器 — 从 BaseTileMap.alreadyShowMap 静态字段提取。
    /// 协调多个 BaseTileMap 子类竞争显示 TileInfoUI 时的互斥。
    /// </summary>
    public sealed class TileInfoCoordinator
    {
        /// <summary>
        /// 当前占用 TileInfoUI 显示的 Map 类型名称。空字符串表示无占用。
        /// </summary>
        public string ActiveMapType { get; set; } = string.Empty;
    }
}
