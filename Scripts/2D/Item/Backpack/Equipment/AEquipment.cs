namespace LAB2D
{
    using System;

    /// <summary>
    /// 装备
    /// </summary>
    [Serializable]
    public abstract class AEquipment : ABackpackItem
    {
        /// <summary>
        /// 装备类型
        /// </summary>
        public EquipTypeEnum EquipType;

        /// <summary>
        /// 物理攻击力
        /// </summary>
        public float ATN;

        /// <summary>
        /// 魔法攻击力
        /// </summary>
        public float INT;

        /// <summary>
        /// 暴击率
        /// </summary>
        public float CRT;

        /// <summary>
        /// 暴击伤害
        /// </summary>
        public float CSD;

        /// <summary>
        /// 攻击力
        /// </summary>
        public float ATK;

        /// <summary>
        /// 防御力
        /// </summary>
        public float DEF;

        /// <summary>
        /// 速度，回避物理攻击之类的
        /// </summary>
        public float SPD;

        /// <summary>
        /// 命中率或者连击之类的
        /// </summary>
        public float HIT;

        /// <summary>
        /// 魔法防御力
        /// </summary>
        public float RES;

        /// <summary>
        /// 装备类型
        /// </summary>
        public enum EquipTypeEnum
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
    public abstract class AEquipmentObject : ABackpackItemObject
    {
    }
}