using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
