using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Wing : Equipment
    {
        public Wing()
        {
            equipType = EquipType.Wing;
        }
    }

    public abstract class WingObject : EquipmentObject
    {
    }
}
