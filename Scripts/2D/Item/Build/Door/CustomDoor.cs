using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomDoor : DoorItem
    {
        public CustomDoor()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomDoor");
        }
    }
}
