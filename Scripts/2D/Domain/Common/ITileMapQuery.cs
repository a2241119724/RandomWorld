namespace LAB2D.Domain.Common
{
    /// <summary>
    /// 主地图查询接口 — 暴露外部系统需要的地图位置转换和可行走性检查。
    /// 实现类: TileMap（Map 层）。
    /// </summary>
    public interface ITileMapQuery
    {
        /// <summary>
        /// 将世界坐标转换为地图格子坐标。
        /// </summary>
        GameGridPosition WorldPosToMapPos(GameVector2 worldPos);

        /// <summary>
        /// 检查指定地图位置是否可行走（无碰撞体）。
        /// </summary>
        bool IsCanReach(GameGridPosition posMap);

        /// <summary>
        /// 地图宽度（格子数）。
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 地图高度（格子数）。
        /// </summary>
        int Height { get; }

        /// <summary>
        /// 检查地图坐标是否在地图范围内。
        /// </summary>
        bool IsInBounds(GameGridPosition posMap);
    }
}
