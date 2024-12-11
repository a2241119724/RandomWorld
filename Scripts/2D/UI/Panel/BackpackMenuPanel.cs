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

        #region �¼�
        /// <summary>
        /// ������Ϸ
        /// </summary>
        public void OnClick_BackGame()
        {
            controller.close();
        }

        /// <summary>
        /// װ����ť
        /// </summary>
        private void OnClick_Equip()
        {
            if (Select.itemData == null) return;
            if (ItemDataManager.Instance.getById(Select.itemData.id).type == ItemType.Weapon)
            {
                if (PlayerManager.Instance.Select.weapon != null)
                {
                    // �����ڴ�����������뱳��
                    BackpackController.Instance.addItem(PlayerManager.Instance.Select.weaponData);
                    // ��������
                    PhotonNetwork.Destroy(PlayerManager.Instance.Select.weapon);
                }
                // ���õ�ǰװ��id
                PlayerManager.Instance.Select.currentId = Select.itemData.id;
                // ʵ��������
                PlayerManager.Instance.Select.weapon = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath(ItemDataManager.Instance.getById(Select.itemData.id).imageName+".prefab"), Vector3.zero,Quaternion.identity);
                if (PlayerManager.Instance.Select.weapon == null)
                {
                    Debug.LogError(" PlayerManager.Instance.Select.weapon Instantiate Error!!!");
                    return;
                }
                PlayerManager.Instance.Select.weapon.name = ItemDataManager.Instance.getById(Select.itemData.id).imageName;
                PlayerManager.Instance.Select.weapon.GetComponent<WeaponObject>().SetPlayer(PlayerManager.Instance.Mine);
                PlayerManager.Instance.Select.weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                GlobalInit.Instance.showTip("װ���ɹ�");
                // �ӱ���ɾ���õ���
                PlayerManager.Instance.Select.weaponData = (Weapon)Select.itemData;
                BackpackController.Instance.deleteItem(Select.selectItemIndex);
                // ���ܶ�һ���������ж��װ��
                Select.selectItemIndex = -1;
                Select.itemData = null;
            }
            else if (ItemDataManager.Instance.getById(Select.itemData.id).type == ItemType.Consumable)
            {
                // ʵ�������ߵ�������Ľű�����������
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
                // ���ٻ�ɾ��
                if (((BackpackItem)Select.itemData).quantity == 1)
                {
                    // �ӱ���ɾ���õ���
                    BackpackController.Instance.deleteItem(Select.selectItemIndex);
                    Select.selectItemIndex = -1;
                    Select.itemData = null;
                }
                else {
                    Debug.Log(((BackpackItem)Select.itemData));
                    // ����--
                    BackpackController.Instance.reduceQuantity(Select.itemData);
                    // ����--
                    BackpackController.Instance.reduceQuantityUI(Select.itemData);
                    BackpackController.Instance.setBorderColor(BackpackController.Instance.getIndex(Select.itemData));
                    Debug.Log(((BackpackItem)Select.itemData));
                    // ȫ������--
                    BackpackItem item = (BackpackItem)Select.itemData;
                    --item.quantity;
                    Select.itemData = item;
                }
            }
            else 
            {
                GlobalInit.Instance.showTip("δʵ��!!!");
            }
        }

        /// <summary>
        ///  ��������
        /// </summary>
        private void OnClick_Abandon()
        {
            if (Select.itemData == null) return;
            // �ӱ���ɾ���õ���
            BackpackController.Instance.deleteItem(Select.selectItemIndex);
            Select.init();
        }
        #endregion
    }

    /// <summary>
    /// �ٱ�����ѡ��ĵ�������
    /// </summary>
    public class SelectItemData
    {
        /// <summary>
        /// ѡ�еĵ�������
        /// </summary>
        public int selectItemIndex = -1;

        /// <summary>
        /// ѡ�еĵ�������
        /// </summary>
        public Item itemData = null;

        public void init()
        {
            selectItemIndex = -1;
            itemData = null;
        }
    }
}