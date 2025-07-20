namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义戒指
    /// </summary>
    [Serializable]
    public class CustomRing : Ring
    {
        public CustomRing()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRing");
        }
    }

    /// <summary>
    /// 自定义戒指对象
    /// </summary>
    public class CustomRingObject : RingObject
    {
    }
}
