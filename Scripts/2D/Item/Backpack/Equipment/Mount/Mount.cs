namespace LAB2D
{
    using System;

    /// <summary>
    /// 坐骑
    /// </summary>
    [Serializable]
    public abstract class Mount : AEquipment
    {
        public Mount()
        {
            this.EquipTypeValue = EquipType.Mount;
        }
    }

    /// <summary>
    /// 坐骑对象
    /// </summary>
    public abstract class MountObject : AEquipmentObject
    {
    }
}
