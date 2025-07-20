using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    public class BackpackMenuPanel : BasePanel<BackpackMenuPanel>
    {
        public SelectItemData Select { set; get; }

        public BackpackMenuPanel()
        {
            Name = "BackpackMenu";
            Select = new SelectItemData();
            setPanel();
            Tool.GetComponentInChildren<Button>(panel, "Equip").onClick.AddListener(OnClick_Equip);
            Tool.GetComponentInChildren<Button>(panel, "Abandon").onClick.AddListener(OnClick_Abandon);
            Tool.GetComponentInChildren<Button>(panel, "BackGame").onClick.AddListener(OnClick_BackGame);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            BackpackController.Instance.setBorderColor(System.Convert.ToInt32(BackpackNavigationView.Instance.CurItemType), "navigation");
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        #region 事件
        /// <summary>
        /// 返回游戏
        /// </summary>
        public void OnClick_BackGame()
        {
            controller.close();
        }

        /// <summary>
        /// 装备按钮
        /// </summary>
        private void OnClick_Equip()
        {
            if (Select.item == null) return;
            if (ItemDataManager.Instance.GetById(Select.item.Id).Type == ItemType.Weapon)
            {
                if (PlayerManager.Instance.Select.Weapon != null)
                {
                    // 将正在穿戴的物体加入背包
                    BackpackController.Instance.addItem(PlayerManager.Instance.Select.WeaponData);
                    // 销毁武器
                    PhotonNetwork.Destroy(PlayerManager.Instance.Select.Weapon);
                }
                // 设置当前装备id
                PlayerManager.Instance.Select.Id = Select.item.Id;
                // 实例化武器
                PlayerManager.Instance.Select.Weapon = Tool.Instantiate(ResourcesManager.Instance.GetPrefab(ItemDataManager.Instance.GetById(Select.item.Id).ImageName), Vector3.zero, Quaternion.identity);
                if (PlayerManager.Instance.Select.Weapon == null)
                {
                    LogManager.Instance.Log("PlayerManager.Instance.Select.weapon Instantiate Error!!!", LogManager.LogLevel.Error);
                    return;
                }
                PlayerManager.Instance.Select.Weapon.name = ItemDataManager.Instance.GetById(Select.item.Id).ImageName;
                PlayerManager.Instance.Select.Weapon.GetComponent<WeaponObject>().SetPlayer(PlayerManager.Instance.Mine);
                PlayerManager.Instance.Select.Weapon.GetComponent<WeaponObject>().Item = Select.item;
                PlayerManager.Instance.Select.Weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                GlobalInit.Instance.ShowTip("装备成功");
                // 从背包删除该道具
                PlayerManager.Instance.Select.WeaponData = (Weapon)Select.item;
                BackpackController.Instance.deleteItem(Select.selectItemIndex);
                // 不能对一个武器进行多次装备
                Select.selectItemIndex = -1;
                Select.item = null;
            }
            else if (ItemDataManager.Instance.GetById(Select.item.Id).Type == ItemType.Consumable)
            {
                // 实例化道具调用上面的脚本再立即销毁
                GameObject g = ResourcesManager.Instance.GetPrefab("Select.selectItemData.itemName");
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
                if (((BackpackItem)Select.item).Quantity == 1)
                {
                    // 从背包删除该道具
                    BackpackController.Instance.deleteItem(Select.selectItemIndex);
                    Select.selectItemIndex = -1;
                    Select.item = null;
                }
                else
                {
                    LogManager.Instance.Log("数量:" + ((BackpackItem)Select.item).Quantity, LogManager.LogLevel.Info);
                    // 数据--
                    BackpackController.Instance.reduceQuantity(Select.item);
                    // 界面--
                    BackpackController.Instance.reduceQuantityUI(Select.item);
                    BackpackController.Instance.setBorderColor(BackpackController.Instance.getIndex(Select.item));
                    LogManager.Instance.Log("数量:" + ((BackpackItem)Select.item).Quantity, LogManager.LogLevel.Info);
                    // 全局数据--
                    BackpackItem item = (BackpackItem)Select.item;
                    --item.Quantity;
                    Select.item = item;
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
            if (Select.item == null) return;
            // 从背包删除该道具
            BackpackController.Instance.deleteItem(Select.selectItemIndex);
            Select.init();
        }
        #endregion
    }

    /// <summary>
    /// 再背包中选择的道具类型
    /// </summary>
    public class SelectItemData
    {
        /// <summary>
        /// 选中的道具索引
        /// </summary>
        public int selectItemIndex = -1;

        /// <summary>
        /// 选中的道具数据
        /// </summary>
        public Item item = null;

        public void init()
        {
            selectItemIndex = -1;
            item = null;
        }
    }
}