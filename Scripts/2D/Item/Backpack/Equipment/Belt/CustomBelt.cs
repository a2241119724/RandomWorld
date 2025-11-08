namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义腰带
    /// </summary>
    [Serializable]
    public class CustomBelt : Belt
    {
        public CustomBelt()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomBelt");
        }
    }
}
