namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item;
    using Photon.Pun;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 背包菜单面板
    /// </summary>
    public class BackpackMenuPanel : ABasePanel<BackpackMenuPanel>
    {
        public BackpackMenuPanel()
        {
            this.Name = "BackpackMenu";
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
            BackpackController.Instance.SetBorderColor(System.Convert.ToInt32(BackpackNavigationView.Instance.CurItemType), "navigation");
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

            Player.PlayerData playerData = PlayerManager.Instance.Mine.CharacterDataLAB as Player.PlayerData;
            if (ItemDataManager.Instance.GetById(this.Select.Item.Id).Type == AItem.ItemTypeEnum.Weapon)
            {
                if (PlayerManager.Instance.Mine.Weapon != null)
                {
                    // 将正在穿戴的物体加入背包
                    BackpackController.Instance.AddItem(playerData.Weapon);

                    // 销毁武器
                    PhotonNetwork.Destroy(PlayerManager.Instance.Mine.Weapon);
                }

                // 实例化武器
                string name = ItemDataManager.Instance.GetById(this.Select.Item.Id).EnName;
                PlayerManager.Instance.Mine.Weapon = ResourceManager.Instance.Instantiate(name, false);
                if (PlayerManager.Instance.Mine.Weapon == null)
                {
                    AWorkerTask.LogProvider("武器实例化错误!", LogManager.LogLevelEnum.Error);
                    return;
                }

                PlayerManager.Instance.Mine.Weapon.name = name;
                PlayerManager.Instance.Mine.Weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                AWeaponObject weaponObject = PlayerManager.Instance.Mine.Weapon.GetComponent<AWeaponObject>();
                weaponObject.SetCharacter(PlayerManager.Instance.Mine);
                weaponObject.Item = this.Select.Item;
                GlobalInit.Instance.ShowTip("装备成功");

                // 从背包删除该道具
                playerData.Weapon = (AWeapon)this.Select.Item;
                BackpackController.Instance.DeleteItem(this.Select.SelectItemIndex);

                // 不能对一个武器进行多次装备
                this.Select.SelectItemIndex = -1;
                this.Select.Item = null;
            }
            else if (ItemDataManager.Instance.GetById(this.Select.Item.Id).Type == AItem.ItemTypeEnum.Consumable)
            {
                ((AConsumable)this.Select.Item).Use();

                // 减少或删除
                if (((ABackpackItem)this.Select.Item).Quantity == 1)
                {
                    // 从背包删除该道具
                    BackpackController.Instance.DeleteItem(this.Select.SelectItemIndex);
                    this.Select.SelectItemIndex = -1;
                    this.Select.Item = null;
                }
                else
                {
                    AWorkerTask.LogProvider("数量:" + ((ABackpackItem)this.Select.Item).Quantity, LogManager.LogLevelEnum.Trace);

                    // 数据--
                    BackpackController.Instance.ReduceQuantity(this.Select.Item);

                    // 界面--
                    BackpackController.Instance.ReduceQuantityUI(this.Select.Item);
                    BackpackController.Instance.SetBorderColor(BackpackController.Instance.GetIndex(this.Select.Item));
                    AWorkerTask.LogProvider("数量:" + ((ABackpackItem)this.Select.Item).Quantity, LogManager.LogLevelEnum.Trace);

                    // 全局数据--
                    ABackpackItem item = (ABackpackItem)this.Select.Item;
                    --item.Quantity;
                    this.Select.Item = item;
                }
            }
            else
            {
                GlobalInit.Instance.ShowTip("未实现!!!");
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
            BackpackController.Instance.DeleteItem(this.Select.SelectItemIndex);
            this.Select.Init();
        }
    }

    /// <summary>
    /// 再背包中选择的道具类型
    /// </summary>
    public class SelectItemData
    {
        /// <summary>
        /// 选中的道具索引(在背包中)
        /// </summary>
        public int SelectItemIndex = -1;

        /// <summary>
        /// 选中的道具数据
        /// </summary>
        public AItem Item = null;

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            this.SelectItemIndex = -1;
            this.Item = null;
        }
    }
}
