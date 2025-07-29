namespace LAB2D
{
    using System;

    /// <summary>
    /// 上衣
    /// </summary>
    [Serializable]
    public abstract class Body : Equipment
    {
        public Body()
        {
            this.EquipTypeValue = EquipType.Body;
        }
    }

    /// <summary>
    /// 上衣对象
    /// </summary>
    public abstract class BodyObject : EquipmentObject
    {
    }
}
