using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomMount : Mount
    {
        public CustomMount()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomMount");
        }
    }

    public class CustomMountObject : MountObject
    {
    }
}
