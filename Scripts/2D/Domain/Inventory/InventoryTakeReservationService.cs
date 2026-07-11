namespace LAB2D
{
    /// <summary>
    /// 库存取物预留的纯算术规则。
    /// </summary>
    public sealed class InventoryTakeReservationService
    {
        public int GetTargetTakeCount(int requiredCount, int workerMaxCarryCount)
        {
            return requiredCount > workerMaxCarryCount ? requiredCount : workerMaxCarryCount;
        }

        public int GetAvailableTakeCount(int currentCount, int reservedCount)
        {
            int available = currentCount - reservedCount;
            return available > 0 ? available : 0;
        }
    }
}
