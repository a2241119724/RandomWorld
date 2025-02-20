using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomShield : Shield
    {
        public CustomShield()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomShield");
        }
    }

    public class CustomShieldObject : ShieldObject
    {
    }
}
