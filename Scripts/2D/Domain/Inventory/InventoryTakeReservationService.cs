namespace LAB2D
{
    /// <summary>
    /// Pure arithmetic for inventory take reservations.
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
