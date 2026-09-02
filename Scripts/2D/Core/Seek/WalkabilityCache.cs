namespace LAB2D.Core.Seek
{
    using System.Threading;
    using UnityEngine;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;

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

        /// <summary>
        /// 应用退出时释放可步行性快照数组，帮助 GC 回收。
        /// </summary>
        public static void Clear()
        {
            walkability = null;
            isBuilt = false;
            width = 0;
            height = 0;
            mapInstanceId = 0;
        }

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
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    cache[rowOffset + x] = ASeek.IsCanReach(new Vector3Int(x, y, 0)) ? 1 : 0;
                }
            }

            isBuilt = true;
            sw.Stop();
            // WalkabilityCache 与物理碰撞体失配是历史卡墙根因之一 → 构建完成后记录尺寸与耗时。
            AWorkerTask.LogProvider(
                $"[MapDiag] WalkabilityCache 构建完成 size={width}x{height} cells={width * height} elapsed={sw.ElapsedMilliseconds}ms",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 整张地图数据被替换后使快照失效；下一次寻路会重新构建一次。
        /// </summary>
        public static void Invalidate()
        {
            isBuilt = false;
            AWorkerTask.LogProvider(
                $"[MapDiag] WalkabilityCache 失效 size={width}x{height}",
                LogManager.LogLevelEnum.Trace);
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
                // 卡床排查 2026-08-16：UpdateCell 被跳过 → 缓存停留初始构建值（可通）。
                // 记录跳过原因（isBuilt/越界），用于定位「缓存判可通而物理有碰撞体」分叉根因。
                if (!isBuilt)
                {
                    AWorkerTask.LogProviderThrottled(
                        $"CacheSkipNotBuilt|{position.x},{position.y}", 1f,
                        // 惰性求值：地图批量写入（建房预注册/地图生成）会高频进入，被节流时不构造插值串
                        () => $"[MapDiag] 缓存更新跳过 pos=({position.x},{position.y}) 原因=未构建",
                        LogManager.LogLevelEnum.Trace);
                }
                else if (!IsInBounds(position.x, position.y))
                {
                    AWorkerTask.LogProviderThrottled(
                        $"CacheSkipOOB|{position.x},{position.y}", 1f,
                        // 惰性求值：同上，批量写入路径
                        () => $"[MapDiag] 缓存更新跳过 pos=({position.x},{position.y}) 原因=越界 size={width}x{height}",
                        LogManager.LogLevelEnum.Trace);
                }

                return;
            }

            int index = (position.y * width) + position.x;
            bool reach = ASeek.IsCanReach(position);
            Volatile.Write(ref cache[index], reach ? 1 : 0);
            // 卡床排查 2026-08-16：阻挡格（不可通）写入记录，确认 UpdateCell 确实生效。
            // 若此处已写「不可通」但压缩路径仍穿过该格 → 另有机制回写可通，需继续追。
            if (!reach)
            {
                AWorkerTask.LogProviderThrottled(
                    $"CacheWriteBlock|{position.x},{position.y}", 1f,
                    // 惰性求值：阻挡格批量写入（建房/拆除）时逐格进入，被节流时不构造插值串
                    () => $"[MapDiag] 缓存更新 pos=({position.x},{position.y}) 判不可通",
                    LogManager.LogLevelEnum.Trace);
            }
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
