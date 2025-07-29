namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义耳环
    /// </summary>
    [Serializable]
    public class CustomEarring : Earring
    {
        public CustomEarring()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("CustomEarring");
        }
    }

    /// <summary>
    /// 自定义耳环对象
    /// </summary>
    public class CustomEarringObject : EarringObject
    {
    }
}
