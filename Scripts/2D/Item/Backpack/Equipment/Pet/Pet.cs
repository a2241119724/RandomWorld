using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
