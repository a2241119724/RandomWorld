namespace LAB2D
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Model
    /// 优化,只需要存id与数量
    /// </summary>
    public abstract class MVCModel : ASaveData
    {
        /// <summary>
        /// 道具列表
        /// </summary>
        public Dictionary<ItemType, ArrayList> ItemDict;

        public MVCModel(ItemType start, ItemType end)
        {
            this.ItemDict = new Dictionary<ItemType, ArrayList>();
            Tool.SplitEnum<ItemType>(start, end).ForEach((item) =>
            {
                this.ItemDict.Add(item, new ArrayList());
            });
        }

        /// <summary>
        /// 删除一个道具
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="index">道具索引</param>
        public void Delete(ItemType type, int index)
        {
            if (this.ItemDict[type] == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }

            // itemList[index] = null;
            this.ItemDict[type].RemoveAt(index);
        }

        /// <summary>
        /// 添加道具到背包
        /// </summary>
        /// <param name="item">道具信息</param>
        public void Add(Item item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }

            ArrayList itemList;
            ItemType itemType = ItemDataManager.Instance.GetTypeById(item.Id);
            if (this.ItemDict.ContainsKey(itemType))
            {
                itemList = this.ItemDict[itemType];
            }
            else
            {
                itemList = new ArrayList();
            }

            // 可以堆叠
            if (ItemDataManager.Instance.GetById(item.Id).IsStackable)
            {
                for (int i = 0; i < itemList.Count; i++)
                {
                    // 包括道具
                    if (((Item)itemList[i]).Id == item.Id)
                    {
                        ((Item)itemList[i]).Quantity++;
                        return;
                    }
                }
            }

            // 不包括道具,添加
            item.Quantity = 1;
            itemList.Add(item);
        }

        /// <summary>
        /// 交换道具位置
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="index1">道具1的索引</param>
        /// <param name="index2">道具2的索引</param>
        public void Exchange(ItemType type, int index1, int index2)
        {
            if (index1 < 0 || index1 >= this.Count(type) || index2 < 0 || index2 >= this.Count(type))
            {
                LogManager.Instance.Log("index1 or index2 Not Exist!!!", LogManager.LogLevel.Error);
                return;
            }

            ArrayList itemList = this.ItemDict[type];
            Item temp = (Item)itemList[index1];
            itemList[index1] = itemList[index2];
            itemList[index2] = temp;
        }

        /// <summary>
        /// 减少道具数量
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="item">道具</param>
        public void ReduceQuantity(ItemType type, Item item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevel.Error);
                return;
            }

            ((Item)this.ItemDict[type][this.GetIndex(type, (Weapon)item)]).Quantity--;
        }

        /// <summary>
        /// 获取道具信息
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="index">道具的索引</param>
        /// <returns>道具信息</returns>
        public Item Get(ItemType type, int index)
        {
            if (index < 0 || index >= this.Count(type))
            {
                LogManager.Instance.Log("index Not Exist!!!", LogManager.LogLevel.Error);
                return null;
            }

            return (Item)this.ItemDict[type][index];
        }

        /// <summary>
        /// 获取道具数量
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <returns>数量</returns>
        public int Count(ItemType type)
        {
            if (!this.ItemDict.ContainsKey(type))
            {
                return 0;
            }

            return this.ItemDict[type].Count;
        }

        /// <summary>
        /// 获取道具的索引(错的)
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="item">道具</param>
        /// <returns>索引</returns>
        public int GetIndex(ItemType type, Weapon item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevel.Error);
                return -1;
            }

            ArrayList itemList = this.ItemDict[type];
            for (int i = 0; i < itemList.Count; i++)
            {
                if (((Item)itemList[i]).Id == item.Id)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 背包中是否有物品
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <returns>是否</returns>
        public bool IsNull(ItemType type)
        {
            return this.Count(type) == 0;
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
        }
    }
}
