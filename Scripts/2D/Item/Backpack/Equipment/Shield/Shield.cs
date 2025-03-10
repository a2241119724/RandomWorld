using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Shield : Equipment
    {
        public Shield()
        {
            equipType = EquipType.Shield;
        }
    }

    public abstract class ShieldObject : EquipmentObject
    {
    }
}
