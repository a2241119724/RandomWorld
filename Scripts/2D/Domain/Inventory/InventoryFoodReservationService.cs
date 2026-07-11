namespace LAB2D
{
    /// <summary>
    /// 库存规则的纯食物预留算术。
    /// </summary>
    public sealed class InventoryFoodReservationService
    {
        public int GetNeededFoodCount(float hungryValue, float hungryRestoredPerItem)
        {
            if (hungryValue <= 0.0f || hungryRestoredPerItem <= 0.0f)
            {
                return 0;
            }

            int wholeItems = (int)(hungryValue / hungryRestoredPerItem);
            return wholeItems * hungryRestoredPerItem >= hungryValue
                ? wholeItems
                : wholeItems + 1;
        }

        public int GetPreTakeCount(int availableCount, int needCount)
        {
            if (availableCount <= 0 || needCount <= 0)
            {
                return 0;
            }

            return availableCount < needCount ? availableCount : needCount;
        }
    }
}
