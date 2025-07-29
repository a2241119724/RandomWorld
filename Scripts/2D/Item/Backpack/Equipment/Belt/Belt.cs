namespace LAB2D
{
    using System;

    /// <summary>
    /// 腰带
    /// </summary>
    [Serializable]
    public abstract class Belt : Equipment
    {
        public Belt()
        {
            this.EquipTypeValue = EquipType.Belt;
        }
    }

    /// <summary>
    /// 腰带对象
    /// </summary>
    public abstract class BeltObject : EquipmentObject
    {
    }
}
