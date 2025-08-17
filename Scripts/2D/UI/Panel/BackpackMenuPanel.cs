namespace LAB2D
{
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
            Tool.GetComponentInChildren<Button>(this.Panel, "Equip").onClick.AddListener(this.OnClick_Equip);
            Tool.GetComponentInChildren<Button>(this.Panel, "Abandon").onClick.AddListener(this.OnClick_Abandon);
            Tool.GetComponentInChildren<Button>(this.Panel, "BackGame").onClick.AddListener(this.OnClick_Back);
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

            if (ItemDataManager.Instance.GetById(this.Select.Item.Id).Type == Item.ItemType.Weapon)
            {
                if (PlayerManager.Instance.Select.Weapon != null)
                {
                    // 将正在穿戴的物体加入背包
                    BackpackController.Instance.AddItem(PlayerManager.Instance.Select.WeaponData);

                    // 销毁武器
                    PhotonNetwork.Destroy(PlayerManager.Instance.Select.Weapon);
                }

                // 设置当前装备id
                PlayerManager.Instance.Select.Id = this.Select.Item.Id;

                // 实例化武器
                PlayerManager.Instance.Select.Weapon = ResourceManager.Instance.Instantiate(ItemDataManager.Instance.GetById(this.Select.Item.Id).ImageName, Vector3.zero, Quaternion.identity);
                if (PlayerManager.Instance.Select.Weapon == null)
                {
                    LogManager.Instance.Log("PlayerManager.Instance.Select.weapon Instantiate Error!!!", LogManager.LogLevel.Error);
                    return;
                }

                PlayerManager.Instance.Select.Weapon.name = ItemDataManager.Instance.GetById(this.Select.Item.Id).ImageName;
                PlayerManager.Instance.Select.Weapon.GetComponent<WeaponObject>().SetPlayer(PlayerManager.Instance.Mine);
                PlayerManager.Instance.Select.Weapon.GetComponent<WeaponObject>().Item = this.Select.Item;
                PlayerManager.Instance.Select.Weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                GlobalInit.Instance.ShowTip("装备成功");

                // 从背包删除该道具
                PlayerManager.Instance.Select.WeaponData = (Weapon)this.Select.Item;
                BackpackController.Instance.DeleteItem(this.Select.SelectItemIndex);

                // 不能对一个武器进行多次装备
                this.Select.SelectItemIndex = -1;
                this.Select.Item = null;
            }
            else if (ItemDataManager.Instance.GetById(this.Select.Item.Id).Type == Item.ItemType.Consumable)
            {
                // 实例化道具调用上面的脚本再立即销毁
                GameObject g = ResourceManager.Instance.Instantiate(ItemDataManager.Instance.GetById(this.Select.Item.Id).ImageName);
                if (g == null)
                {
                    LogManager.Instance.Log("Consumable is null!!!", LogManager.LogLevel.Error);
                    return;
                }

                g = Object.Instantiate(g);
                if (g == null)
                {
                    LogManager.Instance.Log("Consumable Instantiate Error!!!", LogManager.LogLevel.Error);
                    return;
                }

                g.GetComponent<ConsumableObject>().Use();
                Object.Destroy(g);

                // 减少或删除
                if (((BackpackItem)this.Select.Item).Quantity == 1)
                {
                    // 从背包删除该道具
                    BackpackController.Instance.DeleteItem(this.Select.SelectItemIndex);
                    this.Select.SelectItemIndex = -1;
                    this.Select.Item = null;
                }
                else
                {
                    LogManager.Instance.Log("数量:" + ((BackpackItem)this.Select.Item).Quantity, LogManager.LogLevel.Info);

                    // 数据--
                    BackpackController.Instance.ReduceQuantity(this.Select.Item);

                    // 界面--
                    BackpackController.Instance.ReduceQuantityUI(this.Select.Item);
                    BackpackController.Instance.SetBorderColor(BackpackController.Instance.GetIndex(this.Select.Item));
                    LogManager.Instance.Log("数量:" + ((BackpackItem)this.Select.Item).Quantity, LogManager.LogLevel.Info);

                    // 全局数据--
                    BackpackItem item = (BackpackItem)this.Select.Item;
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
        /// 选中的道具索引
        /// </summary>
        public int SelectItemIndex = -1;

        /// <summary>
        /// 选中的道具数据
        /// </summary>
        public Item Item = null;

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