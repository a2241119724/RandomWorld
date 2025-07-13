using System;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomShoes : Shoes
    {
        public CustomShoes()
        {
            tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomShoes");
        }
    }

    public class CustomShoesObject : ShoesObject
    {
    }
}
