namespace LAB2D
{
    using System;

    /// <summary>
    /// 腰带
    /// </summary>
    [Serializable]
    public abstract class Belt : AEquipment
    {
        public Belt()
        {
            this.EquipTypeValue = EquipType.Belt;
        }
    }
}
