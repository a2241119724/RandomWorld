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
            if (Select.itemData == null) return;
            if (ItemDataManager.Instance.getById(Select.itemData.id).type == ItemType.Weapon)
            {
                if (PlayerManager.Instance.Select.weapon != null)
                {
                    // 将正在穿戴的物体加入背包
                    BackpackController.Instance.addItem(PlayerManager.Instance.Select.weaponData);
                    // 销毁武器
                    PhotonNetwork.Destroy(PlayerManager.Instance.Select.weapon);
                }
                // 设置当前装备id
                PlayerManager.Instance.Select.currentId = Select.itemData.id;
                // 实例化武器
                PlayerManager.Instance.Select.weapon = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath(ItemDataManager.Instance.getById(Select.itemData.id).imageName+".prefab"), Vector3.zero,Quaternion.identity);
                if (PlayerManager.Instance.Select.weapon == null)
                {
                    Debug.LogError(" PlayerManager.Instance.Select.weapon Instantiate Error!!!");
                    return;
                }
                PlayerManager.Instance.Select.weapon.name = ItemDataManager.Instance.getById(Select.itemData.id).imageName;
                PlayerManager.Instance.Select.weapon.GetComponent<WeaponObject>().SetPlayer(PlayerManager.Instance.Mine);
                PlayerManager.Instance.Select.weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                GlobalInit.Instance.showTip("装备成功");
                // 从背包删除该道具
                PlayerManager.Instance.Select.weaponData = (Weapon)Select.itemData;
                BackpackController.Instance.deleteItem(Select.selectItemIndex);
                // 不能对一个武器进行多次装备
                Select.selectItemIndex = -1;
                Select.itemData = null;
            }
            else if (ItemDataManager.Instance.getById(Select.itemData.id).type == ItemType.Consumable)
            {
                // 实例化道具调用上面的脚本再立即销毁
                GameObject g = ResourcesManager.Instance.getPrefab("Select.selectItemData.itemName");
                if (g == null)
                {
                    Debug.LogError("Consumable is null!!!");
                    return;
                }
                g = Object.Instantiate(g);
                if (g == null)
                {
                    Debug.LogError("Consumable Instantiate Error!!!");
                    return;
                }
                g.GetComponent<ConsumableObject>().use();
                Object.Destroy(g);
                // 减少或删除
                if (((BackpackItem)Select.itemData).quantity == 1)
                {
                    // 从背包删除该道具
                    BackpackController.Instance.deleteItem(Select.selectItemIndex);
                    Select.selectItemIndex = -1;
                    Select.itemData = null;
                }
                else {
                    Debug.Log(((BackpackItem)Select.itemData));
                    // 数据--
                    BackpackController.Instance.reduceQuantity(Select.itemData);
                    // 界面--
                    BackpackController.Instance.reduceQuantityUI(Select.itemData);
                    BackpackController.Instance.setBorderColor(BackpackController.Instance.getIndex(Select.itemData));
                    Debug.Log(((BackpackItem)Select.itemData));
                    // 全局数据--
                    BackpackItem item = (BackpackItem)Select.itemData;
                    --item.quantity;
                    Select.itemData = item;
                }
            }
            else 
            {
                GlobalInit.Instance.showTip("未实现!!!");
            }
        }

        /// <summary>
        ///  丢弃道具
        /// </summary>
        private void OnClick_Abandon()
        {
            if (Select.itemData == null) return;
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
        public Item itemData = null;

        public void init()
        {
            selectItemIndex = -1;
            itemData = null;
        }
    }
}