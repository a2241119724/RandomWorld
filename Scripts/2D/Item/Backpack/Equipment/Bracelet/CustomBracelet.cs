namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义手链
    /// </summary>
    [Serializable]
    public class CustomBracelet : Bracelet
    {
        public CustomBracelet()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomBracelet");
        }
    }

    /// <summary>
    /// 自定义手链对象
    /// </summary>
    public class CustomBraceletObject : BraceletObject
    {
    }
}
