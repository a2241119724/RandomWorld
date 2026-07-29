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
                { WallDirectionEnum.LEFT_TOP,    "CustomRoomWall_0" },
                { WallDirectionEnum.TOP,         "CustomRoomWall_1" },
                { WallDirectionEnum.RIGHT_TOP,   "CustomRoomWall_2" },
                { WallDirectionEnum.LEFT,        "CustomRoomWall_3" },
                { WallDirectionEnum.RIGHT,       "CustomRoomWall_4" },
                { WallDirectionEnum.LEFT_DOWN,   "CustomRoomWall_5" },
                { WallDirectionEnum.DOWN,        "CustomRoomWall_6" },
                { WallDirectionEnum.RIGHT_DOWN,  "CustomRoomWall_7" },
            };
            this.DoorTile = "CustomDoor";
        }
    }
}
