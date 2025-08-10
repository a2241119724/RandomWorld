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
            this.Walls = new Dictionary<WallItem.WallDirectionEnum, WallItem>
            {
                { WallItem.WallDirectionEnum.TOP, new InventoryWallT() },
                { WallItem.WallDirectionEnum.DOWN, new InventoryWallD() },
                { WallItem.WallDirectionEnum.LEFT, new InventoryWallL() },
                { WallItem.WallDirectionEnum.RIGHT, new InventoryWallR() },
                { WallItem.WallDirectionEnum.RIGHT_TOP, new InventoryWallRT() },
                { WallItem.WallDirectionEnum.RIGHT_DOWN, new InventoryWallRD() },
                { WallItem.WallDirectionEnum.LEFT_TOP, new InventoryWallLT() },
                { WallItem.WallDirectionEnum.LEFT_DOWN, new InventoryWallLD() },
            };
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] boundary = this.GetBoundary(centerMap);
            for (int i = 1; i < this.Width - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(boundary[0], boundary[2] + i, 0), this.Walls[WallItem.WallDirectionEnum.DOWN].Tile)
                    .DirectBuild(new Vector3Int(boundary[1], boundary[2] + i, 0), this.Walls[WallItem.WallDirectionEnum.TOP].Tile);
            }

            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(boundary[0] + i, boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT].Tile)
                    .DirectBuild(new Vector3Int(boundary[0] + i, boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT].Tile);
            }

            BuildMap.Instance
                .DirectBuild(new Vector3Int(boundary[0], boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT_DOWN].Tile)
                .DirectBuild(new Vector3Int(boundary[0], boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT_DOWN].Tile)
                .DirectBuild(new Vector3Int(boundary[1], boundary[3], 0), this.Walls[WallItem.WallDirectionEnum.RIGHT_TOP].Tile)
                .DirectBuild(new Vector3Int(boundary[1], boundary[2], 0), this.Walls[WallItem.WallDirectionEnum.LEFT_TOP].Tile)
                .AddTask();

            // 添加仓库Cell
            InventoryManager.Instance.AddCells(VectorTool.Add(centerMap, -this.Height / 2, -this.Width / 2), this.Width, this.Height);
        }
    }
}
