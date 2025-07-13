using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomShield : Shield
    {
        public CustomShield()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomShield");
        }
    }

    public class CustomShieldObject : ShieldObject
    {
    }
}
