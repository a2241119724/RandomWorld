using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Ring : Equipment
    {
        public Ring()
        {
            equipType = EquipType.Ring;
        }
    }

    public abstract class RingObject : EquipmentObject
    {
    }
}
