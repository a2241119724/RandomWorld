namespace LAB2D.Item.Build.Room
{
    using LAB2D;
    using LAB2D.Core;
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
            this.WallTiles = new Dictionary<WallDirectionEnum, string>
            {
                { WallDirectionEnum.TOP, "InventoryWallT" },
                { WallDirectionEnum.DOWN, "InventoryWallD" },
                { WallDirectionEnum.LEFT, "InventoryWallL" },
                { WallDirectionEnum.RIGHT, "InventoryWallR" },
                { WallDirectionEnum.RIGHT_TOP, "InventoryWallRT" },
                { WallDirectionEnum.RIGHT_DOWN, "InventoryWallRD" },
                { WallDirectionEnum.LEFT_TOP, "InventoryWallLT" },
                { WallDirectionEnum.LEFT_DOWN, "InventoryWallLD" },
            };
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

            for (int i = 1; i < width - 1; i++)
            {
                Core.ServiceLocator.Get<Map.BuildMap>().AddBuild(new Vector3Int(boundary[0], boundary[2] + i, 0), this.WallTiles[WallDirectionEnum.DOWN])
                    .AddBuild(new Vector3Int(boundary[1], boundary[2] + i, 0), this.WallTiles[WallDirectionEnum.TOP]);
            }

            for (int i = 1; i < height - 1; i++)
            {
                Core.ServiceLocator.Get<Map.BuildMap>().AddBuild(new Vector3Int(boundary[0] + i, boundary[2], 0), this.WallTiles[WallDirectionEnum.LEFT])
                    .AddBuild(new Vector3Int(boundary[0] + i, boundary[3], 0), this.WallTiles[WallDirectionEnum.RIGHT]);
            }

            // 四角
            Core.ServiceLocator.Get<Map.BuildMap>()
                .AddBuild(new Vector3Int(boundary[0], boundary[3], 0), this.WallTiles[WallDirectionEnum.RIGHT_DOWN])
                .AddBuild(new Vector3Int(boundary[0], boundary[2], 0), this.WallTiles[WallDirectionEnum.LEFT_DOWN])
                .AddBuild(new Vector3Int(boundary[1], boundary[3], 0), this.WallTiles[WallDirectionEnum.RIGHT_TOP])
                .AddBuild(new Vector3Int(boundary[1], boundary[2], 0), this.WallTiles[WallDirectionEnum.LEFT_TOP]);

            // 添加仓库Cell
            ServiceLocator.Get<InventoryManager>().AddCells(new Vector3Int(boundary[0], boundary[2]), width, height);
        }
    }
}
