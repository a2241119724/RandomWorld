using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomWood : Wood
    {
        public CustomWood()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomWood");
        }
    }
}
