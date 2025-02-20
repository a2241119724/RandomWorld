using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class CustomNecklace : Necklace
    {
        public CustomNecklace()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomNecklace");
        }
    }

    public abstract class CustomNecklaceObject : NecklaceObject
    {
    }
}
