using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    // 优化,只需要存id与数量
    public abstract class MVCModel : ASaveData
    {
        /// <summary>
        /// 道具列表
        /// </summary>
        public Dictionary<ItemType, ArrayList> itemDict;

        public MVCModel()
        {
            itemDict = new Dictionary<ItemType, ArrayList>();
            if (itemDict == null)
            {
                Debug.LogError("itemDict assign resource Error!!!");
            }
        }

        #region CRUD
        /// <summary>
        /// 删除一个道具
        /// </summary>
        /// <param name="index">道具索引</param>
        public void deleteItem(ItemType type,int index)
        {
            if (itemDict[type] == null)
            {
                Debug.LogError("item is null!!!");
                return;
            }
            //itemList[index] = null;
            itemDict[type].RemoveAt(index);
        }

        /// <summary>
        /// 添加道具到背包
        /// </summary>
        /// <param name="item">道具信息</param>
        public void addItem(Item item)
        {
            if (item == null)
            {
                Debug.LogError("没有该物品类型!!!");
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
            // 可以堆叠
            if (ItemDataManager.Instance.getById(item.id).isStackable)
            {
                for (int i = 0; i < itemList.Count; i++)
                {
                    // 包括道具
                    if (((Item)itemList[i]).id == item.id)
                    {
                        ((Item)itemList[i]).quantity++;
                        return;
                    }
                }
            }
            // 不包括道具,添加
            item.quantity = 1;
            itemList.Add(item);
        }

        /// <summary>
        /// 交换道具位置
        /// </summary>
        /// <param name="index1">道具1的索引</param>
        /// <param name="index2">道具2的索引</param>
        public void exchangeItem(ItemType type, int index1, int index2)
        {
            if (index1 < 0 || index1 >= count(type) || index2 < 0 || index2 >= count(type))
            {
                Debug.LogError("index1 or index2 Not Exist!!!");
                return;
            }
            ArrayList itemList = itemDict[type];
            Item temp = (Item)itemList[index1];
            itemList[index1] = itemList[index2];
            itemList[index2] = temp;
        }

        /// <summary>
        /// 减少道具数量
        /// </summary>
        /// <param name="index">道具索引</param>
        public void reduceQuantity(ItemType type, Item item)
        {
            if (item == null)
            {
                Debug.LogError("item is null!!!");
                return;
            }
            ((Item)itemDict[type][getIndex(type, (Weapon)item)]).quantity--;
        }
        #endregion

        #region GET
        /// <summary>
        /// 获取道具信息
        /// </summary>
        /// <param name="index">道具的索引</param>
        /// <returns>道具信息</returns>
        public Item get(ItemType type, int index)
        {
            if (index < 0 || index >= count(type))
            {
                Debug.LogError("index Not Exist!!!");
                return null;
            }
            return (Item)(itemDict[type][index]);
        }

        /// <summary>
        /// 获取道具数量
        /// </summary>
        /// <returns>数量</returns>
        public int count(ItemType type)
        {
            try
            {
                return itemDict[type].Count;
            }catch(KeyNotFoundException e)
            {
                Debug.Log(e);
            }
            return 0;
        }

        /// <summary>
        /// 获取道具的索引(错的)
        /// </summary>
        /// <param name="item">道具</param>
        /// <returns></returns>
        public int getIndex(ItemType type, Weapon item)
        {
            if (item == null)
            {
                Debug.LogError("item is null");
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
        /// 背包中是否有物品
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
