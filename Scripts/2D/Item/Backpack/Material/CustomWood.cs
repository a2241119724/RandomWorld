namespace LAB2D.Item.Backpack.Material
{
    using LAB2D;
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
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomWood");
        }
    }
}
