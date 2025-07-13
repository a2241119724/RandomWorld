using System;

namespace LAB2D
{
    [Serializable]
    public abstract class Pet : Equipment
    {
        public Pet()
        {
            equipType = EquipType.Pet;
        }
    }

    public abstract class PetObject : EquipmentObject
    {
    }
}
