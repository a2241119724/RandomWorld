namespace LAB2D.Item.Backpack.Material
{
    using LAB2D;
    using LAB2D.Core;
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义木头
    /// </summary>
    [Serializable]
    public class CustomWood : WoodItem
    {
        public CustomWood()
        {
            this.Tile = (TileBase)ServiceLocator.Get<ResourceManager>().GetAsset("CustomWood");
        }
    }
}
