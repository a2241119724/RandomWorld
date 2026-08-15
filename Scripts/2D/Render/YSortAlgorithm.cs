namespace LAB2D.Render
{
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
    }
}
