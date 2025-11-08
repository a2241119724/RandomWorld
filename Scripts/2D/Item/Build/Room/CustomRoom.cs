namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 自定义房间
    /// </summary>
    [Serializable]
    public class CustomRoom : ARoom
    {
        /// <summary>
        /// 门
        /// </summary>
        public ADoor Door;

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

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] boundary = this.GetBoundary(centerMap);
            RoomInfo roomInfo = new ();
            for (int i = 1; i < this.Width - 1; i++)
            {
                BuildMap.Instance.AddBuild(new Vector3Int(boundary[0], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.DOWN].TileName)
                    .AddBuild(new Vector3Int(boundary[1], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.TOP].TileName);
                roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[2] + i, 0));
                roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[2] + i, 0));
            }

            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.AddBuild(new Vector3Int(boundary[0] + i, boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT].TileName)
                    .AddBuild(new Vector3Int(boundary[0] + i, boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT].TileName);
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[2], 0));
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[3], 0));
            }

            BuildMap.Instance
                .AddBuild(new Vector3Int(boundary[0], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[0], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_TOP].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_TOP].TileName)
                .AddBuild(new Vector3Int(boundary[0], centerMap.y, 0), this.Door.TileName);
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
}
