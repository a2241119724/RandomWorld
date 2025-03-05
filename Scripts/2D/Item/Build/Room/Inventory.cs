using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Inventory : RoomItem 
    {
        public Inventory()
        {
            width = 10;
            height = 7;
            walls = new Dictionary<WallDirection, Wall>();
            walls.Add(WallDirection.TOP, new InventoryWallT());
            walls.Add(WallDirection.DOWN, new InventoryWallD());
            walls.Add(WallDirection.LEFT, new InventoryWallL());
            walls.Add(WallDirection.RIGHT, new InventoryWallR());
            walls.Add(WallDirection.RIGHT_TOP, new InventoryWallRT());
            walls.Add(WallDirection.RIGHT_DOWN, new InventoryWallRD());
            walls.Add(WallDirection.LEFT_TOP, new InventoryWallLT());
            walls.Add(WallDirection.LEFT_DOWN, new InventoryWallLD());
        }

        public override void addBuildTask(Vector3Int centerMap)
        {
            int[] xB = getXBoundary(centerMap);
            int[] yB = getYBoundary(centerMap);
            for (int i = 1; i < width - 1; i++)
            {
                BuildMap.Instance.directBuild(new Vector3Int(xB[0], yB[0] + i, 0), walls[WallDirection.DOWN].tile)
                    .directBuild(new Vector3Int(xB[1], yB[0] + i, 0), walls[WallDirection.TOP].tile);
            }
            for (int i = 1; i < height - 1; i++)
            {
                BuildMap.Instance.directBuild(new Vector3Int(xB[0] + i, yB[0], 0), walls[WallDirection.LEFT].tile)
                    .directBuild(new Vector3Int(xB[0] + i, yB[1], 0), walls[WallDirection.RIGHT].tile);
            }
            BuildMap.Instance
                .directBuild(new Vector3Int(xB[0], yB[1], 0), walls[WallDirection.RIGHT_DOWN].tile)
                .directBuild(new Vector3Int(xB[0], yB[0], 0), walls[WallDirection.LEFT_DOWN].tile)
                .directBuild(new Vector3Int(xB[1], yB[1], 0), walls[WallDirection.RIGHT_TOP].tile)
                .directBuild(new Vector3Int(xB[1], yB[0], 0), walls[WallDirection.LEFT_TOP].tile)
                .addTask();
            // Ìí¼Ó²Ö¿âCell
            InventoryManager.Instance.addCells(Tool.add(centerMap, -height / 2, -width / 2), width, height);
        }
    }
}
