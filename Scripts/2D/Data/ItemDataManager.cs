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
            string[] data = Tool.getCSV(ResourceConstant.DATA_ROOT + "ItemData");
            int len = data.Length;
            // 跳过第一行
            for(int i=1;i< len; i++)
            {
                string[] cols = data[i].Split(',');
                allItemInfo.Add(Convert.ToInt32(cols[0]), new ItemData.ItemDataBuilder()
                    .setId(Convert.ToInt32(cols[0])).setItemName(cols[1])
                    .setImageName(cols[2]).setInfo(cols[3]).setIsStackable(cols[4].Equals("TRUE"))
                    .build());
                nameToId.Add(cols[2], Convert.ToInt32(cols[0]));
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
                Debug.LogError("没有case该id的道具!!!");
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
                Debug.LogError("没有名字为"+ name + "的道具!!!");
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
    }
}

