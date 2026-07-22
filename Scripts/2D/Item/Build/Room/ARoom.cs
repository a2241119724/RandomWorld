namespace LAB2D.Item.Build.Room
{
    using LAB2D;
    using LAB2D.Item.Build;
    using LAB2D.Item.Build.Door;
    using LAB2D.Item.Build.Wall;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 房间数据
    /// </summary>
    [Serializable]
    public abstract class ARoom : ABuildItem
    {
        /// <summary>
        /// 门
        /// </summary>
        public ADoor Door;

        /// <summary>
        /// 房间的所有墙
        /// </summary>
        public Dictionary<AWall.WallDirectionEnum, AWall> Walls;

        protected ARoom()
        {
            this.IsCustomSize = true;
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            int[] boundary = this.GetBoundary(centerMap, extra);
            int width = boundary[3] - boundary[2] + 1;
            int height = boundary[1] - boundary[0] + 1;
            if (!this.CheckBoundary(boundary))
            {
                return;
            }

            RoomInfo roomInfo = new ();
            for (int i = 1; i < width - 1; i++)
            {
                Core.ServiceLocator.Get<BuildMap>().AddBuild(new Vector3Int(boundary[0], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.DOWN].TileName)
                    .AddBuild(new Vector3Int(boundary[1], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.TOP].TileName);
                roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[2] + i, 0));
                roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[2] + i, 0));
            }

            for (int i = 1; i < height - 1; i++)
            {
                Core.ServiceLocator.Get<BuildMap>().AddBuild(new Vector3Int(boundary[0] + i, boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT].TileName)
                    .AddBuild(new Vector3Int(boundary[0] + i, boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT].TileName);
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[2], 0));
                roomInfo.Points.Add(new Vector3Int(boundary[0] + i, boundary[3], 0));
            }

            // 四角加门
            Core.ServiceLocator.Get<BuildMap>()
                .AddBuild(new Vector3Int(boundary[0], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[0], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_TOP].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_TOP].TileName)
                .AddBuild(new Vector3Int(boundary[0], boundary[2] + ((boundary[3] - boundary[2]) / 2), 0), this.Door.TileName);
            roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[3], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[0], boundary[2], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[3], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[1], boundary[2], 0));
            roomInfo.Points.Add(new Vector3Int(boundary[0], centerMap.y, 0));

            // 由于多计算了一次墙,门覆盖了前面的墙
            roomInfo.Progress = roomInfo.Points.Count - 1;
            RoomManager.Instance.AddRoom(Guid.NewGuid().ToString(), roomInfo);
        }

        /// <summary>
        /// 获得XY轴边界
        /// </summary>
        /// <param name="centerMap">中心位置</param>
        /// <param name="extra">额外信息</param>
        /// <returns>坐标</returns>
        public int[] GetBoundary(Vector3Int centerMap, Extra extra)
        {
            int h_start = centerMap.x - (this.Height / 2), h_end = centerMap.x + this.Height - 1 - (this.Height / 2);
            int w_start = centerMap.y - (this.Width / 2), w_end = centerMap.y + this.Width - 1 - (this.Width / 2);
            if (extra != null && extra.RectType == AWorkerTask.RectType.TopLeft)
            {
                h_start = centerMap.x - extra.Height + 1;
                h_end = centerMap.x;
                w_start = centerMap.y;
                w_end = centerMap.y + extra.Width - 1;
            }

            return new int[]
            {
                h_start, h_end,
                w_start, w_end,
            };
        }

        public bool CheckBoundary(int[] boundary)
        {
            if (boundary[0] < 0 || boundary[1] >= TileMap.Instance.TileMapDataLAB.Width || boundary[2] < 0 || boundary[3] >= TileMap.Instance.TileMapDataLAB.Height
                || boundary[1] - boundary[0] <= 0 || boundary[3] - boundary[2] <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
