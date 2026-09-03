namespace LAB2D.Domain.Common
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 均匀网格空间哈希（纯 C#，仅主线程使用）。
    /// 惰性全量重建模型：BeginRebuild 清上轮非空桶 → Add 逐个入桶 → 查询。
    /// 桶字典只增 key 不删除，但仅"上轮非空桶"（usedKeys）会被清理与查询，
    /// 空桶堆积上界 = 历史单轮最大非空桶数。
    /// cellSize 取最大查询半径（现役 8）：全部半径下桶覆盖恒 3×3；
    /// 新增显著大于 cellSize 的查询半径时需重评（r=2×cellSize 时 25 桶仍良性）。
    /// </summary>
    public sealed class SpatialGrid<T> where T : class
    {
        /// <summary>桶内条目：查询时需要元素位置做精确距离过滤。</summary>
        private struct Entry
        {
            public GameVector2 Pos;
            public T Item;
        }

        private readonly float cellSize;
        private readonly Dictionary<long, List<Entry>> cells = new Dictionary<long, List<Entry>>();
        private readonly List<long> usedKeys = new List<long>();
        private int count;

        /// <summary>
        /// 构造网格。
        /// </summary>
        /// <param name="cellSize">格边长（世界单位），须为正。</param>
        public SpatialGrid(float cellSize)
        {
            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "cellSize 必须为正数");
            }

            this.cellSize = cellSize;
        }

        /// <summary>上轮 Add 的元素总数。</summary>
        public int Count
        {
            get { return this.count; }
        }

        /// <summary>
        /// 开始新一轮重建：清空上轮所有非空桶（List.Clear 保容量防 GC）。
        /// 后续 Add 重新填充；不 Add 即为空网格。
        /// </summary>
        public void BeginRebuild()
        {
            for (int i = 0; i < this.usedKeys.Count; i++)
            {
                this.cells[this.usedKeys[i]].Clear();
            }

            this.usedKeys.Clear();
            this.count = 0;
        }

        /// <summary>
        /// 重建阶段加入一个元素。
        /// </summary>
        /// <param name="pos">元素位置。</param>
        /// <param name="item">元素载荷。</param>
        public void Add(GameVector2 pos, T item)
        {
            long key = PackKey(CellIndexOf(pos.X), CellIndexOf(pos.Y));
            if (!this.cells.TryGetValue(key, out List<Entry> bucket))
            {
                bucket = new List<Entry>();
                this.cells.Add(key, bucket);
            }

            if (bucket.Count == 0)
            {
                // 桶由空变非空才记 usedKeys（归纳保证：任何非空桶必在 usedKeys 里）
                this.usedKeys.Add(key);
            }

            bucket.Add(new Entry { Pos = pos, Item = item });
            this.count++;
        }

        /// <summary>
        /// 范围查询：把半径内（含边界）且通过 filter 的元素追加进 results（不清空 results）。
        /// filter 为查询时刻的实时复查（如存活判断），null 则不过滤——
        /// 网格是重建时刻的快照，判活类语义必须靠 filter 对齐。
        /// </summary>
        public void QueryRange(GameVector2 center, float radius, List<T> results, Func<T, bool> filter = null)
        {
            float radiusSq = radius * radius;
            int iy0 = CellIndexOf(center.Y - radius);
            int iy1 = CellIndexOf(center.Y + radius);
            int ix0 = CellIndexOf(center.X - radius);
            int ix1 = CellIndexOf(center.X + radius);
            for (int iy = iy0; iy <= iy1; iy++)
            {
                for (int ix = ix0; ix <= ix1; ix++)
                {
                    if (!this.cells.TryGetValue(PackKey(ix, iy), out List<Entry> bucket))
                    {
                        continue;
                    }

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        Entry e = bucket[i];
                        if (filter != null && !filter(e.Item))
                        {
                            continue;
                        }

                        if (e.Pos.SqrDistanceTo(center) <= radiusSq)
                        {
                            results.Add(e.Item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 最近邻查询：半径内（含边界）且通过 filter 的最近元素，无候选返回 null 且 sqrDistance=float.MaxValue。
        /// </summary>
        public T QueryNearest(GameVector2 center, float radius, out float sqrDistance, Func<T, bool> filter = null)
        {
            sqrDistance = float.MaxValue;
            T nearest = null;
            float radiusSq = radius * radius;
            int iy0 = CellIndexOf(center.Y - radius);
            int iy1 = CellIndexOf(center.Y + radius);
            int ix0 = CellIndexOf(center.X - radius);
            int ix1 = CellIndexOf(center.X + radius);
            for (int iy = iy0; iy <= iy1; iy++)
            {
                for (int ix = ix0; ix <= ix1; ix++)
                {
                    if (!this.cells.TryGetValue(PackKey(ix, iy), out List<Entry> bucket))
                    {
                        continue;
                    }

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        Entry e = bucket[i];
                        if (filter != null && !filter(e.Item))
                        {
                            continue;
                        }

                        float d = e.Pos.SqrDistanceTo(center);
                        if (d <= radiusSq && d < sqrDistance)
                        {
                            sqrDistance = d;
                            nearest = e.Item;
                        }
                    }
                }
            }

            return nearest;
        }

        private int CellIndexOf(float v)
        {
            // Floor 保证负坐标正确（地图 0..548，防御性支持负值）
            return (int)Math.Floor(v / this.cellSize);
        }

        private static long PackKey(int ix, int iy)
        {
            return ((long)ix << 32) | (uint)iy;
        }
    }
}
