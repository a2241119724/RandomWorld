namespace LAB2D
{
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
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomWood");
        }
    }
}
