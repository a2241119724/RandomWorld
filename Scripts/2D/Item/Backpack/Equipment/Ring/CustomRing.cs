using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomRing : Ring
    {
        public CustomRing()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRing");
        }
    }

    public class CustomRingObject : RingObject
    {
    }
}
