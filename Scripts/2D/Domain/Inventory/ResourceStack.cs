namespace LAB2D.Domain.Inventory
{
    /// <summary>
    /// 资源堆叠 — 纯 C# 值对象，表示同类型资源的堆叠数量。
    /// 支持合并、拆分、容量检查等操作，不依赖 UnityEngine。
    /// </summary>
    public readonly struct ResourceStack
    {
        public readonly int ItemId;
        public readonly int Count;
        public readonly int Capacity;

        public bool IsEmpty
        {
            get { return this.ItemId < 0 || this.Count <= 0; }
        }

        public int AvailableSpace
        {
            get { return this.Capacity - this.Count; }
        }

        public bool IsFull
        {
            get { return this.Count >= this.Capacity; }
        }

        public ResourceStack(int itemId, int count, int capacity = 1000)
        {
            this.ItemId = itemId;
            this.Count = count > 0 ? count : 0;
            this.Capacity = capacity;
        }

        public static ResourceStack Empty(int capacity = 1000)
        {
            return new ResourceStack(-1, 0, capacity);
        }

        public bool CanMerge(ResourceStack other)
        {
            if (other.IsEmpty || this.IsEmpty)
            {
                return this.IsEmpty || other.Count <= this.AvailableSpace;
            }

            return this.ItemId == other.ItemId && this.Count + other.Count <= this.Capacity;
        }

        public ResourceStack Merge(ResourceStack other)
        {
            if (!this.CanMerge(other))
            {
                return this;
            }

            if (this.IsEmpty)
            {
                return new ResourceStack(other.ItemId, other.Count, this.Capacity);
            }

            return new ResourceStack(this.ItemId, this.Count + other.Count, this.Capacity);
        }

        public ResourceStack Add(int amount)
        {
            return this.Merge(new ResourceStack(this.IsEmpty ? -1 : this.ItemId, amount, this.Capacity));
        }

        public ResourceStack Take(int amount, out ResourceStack taken)
        {
            if (amount <= 0 || this.IsEmpty)
            {
                taken = ResourceStack.Empty(this.Capacity);
                return this;
            }

            int actualTaken = amount <= this.Count ? amount : this.Count;
            taken = new ResourceStack(this.ItemId, actualTaken, this.Capacity);

            int remaining = this.Count - actualTaken;
            if (remaining <= 0)
            {
                return ResourceStack.Empty(this.Capacity);
            }

            return new ResourceStack(this.ItemId, remaining, this.Capacity);
        }

        public ResourceStack WithCount(int newCount)
        {
            if (newCount <= 0)
            {
                return ResourceStack.Empty(this.Capacity);
            }

            return new ResourceStack(this.ItemId, newCount, this.Capacity);
        }

        public ResourceStack WithCapacity(int newCapacity)
        {
            return new ResourceStack(this.ItemId, this.Count, newCapacity);
        }
    }
}
