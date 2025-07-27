namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 自定义房间
    /// </summary>
    [Serializable]
    public class CustomRoom : RoomItem
    {
        /// <summary>
        /// 门
        /// </summary>
        public DoorItem Door;

        public CustomRoom()
        {
            this.Width = 10;
            this.Height = 7;
            this.Walls = new Dictionary<WallDirection, Wall>
            {
                { WallDirection.TOP, new CustomRoomWallT() },
                { WallDirection.DOWN, new CustomRoomWallD() },
                { WallDirection.LEFT, new CustomRoomWallL() },
                { WallDirection.RIGHT, new CustomRoomWallR() },
                { WallDirection.RIGHT_TOP, new CustomRoomWallRT() },
                { WallDirection.RIGHT_DOWN, new CustomRoomWallRD() },
                { WallDirection.LEFT_TOP, new CustomRoomWallLT() },
                { WallDirection.LEFT_DOWN, new CustomRoomWallLD() },
            };
            this.Door = new CustomDoor();
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] x_B = this.GetXBoundary(centerMap);
            int[] y_B = this.GetYBoundary(centerMap);
            RoomInfo roomInfo = new ();
            for (int i = 1; i < this.Width - 1; i++)
            {
                BuildMap.Instance.AddBuilding(new Vector3Int(x_B[0], y_B[0] + i, 0), this.Walls[WallDirection.DOWN].Tile)
                    .AddBuilding(new Vector3Int(x_B[1], y_B[0] + i, 0), this.Walls[WallDirection.TOP].Tile);
                roomInfo.Points.Add(new Vector3Int(x_B[0], y_B[0] + i, 0));
                roomInfo.Points.Add(new Vector3Int(x_B[1], y_B[0] + i, 0));
            }

            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.AddBuilding(new Vector3Int(x_B[0] + i, y_B[0], 0), this.Walls[WallDirection.LEFT].Tile)
                    .AddBuilding(new Vector3Int(x_B[0] + i, y_B[1], 0), this.Walls[WallDirection.RIGHT].Tile);
                roomInfo.Points.Add(new Vector3Int(x_B[0] + i, y_B[0], 0));
                roomInfo.Points.Add(new Vector3Int(x_B[0] + i, y_B[1], 0));
            }

            BuildMap.Instance
                .AddBuilding(new Vector3Int(x_B[0], y_B[1], 0), this.Walls[WallDirection.RIGHT_DOWN].Tile)
                .AddBuilding(new Vector3Int(x_B[0], y_B[0], 0), this.Walls[WallDirection.LEFT_DOWN].Tile)
                .AddBuilding(new Vector3Int(x_B[1], y_B[1], 0), this.Walls[WallDirection.RIGHT_TOP].Tile)
                .AddBuilding(new Vector3Int(x_B[1], y_B[0], 0), this.Walls[WallDirection.LEFT_TOP].Tile)
                .AddBuilding(new Vector3Int(x_B[0], centerMap.y, 0), this.Door.Tile, false)
                .AddTask();
            roomInfo.Points.Add(new Vector3Int(x_B[0], y_B[1], 0));
            roomInfo.Points.Add(new Vector3Int(x_B[0], y_B[0], 0));
            roomInfo.Points.Add(new Vector3Int(x_B[1], y_B[1], 0));
            roomInfo.Points.Add(new Vector3Int(x_B[1], y_B[0], 0));
            roomInfo.Points.Add(new Vector3Int(x_B[0], centerMap.y, 0));

            // 由于多计算了一次墙,门覆盖了前面的墙
            roomInfo.Progress = roomInfo.Points.Count - 1;
            RoomManager.Instance.AddRoom(Guid.NewGuid().ToString(), roomInfo);
        }
    }

    /// <summary>
    /// 房间对象
    /// </summary>
    public class CustomRoomObject : BuildItemObject
    {
    }

    /// <summary>
    /// 自定义房间上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallT : Wall
    {
        public CustomRoomWallT()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallT");
        }
    }

    /// <summary>
    /// 自定义房间下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallD : Wall
    {
        public CustomRoomWallD()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallD");
        }
    }

    /// <summary>
    /// 自定义房间左墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallL : Wall
    {
        public CustomRoomWallL()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallL");
        }
    }

    /// <summary>
    /// 自定义房间右墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallR : Wall
    {
        public CustomRoomWallR()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallR");
        }
    }

    /// <summary>
    /// 自定义房间右上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallRT : Wall
    {
        public CustomRoomWallRT()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallRT");
        }
    }

    /// <summary>
    /// 自定义房间右下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallRD : Wall
    {
        public CustomRoomWallRD()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallRD");
        }
    }

    /// <summary>
    /// 自定义房间左上墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallLT : Wall
    {
        public CustomRoomWallLT()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallLT");
        }
    }

    /// <summary>
    /// 自定义房间左下墙
    /// </summary>
    [Serializable]
    public class CustomRoomWallLD : Wall
    {
        public CustomRoomWallLD()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("CustomRoomWallLD");
        }
    }

    /// <summary>
    /// 自定义房间墙对象
    /// </summary>
    public class CustomRoomWallObject : BuildItemObject
    {
    }
}
