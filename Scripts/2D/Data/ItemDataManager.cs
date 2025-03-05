using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// Id间隔1000
    /// Start Id 根据枚举类型BackpackItemType,BuildItemType一致
    /// </summary>
    public class ItemDataManager : MonoBehaviour
    {
        public static ItemDataManager Instance { private set; get; }

        private Dictionary<int,ItemData> allItemInfo;
        private Dictionary<string, int> nameToId;
        private const int typeInterval = 100000;

        private void Awake()
        {
            Instance = this;
            nameToId = new Dictionary<string, int>();
            allItemInfo = new Dictionary<int, ItemData>();
            foreach(ItemType itemType in Enum.GetValues(typeof(ItemType)))
            {
                string[] data = Tool.getCSV(ResourceConstant.DATA_ROOT + itemType.ToString() + "ItemData");
                if (data == null) continue;
                int len = data.Length;
                int start_id = (int)itemType * typeInterval;
                // 跳过第一行
                for (int i = 1; i < len; i++)
                {
                    string[] cols = data[i].Split(',');
                    int id = Convert.ToInt32(cols[0]) == -1 ? i - 1 : Convert.ToInt32(cols[0]);
                    if(id < i - 1)
                    {
                        LogManager.Instance.log("id不对应!!!请检查数据", LogManager.LogLevel.Error);
                    }
                    id += start_id;
                    allItemInfo.Add(id, new ItemData.ItemDataBuilder()
                        .setId(id).setItemName(cols[1]).setImageName(cols[2])
                        .setInfo(cols[3]).setIsStackable(cols[4].Equals("TRUE")).build());
                    nameToId.Add(cols[2], id);
                }
            }
        }

        /// <summary>
        /// 获得对应id的道具数据
        /// </summary>
        /// <param name="id">道具标识</param>
        /// <returns>道具数据</returns>
        public ItemData getById(int id)
        {
            if (!allItemInfo.ContainsKey(id))
            {
                LogManager.Instance.log("没有case该id的道具!!!", LogManager.LogLevel.Error);
                return null;
            }
            return allItemInfo[id];
        }

        /// <summary>
        /// 通过名字获得数据
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ItemData getByName(string name)
        {
            if (!nameToId.ContainsKey(name))
            {
                LogManager.Instance.log("没有名字为" + name + "的道具!!!", LogManager.LogLevel.Error);
                return null;
            }
            return getById(nameToId[name]);
        }

        public ItemType getTypeById(int id)
        {
            if (id < 0) return ItemType.Null;
            return (ItemType)(object)(id / typeInterval);
        }

        public Equipment.EquipType getEquipmentTypeById(int id) {
            if (getTypeById(id) != ItemType.Equipment) return Equipment.EquipType.Null;
            id -= ((int)ItemType.Equipment) * typeInterval;
            // 最多10种装备
            return (Equipment.EquipType)(object)(id * 10 / typeInterval);
        }

        /// <summary>
        /// 由于ItemType包含了所有类型
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ItemType getIndexById(int id)
        {
            id /= 1000;
            if(id < (int)ItemType.Room)
            {
                return (ItemType)(object)(id);
            }
            else
            {
                return (ItemType)(object)(id - (int)ItemType.Room);
            }
        }

        /// <summary>
        /// 由于ItemType包含了所有类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public int getIndexByType(ItemType type)
        {
            if ((int)type < (int)ItemType.Room)
            {
                return (int)type;
            }
            else
            {
                return (int)type - (int)ItemType.Room;
            }
        }

        internal object getByName(object name)
        {
            throw new NotImplementedException();
        }
    }
}

