namespace LAB2D
{
    using System;

    /// <summary>
    /// 项链
    /// </summary>
    [Serializable]
    public abstract class Necklace : Equipment
    {
        public Necklace()
        {
            this.EquipTypeValue = EquipType.Necklace;
        }
    }

    /// <summary>
    /// 项链对象
    /// </summary>
    public abstract class NecklaceObject : EquipmentObject
    {
    }
}
