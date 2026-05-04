namespace LAB2D
{
    using System.Collections.Concurrent;

    /// <summary>
    /// Spend对象池 — 多个A*搜索共享Spend[,]数组, 避免每个Worker创建完整地图的Spend对象
    /// 通过ConcurrentBag实现线程安全的租借/归还, 池按需增长
    /// </summary>
    public static class SpendPool
    {
        private static readonly ConcurrentBag<Spend[,]> Pool = new ();
        private static int width;
        private static int height;
        private static bool initialized;

        public static void Initialize(int w, int h)
        {
            if (initialized)
            {
                return;
            }

            width = w;
            height = h;
            initialized = true;
        }

        /// <summary>
        /// 租借一个Spend数组. 如果池中有空闲则复用, 否则创建新的.
        /// </summary>
        public static Spend[,] Rent()
        {
            if (Pool.TryTake(out Spend[,] spends))
            {
                return spends;
            }

            var newSpends = new Spend[width, height];
            for (short i = 0; i < width; i++)
            {
                for (short j = 0; j < height; j++)
                {
                    newSpends[i, j] = new Spend(i, j);
                }
            }

            return newSpends;
        }

        /// <summary>
        /// 归还Spend数组到池中供后续复用
        /// </summary>
        public static void Return(Spend[,] spends)
        {
            if (spends != null)
            {
                Pool.Add(spends);
            }
        }
    }
}
