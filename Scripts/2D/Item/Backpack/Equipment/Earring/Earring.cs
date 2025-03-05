using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Earring : Equipment
    {
        public Earring()
        {
            equipType = EquipType.Earring;
        }
    }

    public abstract class EarringObject : EquipmentObject
    {
    }
}
