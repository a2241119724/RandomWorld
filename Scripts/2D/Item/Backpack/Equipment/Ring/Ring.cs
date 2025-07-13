using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Ring : Equipment
    {
        public Ring()
        {
            equipType = EquipType.Ring;
        }
    }

    public abstract class RingObject : EquipmentObject
    {
    }
}
