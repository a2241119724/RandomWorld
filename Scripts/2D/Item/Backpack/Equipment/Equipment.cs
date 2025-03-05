using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Equipment : BackpackItem
    {
        public EquipType equipType; //装备类型

        public enum EquipType
        {
            Head,   //头部
            Body,   //上衣
            Trouser, // 裤子
            Shoes,  //鞋子
            Weapon, //武器
            Shield, //盾牌
            Ring,   //戒指
            Necklace, //项链
            Bracelet, //手镯
            Belt,   //腰带
            Earring, //耳环
            Wing,   //翅膀
            Mount,  //坐骑
            Pet,    //宠物
            Null
        }
    }

    public abstract class EquipmentObject : BackpackItemObject
    {
    }
}