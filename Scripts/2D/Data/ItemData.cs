using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// 不能将ItemData转换为json,因为需要[Serializable]修饰,而包装类没有被修饰
    /// 公共的数据，仅存储一份
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int id; // 标识符
        public string itemName; // 名字
        public string imageName;  // 获得对应的图片
        public string info; // 简介
        public bool isStackable; // 是否可堆叠
        public ItemType type; // 道具类型

        public class ItemDataBuilder
        {
            private ItemData itemData;

            public ItemDataBuilder()
            {
                itemData = new ItemData();
            }

            public ItemDataBuilder setId(int id)
            {
                itemData.id = id;
                return this;
            }

            public ItemDataBuilder setItemName(string itemName)
            {
                itemData.itemName = itemName;
                return this;
            }

            public ItemDataBuilder setImageName(string imageName)
            {
                itemData.imageName = imageName;
                return this;
            }

            public ItemDataBuilder setInfo(string info)
            {
                itemData.info = info;
                return this;
            }

            public ItemDataBuilder setIsStackable(bool isStackable)
            {
                itemData.isStackable = isStackable;
                return this;
            }

            public ItemDataBuilder setItemType(ItemType type)
            {
                itemData.type = type;
                return this;
            }

            public ItemData build()
            {
                return itemData;
            }
        }
    }

    /// <summary>
    /// 所有Backpack与Build均在该Enum中
    /// </summary>
    [Serializable]
    public enum ItemType
    {
        // 背包
        Weapon=0, // 武器
        Equipment, // 装备
        Consumable, // 道具
        Material, // 材料
        Task, // 任务用品
        Other,
        //
        Room=6,
        Wall,
        BuildOther
    }
}
