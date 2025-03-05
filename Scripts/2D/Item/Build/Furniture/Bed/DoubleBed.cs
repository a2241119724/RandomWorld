using LAB2D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static LAB2D.RoomItem;

namespace LAB2D
{
    public class DoubleBed : BedItem
    {
        public DoubleBed()
        {
            width = 2;
            height = 2;
            tile = (TileBase)ResourcesManager.Instance.getAsset("DoubleBed");
        }
    }
}
