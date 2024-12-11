using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LAB2D
{
    /// <summary>
    /// User:����View��Controller
    /// Model
    /// View:�¼�֪ͨController
    /// Controller:����View��Model
    /// </summary>
    public abstract class MVCController<IMV, M, NV, IV, IV_> : MonoBehaviourInit
        where M : MVCModel, new()
        where IV : MVCItemView
        where IMV : MVCItemManagerView<IV,M>
        where NV : MVCNavigationView
        where IV_ : MVCInfoView
    {
        protected IMV itemManagerView;
        protected M model;
        protected NV navigationView;
        protected IV_ infoView;

        public virtual void Awake()
        {
            // ��ӵ�ItemManagerView
            itemManagerView.exchangeItem += exchangeItem;
            itemManagerView.setBorderColor += setBorderColor;
            itemManagerView.get += get;
            itemManagerView.showInfo += showInfo;
            model = new M();
            navigationView.OnClick += (int index) =>
            {
                setBorderColor(index, "navigation");
                updateInventory();
            };
        }

        private void OnEnable()
        {
            updateInventory();
        }

        public virtual void addItem(Item item)
        {
            model.addItem(item);
            // ���ܸ��£����ڸ�����Ҫ�򿪱���
            // updateInventory();
        }

        #region ��������
        public void exchangeItem(int index1, int index2)
        {
            model.exchangeItem(navigationView.CurItemType,index1, index2);
            updateInventory();
        }

        public void deleteItem(int index)
        {
            model.deleteItem(navigationView.CurItemType, index);
            updateInventory();
        }

        public Item get(int index)
        {
            return model.get(navigationView.CurItemType, index);
        }

        public void reduceQuantity(Item item)
        {
            model.reduceQuantity(navigationView.CurItemType, item);
        }

        public int getIndex(Item item)
        {
            return model.getIndex(navigationView.CurItemType, (Weapon)item);
        }
        #endregion

        #region ����ҳ��
        /// <summary>
        /// ���²ֿ����
        /// </summary>
        public void updateInventory()
        {
            if (itemManagerView == null)
            {
                Debug.LogError("inventoryView is null!!!");
                return;
            }
            itemManagerView.updateView(navigationView.CurItemType,model);
        }

        /// <summary>
        /// ���ü���ʱ�ı߿���ɫ
        /// </summary>
        /// <param name="index"></param>
        public void setBorderColor(int index, string name = "item")
        {
            switch (name)
            {
                case "item":
                    foreach (IV item in itemManagerView.itemsView)
                    {
                        item.GetComponent<Image>().color = Color.white;
                        item.IsDrag = false;
                    }
                    itemManagerView.itemsView[index].GetComponent<Image>().color = Color.red;
                    itemManagerView.itemsView[index].IsDrag = true;
                    break;
                case "navigation":
                    Button[] btns = navigationView.GetComponentsInChildren<Button>();
                    foreach (Button btn in btns)
                    {
                        btn.GetComponent<RoundCorner>().color = Color.white;
                    }
                    btns[index].GetComponent<RoundCorner>().color = new Color(255 / 255.0f, 150 / 255.0f, 150 / 255.0f, 255 / 255.0f);
                    break;
                default:
                    Debug.LogError("û�и����ͱ߿�����޸�!!!");
                    break;
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="item"></param>
        public void reduceQuantityUI(Item item)
        {
            itemManagerView.reduceQuantityUI(getIndex(item));
        }
        #endregion

        public void showInfo(Item data)
        {
            if (infoView == null)
            {
                Debug.LogError("infoView is null!!!");
                return;
            }
            infoView.showInfo(data);
        }
    }
}

