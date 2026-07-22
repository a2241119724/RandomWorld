namespace LAB2D.Core.Seek
{
    using LAB2D;
    using System.Threading;
    using UnityEngine;

    /// <summary>
    /// 静态可步行性缓存 — 避免 A* 寻路时每个邻居都向主线程派发 IsCanReach 检查。
    ///
    /// 并发设计：
    ///   主线程 Refresh() 写入新数组，完成后原子交换引用。
    ///   后台线程 IsWalkable() 始终读取已发布完成的旧数组。
    ///   双缓冲消除 Refresh 期间的读写竞争。
    /// </summary>
    public static class WalkabilityCache
    {
        private static bool[,] readCache;
        private static int width;
        private static int height;

        public static bool IsInitialized => readCache != null;

        public static void Initialize(int w, int h)
        {
            if (readCache != null)
            {
                return;
            }

            width = w;
            height = h;
            readCache = new bool[width, height];
        }

        /// <summary>
        /// 刷新整个地图的可步行性缓存 — 必须在主线程调用。
        /// 写入完成前不发布，后台线程始终读取上一次完整快照。
        /// </summary>
        public static void Refresh()
        {
            if (readCache == null)
            {
                var tileMap = Core.ServiceLocator.Get<TileMap>().TileMapDataLAB;
                Initialize(tileMap.Width, tileMap.Height);
            }

            // 在新数组上写入，避免后台线程看到部分刷新的数据
            bool[,] writeCache = new bool[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    writeCache[x, y] = ASeek.IsCanReach(new Vector3Int(x, y, 0));
                }
            }

            // 原子交换：引用赋值在 .NET 中是原子的
            Thread.MemoryBarrier();
            readCache = writeCache;
        }

        /// <summary>
        /// 检查坐标是否可行走 — 可在任何线程安全调用。
        /// </summary>
        public static bool IsWalkable(int x, int y)
        {
            bool[,] cache = readCache;
            if (cache == null || x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            return cache[x, y];
        }
    }
}
