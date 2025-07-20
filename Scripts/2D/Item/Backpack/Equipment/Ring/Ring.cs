namespace LAB2D
{
    using System;

    /// <summary>
    /// 戒指
    /// </summary>
    [Serializable]
    public abstract class Ring : Equipment
    {
        public Ring()
        {
            this.EquipTypeValue = EquipType.Ring;
        }
    }

    /// <summary>
    /// 戒指对象
    /// </summary>
    public abstract class RingObject : EquipmentObject
    {
    }
}
