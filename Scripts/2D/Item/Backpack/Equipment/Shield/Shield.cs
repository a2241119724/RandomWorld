namespace LAB2D
{
    using System;

    /// <summary>
    /// 盾牌
    /// </summary>
    [Serializable]
    public abstract class Shield : AEquipment
    {
        public Shield()
        {
            this.EquipTypeValue = EquipType.Shield;
        }
    }

    /// <summary>
    /// 盾牌对象
    /// </summary>
    public abstract class ShieldObject : AEquipmentObject
    {
    }
}
