namespace LAB2D.MVC
{
    using LAB2D;
    using LAB2D.Data;
    using LAB2D.Item;
    using System.Collections.Generic;

    /// <summary>
    /// 模型
    /// 优化,只需要存id与数量
    /// </summary>
    public abstract class MVCModel : ASaveData
    {
        /// <summary>
        /// 道具列表
        /// </summary>
        public Dictionary<AItem.ItemTypeEnum, List<AItem>> ItemDict;

        public MVCModel(AItem.ItemTypeEnum start, AItem.ItemTypeEnum end)
        {
            this.ItemDict = new Dictionary<AItem.ItemTypeEnum, List<AItem>>();
            LAB2D.Tool.Tool.SplitEnum<AItem.ItemTypeEnum>(start, end).ForEach((item) =>
            {
                this.ItemDict.Add(item, new List<AItem>());
            });
        }

        /// <summary>
        /// 删除一个道具
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="index">道具索引</param>
        public void Delete(AItem.ItemTypeEnum type, int index)
        {
            if (this.ItemDict[type] == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // itemList[index] = null;
            this.ItemDict[type].RemoveAt(index);
        }

        /// <summary>
        /// 添加道具到背包
        /// </summary>
        /// <param name="item">道具信息</param>
        public void Add(AItem item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            List<AItem> itemList;
            AItem.ItemTypeEnum itemType = ItemDataManager.Instance.IdToType(item.Id);
            if (this.ItemDict.ContainsKey(itemType))
            {
                itemList = this.ItemDict[itemType];
            }
            else
            {
                itemList = new List<AItem>();
            }

            // 可以堆叠
            if (ItemDataManager.Instance.GetById(item.Id).IsStackable)
            {
                for (int i = 0; i < itemList.Count; i++)
                {
                    // 包括道具
                    if (itemList[i].Id == item.Id)
                    {
                        itemList[i].Quantity++;
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
        public void Exchange(AItem.ItemTypeEnum type, int index1, int index2)
        {
            if (index1 < 0 || index1 >= this.Count(type) || index2 < 0 || index2 >= this.Count(type))
            {
                LogManager.Instance.Log("index1 or index2 Not Exist!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            List<AItem> itemList = this.ItemDict[type];
            AItem temp = itemList[index1];
            itemList[index1] = itemList[index2];
            itemList[index2] = temp;
        }

        /// <summary>
        /// 减少道具数量
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="item">道具</param>
        public void ReduceQuantity(AItem.ItemTypeEnum type, AItem item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.ItemDict[type][this.GetIndex(type, item)].Quantity--;
        }

        /// <summary>
        /// 获取道具信息
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <param name="index">道具的索引</param>
        /// <returns>道具信息</returns>
        public AItem Get(AItem.ItemTypeEnum type, int index)
        {
            if (index < 0 || index >= this.Count(type))
            {
                LogManager.Instance.Log("index Not Exist!!!", LogManager.LogLevelEnum.Error);
                return null;
            }

            return this.ItemDict[type][index];
        }

        /// <summary>
        /// 获取道具数量
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <returns>数量</returns>
        public int Count(AItem.ItemTypeEnum type)
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
        public int GetIndex(AItem.ItemTypeEnum type, AItem item)
        {
            if (item == null)
            {
                LogManager.Instance.Log("item is null!!!", LogManager.LogLevelEnum.Error);
                return -1;
            }

            List<AItem> itemList = this.ItemDict[type];
            for (int i = 0; i < itemList.Count; i++)
            {
                if (itemList[i].Id == item.Id)
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
        public bool IsNull(AItem.ItemTypeEnum type)
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
