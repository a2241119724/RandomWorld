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

        /// <summary>
        /// 获取背包中所有种子物品。
        /// </summary>
        /// <returns>种子物品列表（副本，不会为 null）</returns>
        public List<AItem> GetSeeds()
        {
            if (!this.model.ItemDict.TryGetValue(AItem.ItemTypeEnum.Seed, out List<AItem> seeds))
            {
                return new List<AItem>();
            }

            return new List<AItem>(seeds);
        }

        /// <summary>
        /// 根据 Uid 移除指定种子。
        /// 若种子数量大于 1 则减少数量，否则删除整个物品。
        /// </summary>
        /// <param name="uid">种子的唯一标识符</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveSeedByUid(int uid)
        {
            if (!this.model.ItemDict.TryGetValue(AItem.ItemTypeEnum.Seed, out List<AItem> seeds))
            {
                return false;
            }

            for (int i = 0; i < seeds.Count; i++)
            {
                if (seeds[i].Uid == uid)
                {
                    if (seeds[i].Quantity > 1)
                    {
                        seeds[i].Quantity--;
                    }
                    else
                    {
                        this.model.Delete(AItem.ItemTypeEnum.Seed, i);
                    }

                    this.UpdateInventory();
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void OnSelectItem(int index, AItem item)
        {
            BackpackMenuPanel.Instance.Select.SelectItemIndex = index;
            BackpackMenuPanel.Instance.Select.Item = item as ABackpackItem;
        }
    }
}