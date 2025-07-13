using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Trouser : Equipment
    {
        public Trouser()
        {
            equipType = EquipType.Trouser;
        }
    }

    public abstract class TrouserObject : EquipmentObject
    {
    }
}
