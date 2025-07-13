using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Necklace : Equipment
    {
        public Necklace()
        {
            equipType = EquipType.Necklace;
        }
    }

    public abstract class NecklaceObject : EquipmentObject
    {
    }
}
