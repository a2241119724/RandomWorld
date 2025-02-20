using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomMount : Mount
    {
        public CustomMount()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomMount");
        }
    }

    public class CustomMountObject : MountObject
    {
    }
}
