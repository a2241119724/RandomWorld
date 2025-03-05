using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
