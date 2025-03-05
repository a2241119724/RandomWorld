using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomWood : Wood
    {
        public CustomWood()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomWood");
        }
    }
}
