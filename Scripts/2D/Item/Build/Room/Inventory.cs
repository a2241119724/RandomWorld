namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 仓库
    /// </summary>
    [Serializable]
    public class Inventory : RoomItem
    {
        public Inventory()
        {
            this.Width = 10;
            this.Height = 7;
            this.Walls = new Dictionary<WallDirection, Wall>
            {
                { WallDirection.TOP, new InventoryWallT() },
                { WallDirection.DOWN, new InventoryWallD() },
                { WallDirection.LEFT, new InventoryWallL() },
                { WallDirection.RIGHT, new InventoryWallR() },
                { WallDirection.RIGHT_TOP, new InventoryWallRT() },
                { WallDirection.RIGHT_DOWN, new InventoryWallRD() },
                { WallDirection.LEFT_TOP, new InventoryWallLT() },
                { WallDirection.LEFT_DOWN, new InventoryWallLD() },
            };
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] x_B = this.GetXBoundary(centerMap);
            int[] y_B = this.GetYBoundary(centerMap);
            for (int i = 1; i < this.Width - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(x_B[0], y_B[0] + i, 0), this.Walls[WallDirection.DOWN].Tile)
                    .DirectBuild(new Vector3Int(x_B[1], y_B[0] + i, 0), this.Walls[WallDirection.TOP].Tile);
            }

            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(x_B[0] + i, y_B[0], 0), this.Walls[WallDirection.LEFT].Tile)
                    .DirectBuild(new Vector3Int(x_B[0] + i, y_B[1], 0), this.Walls[WallDirection.RIGHT].Tile);
            }

            BuildMap.Instance
                .DirectBuild(new Vector3Int(x_B[0], y_B[1], 0), this.Walls[WallDirection.RIGHT_DOWN].Tile)
                .DirectBuild(new Vector3Int(x_B[0], y_B[0], 0), this.Walls[WallDirection.LEFT_DOWN].Tile)
                .DirectBuild(new Vector3Int(x_B[1], y_B[1], 0), this.Walls[WallDirection.RIGHT_TOP].Tile)
                .DirectBuild(new Vector3Int(x_B[1], y_B[0], 0), this.Walls[WallDirection.LEFT_TOP].Tile)
                .AddTask();

            // 添加仓库Cell
            InventoryManager.Instance.AddCells(Tool.Add(centerMap, -this.Height / 2, -this.Width / 2), this.Width, this.Height);
        }
    }
}
