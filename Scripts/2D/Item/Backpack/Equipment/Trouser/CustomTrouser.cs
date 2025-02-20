using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomTrouser : Trouser
    {
        public CustomTrouser()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomTrouser");
        }
    }

    public class CustomTrouserObject : TrouserObject
    {
    }
}
