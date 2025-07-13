using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomDoor : DoorItem
    {
        public CustomDoor()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomDoor");
        }
    }
}
