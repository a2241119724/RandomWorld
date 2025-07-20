namespace LAB2D
{
    using System;

    /// <summary>
    /// 帽子
    /// </summary>
    [Serializable]
    public abstract class Head : Equipment
    {
        public Head()
        {
            this.EquipTypeValue = EquipType.Head;
        }
    }

    /// <summary>
    /// 帽子对象
    /// </summary>
    public abstract class HeadObject : EquipmentObject
    {
    }
}
