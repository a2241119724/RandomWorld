using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

