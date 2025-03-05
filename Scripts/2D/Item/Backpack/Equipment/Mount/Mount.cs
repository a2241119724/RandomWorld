using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Mount : Equipment
    {
        public Mount()
        {
            equipType = EquipType.Mount;
        }
    }

    public abstract class MountObject : EquipmentObject
    {
    }
}
