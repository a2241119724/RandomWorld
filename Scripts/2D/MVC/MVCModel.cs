using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    // �Ż�,ֻ��Ҫ��id������
    public abstract class MVCModel : ASaveData
    {
        /// <summary>
        /// �����б�
        /// </summary>
        public Dictionary<ItemType, ArrayList> itemDict;

        public MVCModel(ItemType start, ItemType end)
        {
            itemDict = new Dictionary<ItemType, ArrayList>();
            Tool.splitEnum<ItemType>(start, end).ForEach((item) =>
            {
                itemDict.Add(item, new ArrayList());
            });
        }

        #region CRUD
        /// <summary>
        /// ɾ��һ������
        /// </summary>
        /// <param name="index">��������</param>
        public void deleteItem(ItemType type,int index)
        {
            if (itemDict[type] == null)
            {
                LogManager.Instance.log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }
            //itemList[index] = null;
            itemDict[type].RemoveAt(index);
        }

        /// <summary>
        /// ��ӵ��ߵ�����
        /// </summary>
        /// <param name="item">������Ϣ</param>
        public void addItem(Item item)
        {
            if (item == null)
            {
                LogManager.Instance.log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }
            ArrayList itemList = null;
            ItemType itemType = ItemDataManager.Instance.getTypeById(item.id);
            if (itemDict.ContainsKey(itemType))
            {
                itemList = itemDict[itemType];
            }
            else
            {
                itemList = new ArrayList();
            }
            // ���Զѵ�
            if (ItemDataManager.Instance.getById(item.id).isStackable)
            {
                for (int i = 0; i < itemList.Count; i++)
                {
                    // ��������
                    if (((Item)itemList[i]).id == item.id)
                    {
                        ((Item)itemList[i]).quantity++;
                        return;
                    }
                }
            }
            // ����������,���
            item.quantity = 1;
            itemList.Add(item);
        }

        /// <summary>
        /// ��������λ��
        /// </summary>
        /// <param name="index1">����1������</param>
        /// <param name="index2">����2������</param>
        public void exchangeItem(ItemType type, int index1, int index2)
        {
            if (index1 < 0 || index1 >= count(type) || index2 < 0 || index2 >= count(type))
            {
                LogManager.Instance.log("index1 or index2 Not Exist!!!", LogManager.LogLevel.Error);
                return;
            }
            ArrayList itemList = itemDict[type];
            Item temp = (Item)itemList[index1];
            itemList[index1] = itemList[index2];
            itemList[index2] = temp;
        }

        /// <summary>
        /// ���ٵ�������
        /// </summary>
        /// <param name="index">��������</param>
        public void reduceQuantity(ItemType type, Item item)
        {
            if (item == null)
            {
                LogManager.Instance.log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }
            ((Item)itemDict[type][getIndex(type, (Weapon)item)]).quantity--;
        }
        #endregion

        #region GET
        /// <summary>
        /// ��ȡ������Ϣ
        /// </summary>
        /// <param name="index">���ߵ�����</param>
        /// <returns>������Ϣ</returns>
        public Item get(ItemType type, int index)
        {
            if (index < 0 || index >= count(type))
            {
                LogManager.Instance.log("index Not Exist!!!", LogManager.LogLevel.Error);
                return null;
            }
            return (Item)(itemDict[type][index]);
        }

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        /// <returns>����</returns>
        public int count(ItemType type)
        {
            if (!itemDict.ContainsKey(type)) return 0;
            return itemDict[type].Count;
        }

        /// <summary>
        /// ��ȡ���ߵ�����(���)
        /// </summary>
        /// <param name="item">����</param>
        /// <returns></returns>
        public int getIndex(ItemType type, Weapon item)
        {
            if (item == null)
            {
                LogManager.Instance.log("item is null!!!", LogManager.LogLevel.Error);
                return -1;
            }
            ArrayList itemList = itemDict[type];
            for (int i = 0; i < itemList.Count; i++)
            {
                if (((Item)itemList[i]).id == item.id)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// �������Ƿ�����Ʒ
        /// </summary>
        public bool isNull(ItemType type)
        {
            return count(type) == 0;
        }
        #endregion

        public override void loadData() { }
        public override void saveData() { }
    }
}
