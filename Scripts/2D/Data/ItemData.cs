namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 不能将ItemData转换为json,因为需要[Serializable]修饰,而包装类没有被修饰
    /// 公共的数据，仅存储一份
    /// </summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>
        /// 唯一标识符
        /// </summary>
        public int Id;

        /// <summary>
        /// 物品名称(中文)
        /// </summary>
        public string ItemName;

        /// <summary>
        /// 图片名称(英文)
        /// </summary>
        public string ImageName;

        /// <summary>
        /// 物品信息
        /// </summary>
        public string Info;

        /// <summary>
        /// 是否可堆叠
        /// </summary>
        public bool IsStackable;

        /// <summary>
        /// 物品类型
        /// </summary>
        public Item.ItemType Type;

        /// <summary>
        /// 建造者
        /// </summary>
        public class ItemDataBuilder
        {
            private readonly ItemData itemData;

#pragma warning disable SA1600 // Elements should be documented
            public ItemDataBuilder()
            {
                this.itemData = new ItemData();
            }

            public ItemDataBuilder SetId(int id)
            {
                this.itemData.Id = id;
                return this;
            }

            public ItemDataBuilder SetItemName(string itemName)
            {
                this.itemData.ItemName = itemName;
                return this;
            }

            public ItemDataBuilder SetImageName(string imageName)
            {
                this.itemData.ImageName = imageName;
                return this;
            }

            public ItemDataBuilder SetInfo(string info)
            {
                this.itemData.Info = info;
                return this;
            }

            public ItemDataBuilder SetIsStackable(bool isStackable)
            {
                this.itemData.IsStackable = isStackable;
                return this;
            }

            public ItemDataBuilder SetItemType(Item.ItemType type)
            {
                this.itemData.Type = type;
                return this;
            }

            public ItemData Build()
            {
                return this.itemData;
            }
#pragma warning restore SA1600 // Elements should be documented
        }
    }
}
