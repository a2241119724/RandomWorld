using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomBelt : Belt
    {
        public CustomBelt()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomBelt");
        }
    }

    public class CustomBeltObject : BeltObject
    {
    }
}
