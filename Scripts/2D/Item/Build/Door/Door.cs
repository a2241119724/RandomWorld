using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class Door : BuildItem
    {
        public TileBase tile;

        public Door() { 
            tile = (TileBase)ResourcesManager.Instance.getAsset("Door");
        }
    }
}
