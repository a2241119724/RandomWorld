using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomEarring : Earring
    {
        public CustomEarring()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomEarring");
        }
    }

    public class CustomEarringObject : EarringObject
    {
    }
}
