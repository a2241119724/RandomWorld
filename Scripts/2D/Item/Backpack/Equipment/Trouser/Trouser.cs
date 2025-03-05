using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class Trouser : Equipment
    {
        public Trouser()
        {
            equipType = EquipType.Trouser;
        }
    }

    public abstract class TrouserObject : EquipmentObject
    {
    }
}
