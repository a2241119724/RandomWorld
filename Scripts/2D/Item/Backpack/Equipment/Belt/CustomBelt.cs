using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomBelt : Belt
    {
        public CustomBelt()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomBelt");
        }
    }

    public class CustomBeltObject : BeltObject
    {
    }
}
