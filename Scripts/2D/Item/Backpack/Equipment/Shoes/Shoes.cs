using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Shoes : Equipment
    {
        public Shoes()
        {
            equipType = EquipType.Shoes;
        }
    }

    public abstract class ShoesObject : EquipmentObject
    {
    }
}
