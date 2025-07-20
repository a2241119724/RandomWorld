namespace LAB2D
{
    using System;

    /// <summary>
    /// 装备
    /// </summary>
    [Serializable]
    public abstract class Equipment : BackpackItem
    {
        /// <summary>
        /// 装备类型
        /// </summary>
        public EquipType EquipTypeValue;

        /// <summary>
        /// 装备类型
        /// </summary>
        public enum EquipType
        {
            /// <summary>
            /// 头部
            /// </summary>
            Head,

            /// <summary>
            /// 上衣
            /// </summary>
            Body,

            /// <summary>
            /// 裤子
            /// </summary>
            Trouser,

            /// <summary>
            /// 鞋子
            /// </summary>
            Shoes,

            /// <summary>
            /// 武器
            /// </summary>
            Weapon,

            /// <summary>
            /// 盾牌
            /// </summary>
            Shield,

            /// <summary>
            /// 戒指
            /// </summary>
            Ring,

            /// <summary>
            /// 项链
            /// </summary>
            Necklace,

            /// <summary>
            /// 手镯
            /// </summary>
            Bracelet,

            /// <summary>
            /// 腰带
            /// </summary>
            Belt,

            /// <summary>
            /// 耳环
            /// </summary>
            Earring,

            /// <summary>
            /// 翅膀
            /// </summary>
            Wing,

            /// <summary>
            /// 坐骑
            /// </summary>
            Mount,

            /// <summary>
            /// 宠物
            /// </summary>
            Pet,

            /// <summary>
            /// 空
            /// </summary>
            Null,
        }
    }

    /// <summary>
    /// 装备对象
    /// </summary>
    public abstract class EquipmentObject : BackpackItemObject
    {
    }
}