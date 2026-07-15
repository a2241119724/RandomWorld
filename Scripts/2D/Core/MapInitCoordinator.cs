namespace LAB2D.Core
{
    /// <summary>
    /// 地图初始化协调器 — 从 Lock.IsCompleteTileMap 提取的独立服务。
    /// 协程通过 WaitUntil 等待地图就绪，地图生成完成后设为 true。
    /// </summary>
    public sealed class MapInitCoordinator
    {
        /// <summary>
        /// 地图瓦片是否加载完成。TileMap/AchieveManager 设为 true，
        /// WaveManager/ResourceMap/EnemyManager 等协程等待。
        /// </summary>
        public bool IsComplete { get; set; }
    }
}
