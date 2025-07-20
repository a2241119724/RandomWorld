namespace LAB2D
{
    using System;

    /// <summary>
    /// 翅膀
    /// </summary>
    [Serializable]
    public abstract class Wing : Equipment
    {
        public Wing()
        {
            this.EquipTypeValue = EquipType.Wing;
        }
    }

    /// <summary>
    /// 翅膀对象
    /// </summary>
    public abstract class WingObject : EquipmentObject
    {
    }
}
