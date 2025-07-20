namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class Inventory : RoomItem
    {
        public Inventory()
        {
            this.Width = 10;
            this.Height = 7;
            this.walls = new Dictionary<WallDirection, Wall>();
            this.walls.Add(WallDirection.TOP, new InventoryWallT());
            this.walls.Add(WallDirection.DOWN, new InventoryWallD());
            this.walls.Add(WallDirection.LEFT, new InventoryWallL());
            this.walls.Add(WallDirection.RIGHT, new InventoryWallR());
            this.walls.Add(WallDirection.RIGHT_TOP, new InventoryWallRT());
            this.walls.Add(WallDirection.RIGHT_DOWN, new InventoryWallRD());
            this.walls.Add(WallDirection.LEFT_TOP, new InventoryWallLT());
            this.walls.Add(WallDirection.LEFT_DOWN, new InventoryWallLD());
        }

        public override void AddBuildTask(Vector3Int centerMap)
        {
            int[] xB = this.getXBoundary(centerMap);
            int[] yB = this.getYBoundary(centerMap);
            for (int i = 1; i < Width - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(xB[0], yB[0] + i, 0), this.walls[WallDirection.DOWN].Tile)
                    .DirectBuild(new Vector3Int(xB[1], yB[0] + i, 0), this.walls[WallDirection.TOP].Tile);
            }
            for (int i = 1; i < this.Height - 1; i++)
            {
                BuildMap.Instance.DirectBuild(new Vector3Int(xB[0] + i, yB[0], 0), this.walls[WallDirection.LEFT].Tile)
                    .DirectBuild(new Vector3Int(xB[0] + i, yB[1], 0), this.walls[WallDirection.RIGHT].Tile);
            }
            BuildMap.Instance
                .DirectBuild(new Vector3Int(xB[0], yB[1], 0), this.walls[WallDirection.RIGHT_DOWN].Tile)
                .DirectBuild(new Vector3Int(xB[0], yB[0], 0), this.walls[WallDirection.LEFT_DOWN].Tile)
                .DirectBuild(new Vector3Int(xB[1], yB[1], 0), this.walls[WallDirection.RIGHT_TOP].Tile)
                .DirectBuild(new Vector3Int(xB[1], yB[0], 0), this.walls[WallDirection.LEFT_TOP].Tile)
                .AddTask();
            // 添加仓库Cell
            InventoryManager.Instance.AddCells(Tool.Add(centerMap, -this.Height / 2, -this.Width / 2), this.Width, this.Height);
        }
    }
}
