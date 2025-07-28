namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义帽子
    /// </summary>
    [Serializable]
    public class CustomHead : Head
    {
        public CustomHead()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomHead");
        }
    }

    /// <summary>
    /// 自定义帽子对象
    /// </summary>
    public class CustomHeadObject : HeadObject
    {
    }
}
