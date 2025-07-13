using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Shield : Equipment
    {
        public Shield()
        {
            equipType = EquipType.Shield;
        }
    }

    public abstract class ShieldObject : EquipmentObject
    {
    }
}
