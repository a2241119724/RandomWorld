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

        public void Awake()
        {
            Instance = this;
            this.nameToId = new Dictionary<string, int>();
            this.allItemInfo = new Dictionary<int, ItemData>();

            Item.ItemType[] itemTypes = Item.Ranges["Backpack"];
            for (int type = (int)itemTypes[0]; type <= (int)itemTypes[1]; type++)
            {
                ItemDataSO itemDataSO = ResourceManager.Instance.GetBackpackSO(((Item.ItemType)type).ToString() + "ItemData");
                if (itemDataSO == null)
                {
                    continue;
                }

                foreach (ItemData itemData in itemDataSO.ItemDatas)
                {
                    int id = Convert.ToInt32(itemData.Id);
                    this.allItemInfo.Add(id, itemData);
                    this.nameToId.Add(itemData.EnName, id);
                }
            }

            itemTypes = Item.Ranges["Build"];
            for (int type = (int)itemTypes[0]; type <= (int)itemTypes[1]; type++)
            {
                BuildItemDataSO itemDataSO = ResourceManager.Instance.GetBuildSO(((Item.ItemType)type).ToString() + "ItemData");
                if (itemDataSO == null)
                {
                    continue;
                }

                foreach (BuildItemData itemData in itemDataSO.BuildItemDatas)
                {
                    int id = Convert.ToInt32(itemData.Id);
                    this.allItemInfo.Add(id, itemData);
                    this.nameToId.Add(itemData.EnName, id);
                }
            }
        }

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
        /// <returns>道具类型</returns>
        public Item.ItemType IdToType(int id)
        {
            if (id < 0)
            {
                LogManager.Instance.Log("id小于0!!!", LogManager.LogLevel.Error);
                return Item.ItemType.Null;
            }

            return (Item.ItemType)(object)(id / TypeInterval);
        }

        /// <summary>
        /// 通过ID获取装备类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>装备类型</returns>
        public Equipment.EquipType IdToEquipmentType(int id)
        {
            if (this.IdToType(id) != Item.ItemType.Equipment)
            {
                LogManager.Instance.Log("id不是装备!!!", LogManager.LogLevel.Error);
                return Equipment.EquipType.Null;
            }

            id -= ((int)Item.ItemType.Equipment) * TypeInterval;

            // 最多10种装备
            return (Equipment.EquipType)(object)(id * 10 / TypeInterval);
        }

        /// <summary>
        /// 由于Item.ItemType包含了所有类型
        /// 获取Button导航索引
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>button索引</returns>
        public int GetIndexById(int id)
        {
            return this.GetIndexByType(this.IdToType(id));
        }

        /// <summary>
        /// 由于Item.ItemType包含了所有类型
        /// 获取Button导航索引
        /// </summary>
        /// <param name="type">道具类型</param>
        /// <returns>button索引</returns>
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

        /// <summary>
        /// 通过名称获取建造道具
        /// </summary>
        /// <param name="name">道具名称</param>
        /// <returns>建造道具</returns>
        public BuildItemData GetBuildItemDataByName(string name)
        {
            ItemData itemData = this.GetByName(name);
            return (BuildItemData)itemData;
        }
    }
}
