namespace LAB2D.Map
{
    using System.Collections;

    /// <summary>
    /// 地形生成策略接口。
    /// 实现此接口即可插入自定义地图生成算法（Perlin Noise、波函数坍缩、Cellular Automata 等）。
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>
        /// 在地图上散布初始种子点（地形类型 ID）。
        /// 种子点用于后续填充算法确定各区域的地形类型。
        /// </summary>
        /// <param name="tiles">地图瓦片数据（int[,] 二维数组，0 = Default/未初始化）。</param>
        /// <param name="randomCount">种子点数量。</param>
        /// <param name="height">地图高度。</param>
        /// <param name="width">地图宽度。</param>
        /// <returns>协程迭代器（支持分帧执行）。</returns>
        IEnumerator ScatterSeeds(int[,] tiles, int randomCount, int height, int width);

        /// <summary>
        /// 将 Default（值为 0）的格子用距离最近的非 Default 种子点地形类型填充。
        /// </summary>
        /// <param name="tiles">包含种子点的地图瓦片数据。</param>
        /// <param name="height">地图高度。</param>
        /// <param name="width">地图宽度。</param>
        /// <returns>协程迭代器（支持分帧执行）。</returns>
        IEnumerator Fill(int[,] tiles, int height, int width);
    }
}
