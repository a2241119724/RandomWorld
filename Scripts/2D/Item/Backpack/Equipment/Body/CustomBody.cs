namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义上衣
    /// </summary>
    [Serializable]
    public class CustomBody : ABody
    {
        public CustomBody()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomBelt");
        }
    }
}
