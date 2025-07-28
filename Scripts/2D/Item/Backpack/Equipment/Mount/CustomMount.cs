namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义坐骑
    /// </summary>
    [Serializable]
    public class CustomMount : Mount
    {
        public CustomMount()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomMount");
        }
    }

    /// <summary>
    /// 自定义坐骑对象
    /// </summary>
    public class CustomMountObject : MountObject
    {
    }
}
