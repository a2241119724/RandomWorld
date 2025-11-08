namespace LAB2D
{
    using System;

    /// <summary>
    /// 自定义房间墙
    /// </summary>
    [Serializable]
    public abstract class CustomRoomWall : AWall
    {
    }

    /// <summary>
    /// 自定义房间上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallT : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallD : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间左墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallL : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间右墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallR : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间右上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallRT : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间右下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallRD : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间左上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallLT : CustomRoomWall
    {
    }

    /// <summary>
    /// 自定义房间左下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallLD : CustomRoomWall
    {
    }
}
