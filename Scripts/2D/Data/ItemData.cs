using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// ���ܽ�ItemDataת��Ϊjson,��Ϊ��Ҫ[Serializable]����,����װ��û�б�����
    /// ���������ݣ����洢һ��
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public int id; // ��ʶ��
        public string itemName; // ����
        public string imageName;  // ��ö�Ӧ��ͼƬ
        public string info; // ���
        public bool isStackable; // �Ƿ�ɶѵ�
        public ItemType type; // ��������

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
    /// ����Backpack��Build���ڸ�Enum��
    /// </summary>
    [Serializable]
    public enum ItemType
    {
        // ����
        Weapon=0, // ����
        Equipment, // װ��
        Consumable, // ����
        Material, // ����
        Task, // ������Ʒ
        Other,
        //
        Room=6,
        Wall,
        BuildOther
    }
}
