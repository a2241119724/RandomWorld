namespace LAB2D.Core.Seek
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 静态可步行性缓存 — 避免A*寻路时每个邻居都向主线程派发IsCanReach检查
    /// 主线程写入, 后台线程只读(bool值类型数组多线程读取安全)
    /// </summary>
    public static class WalkabilityCache
    {
        private static bool[,] cache;
        private static int width;
        private static int height;

        public static bool IsInitialized => cache != null;

        public static void Initialize(int w, int h)
        {
            if (cache != null)
            {
                return;
            }

            width = w;
            height = h;
            cache = new bool[width, height];
        }

        /// <summary>
        /// 刷新整个地图的可步行性缓存 — 必须在主线程调用
        /// </summary>
        public static void Refresh()
        {
            if (cache == null)
            {
                var tileMap = TileMap.Instance.TileMapDataLAB;
                Initialize(tileMap.Width, tileMap.Height);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cache[x, y] = ASeek.IsCanReach(new Vector3Int(x, y, 0));
                }
            }
        }

        /// <summary>
        /// 检查坐标是否可行走 — 可在任何线程安全调用
        /// </summary>
        public static bool IsWalkable(int x, int y)
        {
            if (cache == null || x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            return cache[x, y];
        }
    }
}
