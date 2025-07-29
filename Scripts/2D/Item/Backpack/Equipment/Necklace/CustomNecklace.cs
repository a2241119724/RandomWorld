namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义项链
    /// </summary>
    [Serializable]
    public abstract class CustomNecklace : Necklace
    {
        public CustomNecklace()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomNecklace");
        }
    }

    /// <summary>
    /// 自定义项链对象
    /// </summary>
    public abstract class CustomNecklaceObject : NecklaceObject
    {
    }
}
