namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 墙
    /// </summary>
    [Serializable]
    public abstract class Wall : BuildItem
    {
        public Wall()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset(this.GetType().Name);
        }
    }

    /// <summary>
    /// 上墙
    /// </summary>
    [Serializable]
    public abstract class WallT : Wall
    {
    }

    /// <summary>
    /// 下墙
    /// </summary>
    [Serializable]
    public abstract class WallD : Wall
    {
    }

    /// <summary>
    /// 左墙
    /// </summary>
    [Serializable]
    public abstract class WallL : Wall
    {
    }

    /// <summary>
    /// 右墙
    /// </summary>
    [Serializable]
    public abstract class WallR : Wall
    {
    }

    /// <summary>
    /// 右上墙
    /// </summary>
    [Serializable]
    public abstract class WallRT : Wall
    {
    }

    /// <summary>
    /// 右下墙
    /// </summary>
    [Serializable]
    public abstract class WallRD : Wall
    {
    }

    /// <summary>
    /// 左上墙
    /// </summary>
    [Serializable]
    public abstract class WallLT : Wall
    {
    }

    /// <summary>
    /// 左下墙
    /// </summary>
    [Serializable]
    public abstract class WallLD : Wall
    {
    }

    /// <summary>
    /// 墙对象
    /// </summary>
    public class WallObject : BuildItemObject
    {
    }
}
