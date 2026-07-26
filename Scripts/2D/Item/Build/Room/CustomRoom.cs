namespace LAB2D.Item.Build.Room
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 自定义房间
    /// </summary>
    [Serializable]
    public class CustomRoom : ARoom
    {
        public CustomRoom()
        {
            this.Width = 10;
            this.Height = 7;
            this.Walls = new Dictionary<AWall.WallDirectionEnum, AWall>
            {
                { AWall.WallDirectionEnum.TOP, new CustomRoomWallT() },
                { AWall.WallDirectionEnum.DOWN, new CustomRoomWallD() },
                { AWall.WallDirectionEnum.LEFT, new CustomRoomWallL() },
                { AWall.WallDirectionEnum.RIGHT, new CustomRoomWallR() },
                { AWall.WallDirectionEnum.RIGHT_TOP, new CustomRoomWallRT() },
                { AWall.WallDirectionEnum.RIGHT_DOWN, new CustomRoomWallRD() },
                { AWall.WallDirectionEnum.LEFT_TOP, new CustomRoomWallLT() },
                { AWall.WallDirectionEnum.LEFT_DOWN, new CustomRoomWallLD() },
            };
            this.Door = new CustomDoor();
        }
    }
}
