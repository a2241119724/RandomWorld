using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Head : Equipment
    {
        public Head()
        {
            equipType = EquipType.Head;
        }
    }

    public abstract class HeadObject : EquipmentObject
    {
    }
}
