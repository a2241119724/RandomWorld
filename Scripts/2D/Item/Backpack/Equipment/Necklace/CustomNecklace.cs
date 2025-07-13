using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public abstract class CustomNecklace : Necklace
    {
        public CustomNecklace()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomNecklace");
        }
    }

    public abstract class CustomNecklaceObject : NecklaceObject
    {
    }
}
