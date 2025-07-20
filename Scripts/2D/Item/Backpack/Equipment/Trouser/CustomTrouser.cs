namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义裤子
    /// </summary>
    [Serializable]
    public class CustomTrouser : Trouser
    {
        public CustomTrouser()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomTrouser");
        }
    }

    /// <summary>
    /// 自定义裤子对象
    /// </summary>
    public class CustomTrouserObject : TrouserObject
    {
    }
}
