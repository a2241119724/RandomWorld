using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomHead : Head
    {
        public CustomHead()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomHead");
        }
    }

    public class CustomHeadObject : HeadObject
    {
    }
}
