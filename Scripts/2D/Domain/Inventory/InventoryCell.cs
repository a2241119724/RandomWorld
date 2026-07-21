namespace LAB2D.Domain.Inventory
{
    using LAB2D.Domain.Common;

    /// <summary>
    /// 库存格子 — 纯 C# 数据模型，包装 ResourceStack 提供可变库存操作。
    /// 不依赖 UnityEngine，不依赖 ItemMap 或 ResourceManager。
    /// </summary>
    public sealed class InventoryCell
    {
        private ResourceStack stack;

        public int ItemId
        {
            get { return this.stack.ItemId; }
            private set { this.stack = new ResourceStack(value, this.stack.Count, this.stack.Capacity); }
        }

        public int Count
        {
            get { return this.stack.Count; }
            private set { this.stack = this.stack.WithCount(value); }
        }

        public int Capacity
        {
            get { return this.stack.Capacity; }
        }

        public bool IsEmpty
        {
            get { return this.stack.IsEmpty; }
        }

        public int AvailableCapacity
        {
            get { return this.stack.AvailableSpace; }
        }

        public InventoryCell(int capacity = 1000)
        {
            this.stack = ResourceStack.Empty(capacity);
        }

        public bool CanAdd(int itemId, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            if (this.IsEmpty)
            {
                return amount <= this.Capacity;
            }

            return this.ItemId == itemId && this.Count + amount <= this.Capacity;
        }

        public bool Add(int itemId, int amount)
        {
            if (!this.CanAdd(itemId, amount))
            {
                return false;
            }

            if (this.IsEmpty)
            {
                this.stack = new ResourceStack(itemId, amount, this.Capacity);
            }
            else
            {
                this.stack = this.stack.Add(amount);
            }

            return true;
        }

        public int Take(int amount)
        {
            if (amount <= 0 || this.IsEmpty)
            {
                return 0;
            }

            ResourceStack taken;
            this.stack = this.stack.Take(amount, out taken);
            return taken.Count;
        }

        public ResourceInfo GetResourceInfo()
        {
            return new ResourceInfo(this.ItemId, this.Count);
        }

        public void Clear()
        {
            this.stack = ResourceStack.Empty(this.Capacity);
        }
    }
}
