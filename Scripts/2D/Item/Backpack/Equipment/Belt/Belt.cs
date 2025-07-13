using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Belt : Equipment
    {
        public Belt()
        {
            equipType = EquipType.Belt;
        }
    }

    public abstract class BeltObject : EquipmentObject
    {
    }
}
