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
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomEarring");
        }
    }

    /// <summary>
    /// 自定义耳环对象
    /// </summary>
    public class CustomEarringObject : EarringObject
    {
    }
}
