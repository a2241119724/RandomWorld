using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomWing : Wing
    {
        public CustomWing()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomWing");
        }
    }

    public class CustomWingObject : WingObject
    {
    }
}
