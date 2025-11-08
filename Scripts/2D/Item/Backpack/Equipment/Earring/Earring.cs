namespace LAB2D
{
    using System;

    /// <summary>
    /// 耳环
    /// </summary>
    [Serializable]
    public abstract class Earring : AEquipment
    {
        public Earring()
        {
            this.EquipTypeValue = EquipType.Earring;
        }
    }

    /// <summary>
    /// 耳环对象
    /// </summary>
    public abstract class EarringObject : AEquipmentObject
    {
    }
}
