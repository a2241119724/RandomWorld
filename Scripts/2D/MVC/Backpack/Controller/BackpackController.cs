namespace LAB2D.MVC.Backpack.Controller
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.MVC.Backpack.Model;
    using LAB2D.MVC.Backpack.View;
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
            this.itemManagerView = LAB2D.Tool.Tool.GetComponentInChildren<BackpackItemManagerView>(this.gameObject, "Inventory");
            this.navigationView = LAB2D.Tool.Tool.GetComponentInChildren<BackpackNavigationView>(this.gameObject, "Navigation");
            this.infoView = LAB2D.Tool.Tool.GetComponentInChildren<BackpackInfoView>(this.gameObject, "Info");
            base.Awake();
            Instance = this;
            ServiceLocator.Register(this);

            if (this.model.IsNull(AItem.ItemTypeEnum.Weapon))
            {
                // addItem(ItemFactory.Instance.getBackpackItemByName("SingleGun"));
                List<AItem> items = ServiceLocator.Get<ItemInstanceFactory>().GenBackpackItems();
                foreach (AItem item in items)
                {
                    // 为初始道具随机分配品质（使背包中的初始道具品质多样化）
                    if (item is ABackpackItem backpackItem)
                    {
                        EquipmentRarityType rarity = EquipmentLootTool.RollRarity(0);
                        backpackItem.Quality = EquipmentLootTool.MapRarityToQuality(rarity);
                        if (item is AEquipment eq)
                        {
                            EquipmentLootTool.ApplyRarityToAttributes(eq.Attribute, rarity);
                        }
                    }

                    this.AddItem(item);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSelectItem(int index, AItem item)
        {
            BackpackMenuPanel.Instance.Select.SelectItemIndex = index;
            BackpackMenuPanel.Instance.Select.Item = item as ABackpackItem;
        }
    }
}