using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomEarring : Earring
    {
        public CustomEarring()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomEarring");
        }
    }

    public class CustomEarringObject : EarringObject
    {
    }
}
