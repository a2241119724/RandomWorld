namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 仓库
    /// </summary>
    [Serializable]
    public class Inventory : ARoom
    {
        public Inventory()
        {
            this.Width = 10;
            this.Height = 7;
            this.Walls = new Dictionary<AWall.WallDirectionEnum, AWall>
            {
                { AWall.WallDirectionEnum.TOP, new InventoryWallT() },
                { AWall.WallDirectionEnum.DOWN, new InventoryWallD() },
                { AWall.WallDirectionEnum.LEFT, new InventoryWallL() },
                { AWall.WallDirectionEnum.RIGHT, new InventoryWallR() },
                { AWall.WallDirectionEnum.RIGHT_TOP, new InventoryWallRT() },
                { AWall.WallDirectionEnum.RIGHT_DOWN, new InventoryWallRD() },
                { AWall.WallDirectionEnum.LEFT_TOP, new InventoryWallLT() },
                { AWall.WallDirectionEnum.LEFT_DOWN, new InventoryWallLD() },
            };
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            int[] boundary = this.GetBoundary(centerMap, extra);
            if (!this.CheckBoundary(boundary))
            {
                return;
            }

            for (int i = 1; i < boundary[3] - boundary[2]; i++)
            {
                BuildMap.Instance.AddBuild(new Vector3Int(boundary[0], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.DOWN].TileName)
                    .AddBuild(new Vector3Int(boundary[1], boundary[2] + i, 0), this.Walls[AWall.WallDirectionEnum.TOP].TileName);
            }

            for (int i = 1; i < boundary[1] - boundary[0]; i++)
            {
                BuildMap.Instance.AddBuild(new Vector3Int(boundary[0] + i, boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT].TileName)
                    .AddBuild(new Vector3Int(boundary[0] + i, boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT].TileName);
            }

            BuildMap.Instance
                .AddBuild(new Vector3Int(boundary[0], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[0], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_DOWN].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[3], 0), this.Walls[AWall.WallDirectionEnum.RIGHT_TOP].TileName)
                .AddBuild(new Vector3Int(boundary[1], boundary[2], 0), this.Walls[AWall.WallDirectionEnum.LEFT_TOP].TileName);

            // 添加仓库Cell
            InventoryManager.Instance.AddCells(new Vector3Int(boundary[0], boundary[2]), boundary[3] - boundary[2], boundary[1] - boundary[0]);
        }
    }
}
