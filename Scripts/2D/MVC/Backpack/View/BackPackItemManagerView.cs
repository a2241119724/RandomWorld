namespace LAB2D.MVC.Backpack.View
{
    using LAB2D;
    /// <summary>
    /// 背包仓库界面
    /// </summary>
    public class BackpackItemManagerView : MVCItemManagerView<BackpackItemView, BackpackModel>
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BackpackItemManagerView Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            Instance = this;
            this.itemBox = PrefabConstant.BACKPACK_ITEM;
        }

        /// <inheritdoc/>
        protected override int GetQuantity(AItem item)
        {
            return ((ABackpackItem)item).Quantity;
        }
    }
}