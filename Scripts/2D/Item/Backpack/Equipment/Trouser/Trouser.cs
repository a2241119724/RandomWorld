namespace LAB2D
{
    using System;

    /// <summary>
    /// 裤子
    /// </summary>
    [Serializable]
    public abstract class Trouser : Equipment
    {
        public Trouser()
        {
            this.EquipTypeValue = EquipType.Trouser;
        }
    }

    /// <summary>
    /// 裤子对象
    /// </summary>
    public abstract class TrouserObject : EquipmentObject
    {
    }
}
