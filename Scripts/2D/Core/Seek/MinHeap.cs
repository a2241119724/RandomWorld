namespace LAB2D.Core.Seek
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 最小堆数据结构，用于A*算法的openList。
    /// 支持O(log n)的插入、提取最小值和删除操作，
    /// 内部维护位置字典以支持O(log n)的任意元素删除。
    /// </summary>
    /// <typeparam name="T">堆中存储的元素类型</typeparam>
    public class MinHeap<T>
    {
        private readonly List<T> items = new ();
        private readonly Dictionary<T, int> positions = new ();
        private readonly IComparer<T> comparer;

        /// <summary>
        /// 堆中元素数量。
        /// </summary>
        public int Count => this.items.Count;

        /// <summary>
        /// 创建最小堆实例。
        /// </summary>
        /// <param name="comparer">元素比较器，用于确定堆序。</param>
        public MinHeap(IComparer<T> comparer)
        {
            this.comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// 插入元素到堆中。
        /// </summary>
        public void Add(T item)
        {
            this.items.Add(item);
            int index = this.items.Count - 1;
            this.positions[item] = index;
            this.BubbleUp(index);
        }

        /// <summary>
        /// 查看最小元素（不移除）。
        /// </summary>
        public T PeekMin()
        {
            if (this.items.Count == 0)
            {
                throw new InvalidOperationException("MinHeap is empty");
            }

            return this.items[0];
        }

        /// <summary>
        /// 提取并移除最小元素。
        /// </summary>
        public T ExtractMin()
        {
            if (this.items.Count == 0)
            {
                throw new InvalidOperationException("MinHeap is empty");
            }

            T min = this.items[0];
            this.RemoveAt(0);
            return min;
        }

        /// <summary>
        /// 检查元素是否在堆中（O(1)）。
        /// </summary>
        public bool Contains(T item)
        {
            return this.positions.ContainsKey(item);
        }

        /// <summary>
        /// 从堆中移除指定元素（O(log n)）。
        /// 如果元素不在堆中，则无操作。
        /// </summary>
        public void Remove(T item)
        {
            if (this.positions.TryGetValue(item, out int index))
            {
                this.RemoveAt(index);
            }
        }

        /// <summary>
        /// 清空堆中的所有元素。
        /// </summary>
        public void Clear()
        {
            this.items.Clear();
            this.positions.Clear();
        }

        /// <summary>
        /// 移除指定索引处的元素并维护堆性质。
        /// </summary>
        private void RemoveAt(int index)
        {
            int lastIndex = this.items.Count - 1;
            T removedItem = this.items[index];

            // 移除被删除元素的位置记录
            this.positions.Remove(removedItem);

            if (index != lastIndex)
            {
                // 将最后一个元素移到被删除位置
                T lastItem = this.items[lastIndex];
                this.items[index] = lastItem;
                this.positions[lastItem] = index;
                this.items.RemoveAt(lastIndex);

                // 重新堆化：先尝试上浮，再尝试下沉
                this.BubbleUp(index);
                this.BubbleDown(index);
            }
            else
            {
                this.items.RemoveAt(lastIndex);
            }
        }

        /// <summary>
        /// 上浮操作：将索引处的元素上移以维护堆性质。
        /// </summary>
        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (this.comparer.Compare(this.items[index], this.items[parent]) >= 0)
                {
                    break;
                }

                this.Swap(index, parent);
                index = parent;
            }
        }

        /// <summary>
        /// 下沉操作：将索引处的元素下移以维护堆性质。
        /// </summary>
        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = (2 * index) + 1;
                int right = (2 * index) + 2;
                int smallest = index;

                if (left < this.items.Count &&
                    this.comparer.Compare(this.items[left], this.items[smallest]) < 0)
                {
                    smallest = left;
                }

                if (right < this.items.Count &&
                    this.comparer.Compare(this.items[right], this.items[smallest]) < 0)
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    break;
                }

                this.Swap(index, smallest);
                index = smallest;
            }
        }

        /// <summary>
        /// 交换堆中两个位置的元素并更新位置字典。
        /// </summary>
        private void Swap(int i, int j)
        {
            T temp = this.items[i];
            this.items[i] = this.items[j];
            this.items[j] = temp;
            this.positions[this.items[i]] = i;
            this.positions[this.items[j]] = j;
        }
    }
}
