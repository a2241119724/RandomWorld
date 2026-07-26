namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using LAB2D.Domain.Common;
    /// <summary>
    /// IItemDefinitionProvider 的 Unity 实现，封装 ItemDataManager。
    /// </summary>
    public sealed class UnityItemDefinitionAdapter : IItemDefinitionProvider
    {
        /// <inheritdoc/>
        public int GetItemTypeById(int itemId)
        {
            if (Core.ServiceLocator.Get<ItemDataManager>() == null)
            {
                return (int)AItem.ItemTypeEnum.Null;
            }

            AItem.ItemTypeEnum itemType = Core.ServiceLocator.Get<ItemDataManager>().IdToType(itemId);
            return (int)itemType;
        }
    }
}
