namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义翅膀
    /// </summary>
    [Serializable]
    public class CustomWing : Wing
    {
        public CustomWing()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomWing");
        }
    }

    /// <summary>
    /// 自定义翅膀对象
    /// </summary>
    public class CustomWingObject : WingObject
    {
    }
}
