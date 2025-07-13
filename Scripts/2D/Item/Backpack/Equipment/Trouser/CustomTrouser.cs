using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomTrouser : Trouser
    {
        public CustomTrouser()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomTrouser");
        }
    }

    public class CustomTrouserObject : TrouserObject
    {
    }
}
