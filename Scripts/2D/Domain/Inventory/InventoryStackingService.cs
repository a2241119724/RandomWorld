namespace LAB2D
{
    /// <summary>
    /// 库存堆叠容量的纯算术规则。
    /// </summary>
    public sealed class InventoryStackingService
    {
        public int GetAvailableCapacity(int cellCapacity, int currentCount, int reservedCount)
        {
            int available = cellCapacity - (currentCount + reservedCount);
            return available > 0 ? available : 0;
        }

        public int GetPlaceCount(int remainingCount, int availableCapacity)
        {
            if (remainingCount <= 0 || availableCapacity <= 0)
            {
                return 0;
            }

            return remainingCount <= availableCapacity ? remainingCount : availableCapacity;
        }

        public bool CanPlaceAll(int remainingCount, int availableCapacity)
        {
            return remainingCount > 0 && remainingCount <= availableCapacity;
        }
    }
}
