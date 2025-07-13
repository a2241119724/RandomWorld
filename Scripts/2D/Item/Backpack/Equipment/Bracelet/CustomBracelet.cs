using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomBracelet : Bracelet
    {
        public CustomBracelet()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomBracelet");
        }
    }

    public class CustomBraceletObject : BraceletObject
    {
    }
}

