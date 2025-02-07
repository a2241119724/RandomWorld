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
            if (Select.item == null) return;
            if (ItemDataManager.Instance.getById(Select.item.id).type == ItemType.Weapon)
            {
                if (PlayerManager.Instance.Select.weapon != null)
                {
                    // �����ڴ�����������뱳��
                    BackpackController.Instance.addItem(PlayerManager.Instance.Select.weaponData);
                    // ��������
                    PhotonNetwork.Destroy(PlayerManager.Instance.Select.weapon);
                }
                // ���õ�ǰװ��id
                PlayerManager.Instance.Select.id = Select.item.id;
                // ʵ��������
                PlayerManager.Instance.Select.weapon = Tool.Instantiate(ResourcesManager.Instance.getPrefab(ItemDataManager.Instance.getById(Select.item.id).imageName), Vector3.zero,Quaternion.identity);
                if (PlayerManager.Instance.Select.weapon == null)
                {
                    Debug.LogError(" PlayerManager.Instance.Select.weapon Instantiate Error!!!");
                    return;
                }
                PlayerManager.Instance.Select.weapon.name = ItemDataManager.Instance.getById(Select.item.id).imageName;
                PlayerManager.Instance.Select.weapon.GetComponent<WeaponObject>().SetPlayer(PlayerManager.Instance.Mine);
                PlayerManager.Instance.Select.weapon.GetComponent<WeaponObject>().Item = Select.item;
                PlayerManager.Instance.Select.weapon.transform.SetParent(PlayerManager.Instance.Mine.transform, false);
                GlobalInit.Instance.showTip("װ���ɹ�");
                // �ӱ���ɾ���õ���
                PlayerManager.Instance.Select.weaponData = (Weapon)Select.item;
                BackpackController.Instance.deleteItem(Select.selectItemIndex);
                // ���ܶ�һ���������ж��װ��
                Select.selectItemIndex = -1;
                Select.item = null;
            }
            else if (ItemDataManager.Instance.getById(Select.item.id).type == ItemType.Consumable)
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
                if (((BackpackItem)Select.item).quantity == 1)
                {
                    // �ӱ���ɾ���õ���
                    BackpackController.Instance.deleteItem(Select.selectItemIndex);
                    Select.selectItemIndex = -1;
                    Select.item = null;
                }
                else {
                    Debug.Log(((BackpackItem)Select.item));
                    // ����--
                    BackpackController.Instance.reduceQuantity(Select.item);
                    // ����--
                    BackpackController.Instance.reduceQuantityUI(Select.item);
                    BackpackController.Instance.setBorderColor(BackpackController.Instance.getIndex(Select.item));
                    Debug.Log(((BackpackItem)Select.item));
                    // ȫ������--
                    BackpackItem item = (BackpackItem)Select.item;
                    --item.quantity;
                    Select.item = item;
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
            if (Select.item == null) return;
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
        public Item item = null;

        public void init()
        {
            selectItemIndex = -1;
            item = null;
        }
    }
}