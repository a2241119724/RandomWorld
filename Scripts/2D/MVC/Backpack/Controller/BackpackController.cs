namespace LAB2D
{
    using System.Collections.Generic;

    /// <summary>
    /// 背包控制器
    /// </summary>
    public class BackpackController : MVCController<BackpackItemManagerView, BackpackModel, BackpackNavigationView, BackpackItemView, BackpackInfoView>
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static BackpackController Instance { get; private set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            this.itemManagerView = Tool.GetComponentInChildren<BackpackItemManagerView>(this.gameObject, "Inventory");
            this.navigationView = Tool.GetComponentInChildren<BackpackNavigationView>(this.gameObject, "Navigation");
            this.infoView = Tool.GetComponentInChildren<BackpackInfoView>(this.gameObject, "Info");
            base.Awake();
            Instance = this;

            if (this.model.IsNull(AItem.ItemType.Weapon))
            {
                // addItem(ItemFactory.Instance.getBackpackItemByName("SingleGun"));
                List<AItem> items = ItemFactory.Instance.GenBackpackItems();
                foreach (AItem item in items)
                {
                    this.AddItem(item);
                }
            }
        }
    }
}