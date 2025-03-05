using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Body : Equipment
    {
        public Body()
        {
            equipType = EquipType.Body;
        }
    }

    public abstract class BodyObject : EquipmentObject
    {
    }
}
