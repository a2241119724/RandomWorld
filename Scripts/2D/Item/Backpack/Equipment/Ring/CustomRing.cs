using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomRing : Ring
    {
        public CustomRing()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRing");
        }
    }

    public class CustomRingObject : RingObject
    {
    }
}
