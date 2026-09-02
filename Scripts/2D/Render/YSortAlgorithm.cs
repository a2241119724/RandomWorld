namespace LAB2D.Render
{
    using System;

    /// <summary>
    /// 按底端 Y 排序分配 sortingOrder 的纯函数算法（无 UnityEngine 依赖，可单测）。
    /// 规则：底端 y 大（屏幕上方/远处）→ sortingOrder 小（先绘制，被覆盖）；
    ///       底端 y 小（屏幕下方/近处）→ sortingOrder 大（后绘制，盖住上方）。
    /// 结果：按 bottomY 降序赋 0..N-1，保证唯一。
    /// </summary>
    public static class YSortAlgorithm
    {
        /// <summary>
        /// 按 bottomY 降序分配唯一 sortingOrder。
        /// 相同 bottomY 时保持原索引顺序（稳定），视觉上并列时不会抖动。
        /// </summary>
        /// <param name="bottomY">每个条目的底端世界 y，与返回数组索引一一对应。</param>
        /// <returns>与输入同长度的 order 数组（orders[i] 是第 i 个条目的 sortingOrder）。</returns>
        public static int[] AssignOrders(float[] bottomY)
        {
            int n = bottomY == null ? 0 : bottomY.Length;
            int[] orders = new int[n];
            if (n <= 1)
            {
                return orders; // 0 或 1 个条目时 order 恒为 0
            }

            int[] indices = new int[n];
            for (int i = 0; i < n; i++)
            {
                indices[i] = i;
            }

            // 降序稳定排序：bottomY 大的索引排前（获得小 order）；
            // bottomY 相等时按原索引升序保持稳定。
            System.Array.Sort(indices, (a, b) =>
            {
                int cmp = bottomY[b].CompareTo(bottomY[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });

            for (int i = 0; i < n; i++)
            {
                orders[indices[i]] = i;
            }

            return orders;
        }

        /// <summary>
        /// <see cref="AssignOrders(float[])"/> 的缓冲复用版本：调用方持有 indices/orders 缓冲跨帧复用，
        /// 比较器用静态单例（比较键通过字段传入，避免捕获变量的闭包+委托每次分配）。
        /// WorldYSortManager.LateUpdate 每帧高频路径用此重载，实现零分配；排序规则与纯函数版完全一致。
        /// </summary>
        /// <param name="bottomY">底端 y 数组（长度 ≥ count）。</param>
        /// <param name="count">条目数。</param>
        /// <param name="indices">复用的索引缓冲（不足时扩容）。</param>
        /// <param name="orders">复用的结果缓冲（不足时扩容），orders[i] 为第 i 个条目的 sortingOrder。</param>
        public static void AssignOrders(float[] bottomY, int count, ref int[] indices, ref int[] orders)
        {
            if (orders == null || orders.Length < count)
            {
                orders = new int[Math.Max(count, 64)];
            }

            if (count <= 1)
            {
                if (count == 1)
                {
                    orders[0] = 0;
                }

                return;
            }

            if (indices == null || indices.Length < count)
            {
                indices = new int[Math.Max(count, 64)];
            }

            for (int i = 0; i < count; i++)
            {
                indices[i] = i;
            }

            // 与纯函数版相同的降序稳定比较（bottomY 大的索引排前；相等按原索引升序）
            SharedComparer.bottomY = bottomY;
            System.Array.Sort(indices, 0, count, SharedComparer);

            for (int i = 0; i < count; i++)
            {
                orders[indices[i]] = i;
            }
        }

        /// <summary>静态比较器单例：主线程 LateUpdate 单线程调用，比较键经字段传递零分配。</summary>
        private static readonly BottomYComparer SharedComparer = new ();

        private sealed class BottomYComparer : System.Collections.Generic.IComparer<int>
        {
            internal float[] bottomY;

            public int Compare(int a, int b)
            {
                int cmp = this.bottomY[b].CompareTo(this.bottomY[a]);
                return cmp != 0 ? cmp : a.CompareTo(b);
            }
        }
    }
}
