namespace LAB2D
{
    /// <summary>
    /// Unity implementation of IItemDefinitionProvider wrapping ItemDataManager.
    /// </summary>
    public sealed class UnityItemDefinitionAdapter : IItemDefinitionProvider
    {
        /// <inheritdoc/>
        public int GetItemTypeById(int itemId)
        {
            if (ItemDataManager.Instance == null)
            {
                return (int)AItem.ItemTypeEnum.Null;
            }

            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(itemId);
            return (int)itemType;
        }
    }
}
