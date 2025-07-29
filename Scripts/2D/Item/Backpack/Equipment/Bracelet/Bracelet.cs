namespace LAB2D
{
    using System;

    /// <summary>
    /// 手链
    /// </summary>
    [Serializable]
    public abstract class Bracelet : Equipment
    {
        public Bracelet()
        {
            this.EquipTypeValue = EquipType.Bracelet;
        }
    }

    /// <summary>
    /// 手链对象
    /// </summary>
    public abstract class BraceletObject : EquipmentObject
    {
    }
}
