using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class DoorItem : BuildItem
    {
        [NonSerialized]
        public TileBase tile;

        public override void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            throw new NotImplementedException();
        }
    }
}
