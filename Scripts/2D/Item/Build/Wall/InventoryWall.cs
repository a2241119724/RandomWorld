namespace LAB2D.Item.Build.Wall
{
    using LAB2D;
    using System;

    /// <summary>
    /// 仓库上墙
    /// </summary>
    [Serializable]
    public abstract class InventoryWall : AWall, AItem.IDontShow
    {
    }

    /// <summary>
    /// 仓库上墙
    /// </summary>
    [Serializable]
    public class InventoryWallT : InventoryWall
    {
    }

    /// <summary>
    /// 仓库下墙
    /// </summary>
    [Serializable]
    public class InventoryWallD : InventoryWall
    {
    }

    /// <summary>
    /// 仓库左墙
    /// </summary>
    [Serializable]
    public class InventoryWallL : InventoryWall
    {
    }

    /// <summary>
    /// 仓库右墙
    /// </summary>
    [Serializable]
    public class InventoryWallR : InventoryWall
    {
    }

    /// <summary>
    /// 仓库右上墙
    /// </summary>
    [Serializable]
    public class InventoryWallRT : InventoryWall
    {
    }

    /// <summary>
    /// 仓库右下墙
    /// </summary>
    [Serializable]
    public class InventoryWallRD : InventoryWall
    {
    }

    /// <summary>
    /// 仓库左上墙
    /// </summary>
    [Serializable]
    public class InventoryWallLT : InventoryWall
    {
    }

    /// <summary>
    /// 仓库左下墙
    /// </summary>
    [Serializable]
    public class InventoryWallLD : InventoryWall
    {
    }
}
