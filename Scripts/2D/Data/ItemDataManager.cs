namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item;
    using LAB2D.Item.Backpack.Equipment;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Id按1000间隔分组
    /// 起始Id与枚举类型BackpackItemType、BuildItemType保持一致
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
            ServiceLocator.Register<ItemDataManager>(this);
            this.nameToId = new Dictionary<string, int>();
            this.allItemInfo = new Dictionary<int, ItemData>();

            AItem.ItemTypeEnum[] itemTypes = AItem.Ranges["Build"];
            for (int type = (int)itemTypes[0]; type <= (int)itemTypes[1]; type++)
            {
                BuildItemDataSO itemDataSO = ServiceLocator.Get<ResourceManager>().GetBuildSO(((AItem.ItemTypeEnum)type).ToString() + "ItemData");
                if (itemDataSO == null)
                {
                    continue;
                }

                foreach (BuildItemData itemData in itemDataSO.GetExpandedItems())
                {
                    int id = Convert.ToInt32(itemData.Id);
                    this.allItemInfo.Add(id, itemData);
                    this.nameToId.Add(itemData.EnName, id);
                }
            }

            itemTypes = AItem.Ranges["Backpack"];
            List<ItemData> equipmentData = null;
            for (int type = (int)itemTypes[0]; type <= (int)itemTypes[1]; type++)
            {
                string itemType = ((AItem.ItemTypeEnum)type).ToString();
                ItemDataSO itemDataSO = ServiceLocator.Get<ResourceManager>().GetBackpackSO(itemType + "ItemData");
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

                // 初始化装备实例
                if (itemType.Equals("Equipment"))
                {
                    equipmentData = itemDataSO.ItemDatas;
                }
            }

            itemTypes = AItem.Ranges["Resource"];
            for (int type = (int)itemTypes[0]; type <= (int)itemTypes[1]; type++)
            {
                string itemType = ((AItem.ItemTypeEnum)type).ToString();
                ItemDataSO itemDataSO = ServiceLocator.Get<ResourceManager>().GetBackpackSO(itemType + "ItemData");
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

            // 最后初始化背包道具实例
            ServiceLocator.Get<ItemInstanceFactory>().InitItemInstances(equipmentData);
        }

        /// <summary>
        /// 获得对应id的道具数据
        /// </summary>
        /// <param name="id">道具标识</param>
        /// <returns>道具数据</returns>
        public ItemData GetById(int id)
        {
            if (!this.allItemInfo.TryGetValue(id, out ItemData itemData))
            {
                AWorkerTask.LogProvider("没有id的道具!!!", LogManager.LogLevelEnum.Error);
                return null;
            }

            return itemData;
        }

        /// <summary>
        /// 通过名字获得数据
        /// </summary>
        /// <param name="name">名字</param>
        /// <returns>道具数据</returns>
        public ItemData GetByName(string name)
        {
            if (!this.TryGetByName(name, out ItemData itemData))
            {
                AWorkerTask.LogProvider("没有名字为" + name + "的道具!!!", LogManager.LogLevelEnum.Warning);
                return ItemData.Empty;
            }

            return itemData;
        }

        /// <summary>
        /// 尝试通过名字获得数据，不打印缺失日志。
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="itemData">道具数据</param>
        /// <returns>是否存在</returns>
        public bool TryGetByName(string name, out ItemData itemData)
        {
            itemData = ItemData.Empty;
            if (!this.nameToId.TryGetValue(name, out int id))
            {
                return false;
            }

            itemData = this.GetById(id);
            return itemData != null;
        }

        /// <summary>
        /// 通过ID获取道具数据
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>道具类型</returns>
        public AItem.ItemTypeEnum IdToType(int id)
        {
            if (id < 0)
            {
                AWorkerTask.LogProvider($"id:{id}小于0!!!", LogManager.LogLevelEnum.Warning);
                return AItem.ItemTypeEnum.Null;
            }

            return (AItem.ItemTypeEnum)(object)(id / TypeInterval);
        }

        /// <summary>
        /// 通过ID获取装备类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>装备类型</returns>
        public AEquipment.EquipTypeEnum IdToEquipmentType(int id)
        {
            if (this.IdToType(id) != AItem.ItemTypeEnum.Equipment)
            {
                AWorkerTask.LogProvider("id不是装备!!!", LogManager.LogLevelEnum.Error);
                return AEquipment.EquipTypeEnum.Null;
            }

            id -= ((int)AItem.ItemTypeEnum.Equipment) * TypeInterval;

            // 最多10种装备
            return (AEquipment.EquipTypeEnum)(object)(id * 10 / TypeInterval);
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
        public int GetIndexByType(AItem.ItemTypeEnum type)
        {
            if ((int)type < (int)AItem.ItemTypeEnum.Room)
            {
                return (int)type;
            }
            else
            {
                return (int)type - (int)AItem.ItemTypeEnum.Room;
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
