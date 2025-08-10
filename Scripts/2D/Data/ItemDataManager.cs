namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Id间隔1000
    /// Start Id 根据枚举类型BackpackItemType,BuildItemType一致
    /// </summary>
    public class ItemDataManager : MonoBehaviour
    {
        private const int TypeInterval = 100000;
        private Dictionary<int, ItemData> allItemInfo;
        private Dictionary<string, int> nameToId;

        /// <summary>
        /// 单例
        /// </summary>
        public static ItemDataManager Instance { get; private set; }

        /// <summary>
        /// 获得对应id的道具数据
        /// </summary>
        /// <param name="id">道具标识</param>
        /// <returns>道具数据</returns>
        public ItemData GetById(int id)
        {
            if (!this.allItemInfo.ContainsKey(id))
            {
                LogManager.Instance.Log("没有case该id的道具!!!", LogManager.LogLevel.Error);
                return null;
            }

            return this.allItemInfo[id];
        }

        /// <summary>
        /// 通过名字获得数据
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>道具数据</returns>
        public ItemData GetByName(string name)
        {
            if (!this.nameToId.ContainsKey(name))
            {
                LogManager.Instance.Log("没有名字为" + name + "的道具!!!", LogManager.LogLevel.Error);
                return null;
            }

            return this.GetById(this.nameToId[name]);
        }

        /// <summary>
        /// 通过ID获取道具数据
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>道具类型
        /// </returns>
        public Item.ItemType GetTypeById(int id)
        {
            if (id < 0)
            {
                return Item.ItemType.Null;
            }

            return (Item.ItemType)(object)(id / TypeInterval);
        }

        /// <summary>
        /// 通过ID获取装备类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>装备类型</returns>
        public Equipment.EquipType GetEquipmentTypeById(int id)
        {
            if (this.GetTypeById(id) != Item.ItemType.Equipment)
            {
                return Equipment.EquipType.Null;
            }

            id -= ((int)Item.ItemType.Equipment) * TypeInterval;

            // 最多10种装备
            return (Equipment.EquipType)(object)(id * 10 / TypeInterval);
        }

        /// <summary>
        /// 由于Item.ItemType包含了所有类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>道具类型</returns>
        public Item.ItemType GetIndexById(int id)
        {
            id /= 1000;
            if (id < (int)Item.ItemType.Room)
            {
                return (Item.ItemType)(object)id;
            }
            else
            {
                return (Item.ItemType)(object)(id - (int)Item.ItemType.Room);
            }
        }

        /// <summary>
        /// 由于Item.ItemType包含了所有类型
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <returns>索引</returns>
        public int GetIndexByType(Item.ItemType type)
        {
            if ((int)type < (int)Item.ItemType.Room)
            {
                return (int)type;
            }
            else
            {
                return (int)type - (int)Item.ItemType.Room;
            }
        }

        public void Awake()
        {
            Instance = this;
            this.nameToId = new Dictionary<string, int>();
            this.allItemInfo = new Dictionary<int, ItemData>();
            foreach (Item.ItemType itemType in Enum.GetValues(typeof(Item.ItemType)))
            {
                string[] data = DataTool.LoadCSV(ResourceConstant.DATA_ROOT + itemType.ToString() + "ItemData");
                if (data == null)
                {
                    continue;
                }

                int len = data.Length;
                int start_id = (int)itemType * TypeInterval;

                // 跳过第一行
                for (int i = 1; i < len; i++)
                {
                    string[] cols = data[i].Split(',');
                    int id = Convert.ToInt32(cols[0]) == -1 ? i - 1 : Convert.ToInt32(cols[0]);
                    if (id < i - 1)
                    {
                        LogManager.Instance.Log("id不对应!!!请检查数据", LogManager.LogLevel.Error);
                    }

                    id += start_id;
                    this.allItemInfo.Add(id, new ItemData.ItemDataBuilder()
                        .SetId(id).SetItemName(cols[1]).SetImageName(cols[2])
                        .SetInfo(cols[3]).SetIsStackable(cols[4].Equals("TRUE")).Build());
                    this.nameToId.Add(cols[2], id);
                }
            }
        }
    }
}
