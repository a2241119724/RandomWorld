using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class SingleBed : BedItem
    {
        public SingleBed()
        {
            width = 1;
            height = 2;
            tile = (TileBase)ResourcesManager.Instance.getAsset("SingleBed");
        }
    }
}
