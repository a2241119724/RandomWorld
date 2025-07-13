using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Body : Equipment
    {
        public Body()
        {
            equipType = EquipType.Body;
        }
    }

    public abstract class BodyObject : EquipmentObject
    {
    }
}
