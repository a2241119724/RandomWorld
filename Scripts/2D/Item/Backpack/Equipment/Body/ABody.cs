namespace LAB2D
{
    using System;

    /// <summary>
    /// 上衣
    /// </summary>
    [Serializable]
    public abstract class ABody : AEquipment
    {
        public ABody()
        {
            this.EquipTypeValue = EquipType.Body;
        }
    }
}
