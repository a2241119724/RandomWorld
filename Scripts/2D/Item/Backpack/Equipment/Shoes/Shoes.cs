namespace LAB2D
{
    using System;

    /// <summary>
    /// 鞋子
    /// </summary>
    [Serializable]
    public abstract class Shoes : AEquipment
    {
        public Shoes()
        {
            this.EquipTypeValue = EquipType.Shoes;
        }
    }

    /// <summary>
    /// 鞋子对象
    /// </summary>
    public abstract class ShoesObject : AEquipmentObject
    {
    }
}
