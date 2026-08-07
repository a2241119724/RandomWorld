namespace LAB2D.Item.Backpack.Material
{
    using LAB2D;
    using LAB2D.Core;
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义石头
    /// </summary>
    [Serializable]
    public class CustomStone : MaterialItem
    {
        public CustomStone()
        {
            this.Tile = (TileBase)ServiceLocator.Get<ResourceManager>().GetAsset("CustomStone");
        }
    }
}
