namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义鞋子
    /// </summary>
    [Serializable]
    public class CustomShoes : Shoes
    {
        public CustomShoes()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomShoes");
        }
    }

    /// <summary>
    /// 自定义鞋子对象
    /// </summary>
    public class CustomShoesObject : ShoesObject
    {
    }
}
