namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Item;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 背包面板
    /// </summary>
    public class BackpackPanel : ABasePanel<BackpackPanel>
    {
        public BackpackPanel()
        {
            this.Name = "BackpackPanel";
            this.Select = new SelectItemData();
            this.Init();
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Equip").onClick.AddListener(this.OnClick_Equip);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "Abandon").onClick.AddListener(this.OnClick_Abandon);
            LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);
        }

        /// <summary>
        /// 选中的物品
        /// </summary>
        public SelectItemData Select { get; set; }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            ServiceLocator.Get<BackpackController>().SetBorderColor(System.Convert.ToInt32(ServiceLocator.Get<BackpackNavigationView>().CurItemType), "navigation");
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        /// <summary>
        /// 装备按钮
        /// </summary>
        private void OnClick_Equip()
        {
            if (this.Select.Item == null)
            {
                return;
            }

            Player.PlayerData playerData = ServiceLocator.Get<PlayerManager>().Mine.CharacterDataLAB as Player.PlayerData;
            if (ServiceLocator.Get<ItemDataManager>().GetById(this.Select.Item.Id).Type == AItem.ItemTypeEnum.Weapon)
            {
                if (ServiceLocator.Get<PlayerManager>().Mine.Weapon != null)
                {
                    // 将正在穿戴的物体加入背包
                    ServiceLocator.Get<BackpackController>().AddItem(playerData.Weapon);

                    // 销毁武器
                    Core.GameServices.NetworkDestroyProvider(ServiceLocator.Get<PlayerManager>().Mine.Weapon);
                }

                // 实例化武器
                string name = ServiceLocator.Get<ItemDataManager>().GetById(this.Select.Item.Id).Name;
                ServiceLocator.Get<PlayerManager>().Mine.Weapon = ServiceLocator.Get<ResourceManager>().Instantiate(name, false);
                if (ServiceLocator.Get<PlayerManager>().Mine.Weapon == null)
                {
                    AWorkerTask.LogProvider("武器实例化错误!", LogManager.LogLevelEnum.Error);
                    return;
                }

                ServiceLocator.Get<PlayerManager>().Mine.Weapon.name = name;
                ServiceLocator.Get<PlayerManager>().Mine.Weapon.transform.SetParent(ServiceLocator.Get<PlayerManager>().Mine.transform, false);
                AWeaponObject weaponObject = ServiceLocator.Get<PlayerManager>().Mine.Weapon.GetComponent<AWeaponObject>();
                weaponObject.SetCharacter(ServiceLocator.Get<PlayerManager>().Mine);
                weaponObject.Item = this.Select.Item;
                ServiceLocator.Get<GlobalInit>().ShowTip("装备成功");

                // 从背包删除该道具
                playerData.Weapon = (AWeapon)this.Select.Item;
                ServiceLocator.Get<BackpackController>().DeleteItem(this.Select.SelectItemIndex);

                // 不能对一个武器进行多次装备
                this.Select.SelectItemIndex = -1;
                this.Select.Item = null;
            }
            else if (ServiceLocator.Get<ItemDataManager>().GetById(this.Select.Item.Id).Type == AItem.ItemTypeEnum.Consumable)
            {
                // 消耗品使用：AConsumable 子类（如血瓶）走自定义 Use()；
                // 无自定义类的默认物品若配置 Prefab 视觉，则在玩家当前位置创建预制体（如火把）。
                ItemData itemData = ServiceLocator.Get<ItemDataManager>().GetById(this.Select.Item.Id);
                if (this.Select.Item is AConsumable consumable)
                {
                    consumable.Use();
                }
                else if (itemData.VisualMode == ItemVisualMode.Prefab)
                {
                    GameObject instance = ServiceLocator.Get<ResourceManager>().Instantiate(
                        itemData.Name,
                        ServiceLocator.Get<PlayerManager>().Mine.transform.position,
                        Quaternion.identity);
                    if (instance == null)
                    {
                        return;
                    }
                }
                else
                {
                    ServiceLocator.Get<GlobalInit>().ShowTip("未实现!!!");
                    return;
                }

                // 减少或删除
                if (((ABackpackItem)this.Select.Item).Quantity == 1)
                {
                    // 从背包删除该道具
                    ServiceLocator.Get<BackpackController>().DeleteItem(this.Select.SelectItemIndex);
                    this.Select.SelectItemIndex = -1;
                    this.Select.Item = null;
                }
                else
                {
                    AWorkerTask.LogProvider("数量:" + ((ABackpackItem)this.Select.Item).Quantity, LogManager.LogLevelEnum.Trace);

                    // 数据--
                    ServiceLocator.Get<BackpackController>().ReduceQuantity(this.Select.Item);

                    // 界面--
                    ServiceLocator.Get<BackpackController>().ReduceQuantityUI(this.Select.Item);
                    ServiceLocator.Get<BackpackController>().SetBorderColor(ServiceLocator.Get<BackpackController>().GetIndex(this.Select.Item));
                    AWorkerTask.LogProvider("数量:" + ((ABackpackItem)this.Select.Item).Quantity, LogManager.LogLevelEnum.Trace);

                    // 全局数据--
                    ABackpackItem item = (ABackpackItem)this.Select.Item;
                    --item.Quantity;
                    this.Select.Item = item;
                }
            }
            else
            {
                ServiceLocator.Get<GlobalInit>().ShowTip("未实现!!!");
            }
        }

        /// <summary>
        ///  丢弃道具
        /// </summary>
        private void OnClick_Abandon()
        {
            if (this.Select.Item == null)
            {
                return;
            }

            // 从背包删除该道具
            ServiceLocator.Get<BackpackController>().DeleteItem(this.Select.SelectItemIndex);
            this.Select.Init();
        }
    }
}
