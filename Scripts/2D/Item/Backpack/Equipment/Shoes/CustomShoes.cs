using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomShoes : Shoes
    {
        public CustomShoes()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomShoes");
        }
    }

    public class CustomShoesObject : ShoesObject
    {
    }
}
