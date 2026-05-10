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

            if (this.model.IsNull(AItem.ItemTypeEnum.Weapon))
            {
                // addItem(ItemFactory.Instance.getBackpackItemByName("SingleGun"));
                List<AItem> items = ItemInstanceFactory.Instance.GenBackpackItems();
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
    }
}