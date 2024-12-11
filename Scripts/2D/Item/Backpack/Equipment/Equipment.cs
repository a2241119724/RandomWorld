using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Equipment : BackpackItem
    {
        public enum EquipType
        {
            Head,   //头部
            Body,   //上衣
            Leg,    //腿部
            Foot,   //足部
            Wrist,  //腕部
            Knee,   //膝盖
            Hand,   //手部
            Shoulder//肩膀
        }
    }

    public abstract class EquipmentObject : BackpackItemObject
    {
    }
}