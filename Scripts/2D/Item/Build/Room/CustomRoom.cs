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
            this.WallTiles = new Dictionary<WallDirectionEnum, string>
            {
                { WallDirectionEnum.TOP, "CustomRoomWallT" },
                { WallDirectionEnum.DOWN, "CustomRoomWallD" },
                { WallDirectionEnum.LEFT, "CustomRoomWallL" },
                { WallDirectionEnum.RIGHT, "CustomRoomWallR" },
                { WallDirectionEnum.RIGHT_TOP, "CustomRoomWallRT" },
                { WallDirectionEnum.RIGHT_DOWN, "CustomRoomWallRD" },
                { WallDirectionEnum.LEFT_TOP, "CustomRoomWallLT" },
                { WallDirectionEnum.LEFT_DOWN, "CustomRoomWallLD" },
            };
            this.DoorTile = "CustomDoor";
        }
    }
}
