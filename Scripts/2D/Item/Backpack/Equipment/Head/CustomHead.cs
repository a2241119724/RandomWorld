using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomHead : Head
    {
        public CustomHead()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomHead");
        }
    }

    public class CustomHeadObject : HeadObject
    {
    }
}
