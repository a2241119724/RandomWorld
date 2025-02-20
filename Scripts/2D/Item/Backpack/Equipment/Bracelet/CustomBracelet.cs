using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomBracelet : Bracelet
    {
        public CustomBracelet()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomBracelet");
        }
    }

    public class CustomBraceletObject : BraceletObject
    {
    }
}

