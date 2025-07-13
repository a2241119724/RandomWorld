using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Wing : Equipment
    {
        public Wing()
        {
            equipType = EquipType.Wing;
        }
    }

    public abstract class WingObject : EquipmentObject
    {
    }
}
