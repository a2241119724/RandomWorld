namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

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
            this.Walls = new Dictionary<WallItem.WallDirectionEnum, WallItem>
            {
                { WallItem.WallDirectionEnum.TOP, new CustomRoomWallT() },
                { WallItem.WallDirectionEnum.DOWN, new CustomRoomWallD() },
                { WallItem.WallDirectionEnum.LEFT, new CustomRoomWallL() },
                { WallItem.WallDirectionEnum.RIGHT, new CustomRoomWallR() },
                { WallItem.WallDirectionEnum.RIGHT_TOP, new CustomRoomWallRT() },
                { WallItem.WallDirectionEnum.RIGHT_DOWN, new CustomRoomWallRD() },
                { WallItem.WallDirectionEnum.LEFT_TOP, new CustomRoomWallLT() },
                { WallItem.WallDirectionEnum.LEFT_DOWN, new CustomRoomWallLD() },
            };
            this.Door = new CustomDoor();
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] boundary = this.GetBoundary(centerMap);
            RoomInfo roomInfo = new ();
            for (int i = 1; i < this.Width - 1; i++)
            {
                BuildMap.Instance.AddBuilding(new Vector3Int(boundary[0], boundary[2] + i, 0), this.Walls[WallItem.WallDirectionEnum.DOWN].Tile)
                    .AddBuilding(new Vector3Int(boundary[1], boundary[2] + i, 0), this.Walls[WallItem.WallDirectionEnum.TOP].Tile);
                roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[2] + i, 0));
                roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[2] + i, 0));
            }

            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.AddBuilding(new Vector3Int(boundary[0] + i, boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT].Tile)
                    .AddBuilding(new Vector3Int(boundary[0] + i, boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT].Tile);
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[2], 0));
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[3], 0));
            }

            BuildMap.Instance
                .AddBuilding(new Vector3Int(boundary[0], boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT_DOWN].Tile)
                .AddBuilding(new Vector3Int(boundary[0], boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT_DOWN].Tile)
                .AddBuilding(new Vector3Int(boundary[1], boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT_TOP].Tile)
                .AddBuilding(new Vector3Int(boundary[1], boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT_TOP].Tile)
                .AddBuilding(new Vector3Int(boundary[0], centerMap.y, 0), this.Door.Tile, true)
                .AddTask();
            roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[3], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[2], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[3], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[2], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[0], centerMap.y, 0));

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
}
