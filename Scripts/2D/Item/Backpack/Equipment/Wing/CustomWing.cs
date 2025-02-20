using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomWing : Wing
    {
        public CustomWing()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomWing");
        }
    }

    public class CustomWingObject : WingObject
    {
    }
}
