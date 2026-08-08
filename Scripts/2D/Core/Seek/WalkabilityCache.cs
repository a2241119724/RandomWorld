namespace LAB2D.Core.Seek
{
    using System.Threading;
    using UnityEngine;

    /// <summary>
    /// 后台寻路使用的可步行性快照。
    /// 地图加载后只进行一次全量构建，运行期间由地图变更入口按格更新。
    /// </summary>
    public static class WalkabilityCache
    {
        private static int[] walkability;
        private static int width;
        private static int height;
        private static int mapInstanceId;
        private static volatile bool isBuilt;

        public static bool IsInitialized => walkability != null;

        public static void Initialize(int newWidth, int newHeight, int newMapInstanceId)
        {
            if (walkability != null
                && width == newWidth
                && height == newHeight
                && mapInstanceId == newMapInstanceId)
            {
                return;
            }

            isBuilt = false;
            width = newWidth;
            height = newHeight;
            mapInstanceId = newMapInstanceId;
            Volatile.Write(ref walkability, new int[checked(width * height)]);
        }

        /// <summary>
        /// 确保初始快照已经构建。必须在主线程调用。
        /// 该全量扫描在一个地图生命周期内只执行一次。
        /// </summary>
        public static void EnsureBuilt()
        {
            if (isBuilt)
            {
                return;
            }

            int[] cache = Volatile.Read(ref walkability);
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    cache[rowOffset + x] = ASeek.IsCanReach(new Vector3Int(x, y, 0)) ? 1 : 0;
                }
            }

            isBuilt = true;
        }

        /// <summary>
        /// 整张地图数据被替换后使快照失效；下一次寻路会重新构建一次。
        /// </summary>
        public static void Invalidate()
        {
            isBuilt = false;
        }

        /// <summary>
        /// 地图内容改变后刷新一个格子。必须在主线程调用。
        /// 缓存尚未完成初始构建时无需处理，首次构建会读取最新地图状态。
        /// </summary>
        public static void UpdateCell(Vector3Int position)
        {
            int[] cache = Volatile.Read(ref walkability);
            if (!isBuilt || cache == null || !IsInBounds(position.x, position.y))
            {
                return;
            }

            int index = (position.y * width) + position.x;
            Volatile.Write(ref cache[index], ASeek.IsCanReach(position) ? 1 : 0);
        }

        /// <summary>
        /// 检查坐标是否可行走，可从任意后台线程调用。
        /// </summary>
        public static bool IsWalkable(int x, int y)
        {
            int[] cache = Volatile.Read(ref walkability);
            if (!isBuilt || cache == null || !IsInBounds(x, y))
            {
                return false;
            }

            return Volatile.Read(ref cache[(y * width) + x]) != 0;
        }

        private static bool IsInBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
}
