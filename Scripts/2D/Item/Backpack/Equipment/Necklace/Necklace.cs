namespace LAB2D
{
    using System;

    /// <summary>
    /// 项链
    /// </summary>
    [Serializable]
    public abstract class Necklace : AEquipment
    {
        public Necklace()
        {
            this.EquipTypeValue = EquipType.Necklace;
        }
    }

    /// <summary>
    /// 项链对象
    /// </summary>
    public abstract class NecklaceObject : AEquipmentObject
    {
    }
}
