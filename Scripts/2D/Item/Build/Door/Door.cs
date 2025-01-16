using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Door : BuildItem
    {
        [NonSerialized]
        public TileBase tile;

        public Door() { 
            tile = (TileBase)ResourcesManager.Instance.getAsset("Door");
        }

        public override void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            throw new NotImplementedException();
        }
    }
}
