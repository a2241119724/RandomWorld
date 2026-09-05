namespace LAB2D.Core.Seek
{
    using System.Threading;
    using UnityEngine;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;

    /// <summary>
    /// 后台寻路使用的可步行性快照。
    /// 地图加载后只进行一次全量构建，运行期间由地图变更入口按格更新。
    /// 线程安全模型：数组与其尺寸绑定为不可变 <see cref="Snapshot"/> 单次 volatile 发布——
    /// 后台 <see cref="IsWalkable"/> 一次读引用即拿到 (cells, width, height) 一致视图，
    /// 不存在「新边界 + 旧数组」的越界窗口（换图尺寸变化瞬间曾可能 IndexOutOfRange 炸后台寻路线程）。
    /// </summary>
    public static class WalkabilityCache
    {
        /// <summary>快照：格值数组与其尺寸绑定发布，发布后 Cells 内容可原地按格更新（单 int 写原子）。</summary>
        private class Snapshot
        {
            public readonly int[] Cells;
            public readonly int Width;
            public readonly int Height;

            public Snapshot(int width, int height)
            {
                this.Cells = new int[checked(width * height)];
                this.Width = width;
                this.Height = height;
            }
        }

        private static volatile Snapshot current;
        private static int mapInstanceId;
        private static volatile bool isBuilt;

        public static bool IsInitialized => current != null;

        /// <summary>
        /// 应用退出时释放可步行性快照数组，帮助 GC 回收。
        /// </summary>
        public static void Clear()
        {
            current = null;
            isBuilt = false;
            mapInstanceId = 0;
        }

        public static void Initialize(int newWidth, int newHeight, int newMapInstanceId)
        {
            Snapshot snapshot = current;
            if (snapshot != null
                && snapshot.Width == newWidth
                && snapshot.Height == newHeight
                && mapInstanceId == newMapInstanceId)
            {
                return;
            }

            isBuilt = false;
            mapInstanceId = newMapInstanceId;
            current = new Snapshot(newWidth, newHeight);
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

            Snapshot snapshot = current;
            if (snapshot == null)
            {
                return;
            }

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            int[] cache = snapshot.Cells;
            for (int y = 0; y < snapshot.Height; y++)
            {
                int rowOffset = y * snapshot.Width;
                for (int x = 0; x < snapshot.Width; x++)
                {
                    cache[rowOffset + x] = ASeek.IsCanReach(new Vector3Int(x, y, 0)) ? 1 : 0;
                }
            }

            isBuilt = true;
            sw.Stop();
            // WalkabilityCache 与物理碰撞体失配是历史卡墙根因之一 → 构建完成后记录尺寸与耗时。
            AWorkerTask.LogProvider(
                $"[MapDiag] WalkabilityCache 构建完成 size={snapshot.Width}x{snapshot.Height} cells={cache.Length} elapsed={sw.ElapsedMilliseconds}ms",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 整张地图数据被替换后使快照失效；下一次寻路会重新构建一次。
        /// </summary>
        public static void Invalidate()
        {
            isBuilt = false;
            AWorkerTask.LogProvider(
                $"[MapDiag] WalkabilityCache 失效 size={(current != null ? current.Width : 0)}x{(current != null ? current.Height : 0)}",
                LogManager.LogLevelEnum.Trace);
        }

        /// <summary>
        /// 地图内容改变后刷新一个格子。必须在主线程调用。
        /// 缓存尚未完成初始构建时无需处理，首次构建会读取最新地图状态。
        /// </summary>
        public static void UpdateCell(Vector3Int position)
        {
            Snapshot snapshot = current;
            if (!isBuilt || snapshot == null)
            {
                // 未构建期的跳过是设计内正常路径（首次构建会读取最新地图状态）
                return;
            }

            if (position.x < 0 || position.y < 0
                || position.x >= snapshot.Width || position.y >= snapshot.Height)
            {
                // 越界跳过是异常信号（地图外写入），保留观测（全局 1s 节流——逐格 key 在批量
                // 异常写入时等同无节流，见 bug-fixes.md 2026-09-06）。
                AWorkerTask.LogProviderThrottled(
                    "CacheSkipOOB", 1f,
                    () => $"[MapDiag] 缓存更新跳过 pos=({position.x},{position.y}) 原因=越界 size={snapshot.Width}x{snapshot.Height}",
                    LogManager.LogLevelEnum.Trace);
                return;
            }

            // 阻挡格（不可通）逐格日志已删（2026-08-16 卡床排查已结案；开局地图注册曾 2 秒
            // 内 1 万格批量写入——逐格 key 节流失效，每格 key 插值+闭包构造是开局卡顿贡献者）。
            int index = (position.y * snapshot.Width) + position.x;
            Volatile.Write(ref snapshot.Cells[index], ASeek.IsCanReach(position) ? 1 : 0);
        }

        /// <summary>
        /// 检查坐标是否可行走，可从任意后台线程调用。
        /// 未构建/未初始化/越界一律返回 false（宁可寻路失败不误穿墙的安全方向）。
        /// </summary>
        public static bool IsWalkable(int x, int y)
        {
            Snapshot snapshot = current;
            if (!isBuilt || snapshot == null)
            {
                return false;
            }

            if (x < 0 || y < 0 || x >= snapshot.Width || y >= snapshot.Height)
            {
                return false;
            }

            return Volatile.Read(ref snapshot.Cells[(y * snapshot.Width) + x]) != 0;
        }
    }
}
