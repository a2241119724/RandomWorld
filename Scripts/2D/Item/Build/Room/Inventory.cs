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

        public override void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            for (int i = 1; i < width - 1; i++)
            {
                BuildMap.Instance.addPassBuild(new Vector3Int(centerMap.x - height / 2, centerMap.y - width / 2 + i, 0), walls[WallDirection.DOWN].tile)
                    .addPassBuild(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y - width / 2 + i, 0), walls[WallDirection.TOP].tile);
            }
            for (int i = 1; i < height - 1; i++)
            {
                BuildMap.Instance.addPassBuild(new Vector3Int(centerMap.x - height / 2 + i, centerMap.y - width / 2, 0), walls[WallDirection.LEFT].tile)
                    .addPassBuild(new Vector3Int(centerMap.x - height / 2 + i, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT].tile);
            }
            BuildMap.Instance
                .addPassBuild(new Vector3Int(centerMap.x - height / 2, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT_DOWN].tile)
                .addPassBuild(new Vector3Int(centerMap.x - height / 2, centerMap.y - width / 2, 0), walls[WallDirection.LEFT_DOWN].tile)
                .addPassBuild(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT_TOP].tile)
                .addPassBuild(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y - width / 2, 0), walls[WallDirection.LEFT_TOP].tile)
                .addTask();
            // Ìí¼Ó²Ö¿âCell
            InventoryManager.Instance.addCells(Tool.add(centerMap, -height / 2, -width / 2), width, height);
        }
    }

    [Serializable]
    public class InventoryWallT : Wall
    {
        public InventoryWallT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallT");
        }
    }

    [Serializable]
    public class InventoryWallD : Wall
    {
        public InventoryWallD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallD");
        }
    }

    [Serializable]
    public class InventoryWallL : Wall
    {
        public InventoryWallL()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallL");
        }
    }

    [Serializable]
    public class InventoryWallR : Wall
    {
        public InventoryWallR()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallR");
        }
    }

    [Serializable]
    public class InventoryWallRT : Wall
    {
        public InventoryWallRT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallRT");
        }
    }

    [Serializable]
    public class InventoryWallRD : Wall
    {
        public InventoryWallRD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallRD");
        }
    }

    [Serializable]
    public class InventoryWallLT : Wall
    {
        public InventoryWallLT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallLT");
        }
    }

    [Serializable]
    public class InventoryWallLD : Wall
    {
        public InventoryWallLD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("InventoryWallLD");
        }
    }
}
