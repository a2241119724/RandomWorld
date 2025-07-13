using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Bracelet : Equipment
    {
        public Bracelet()
        {
            equipType = EquipType.Bracelet;
        }
    }

    public abstract class BraceletObject : EquipmentObject
    {
    }
}

