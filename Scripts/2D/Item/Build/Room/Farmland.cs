using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class Farmland : RoomItem 
    {
        public Farmland()
        {
            walls = new Dictionary<WallDirection, Wall>();
            walls.Add(WallDirection.TOP, new FarmlandWallT());
            walls.Add(WallDirection.DOWN, new FarmlandWallD());
            walls.Add(WallDirection.LEFT, new FarmlandWallL());
            walls.Add(WallDirection.RIGHT, new FarmlandWallR());
            walls.Add(WallDirection.RIGHT_TOP, new FarmlandWallRT());
            walls.Add(WallDirection.RIGHT_DOWN, new FarmlandWallRD());
            walls.Add(WallDirection.LEFT_TOP, new FarmlandWallLT());
            walls.Add(WallDirection.LEFT_DOWN, new FarmlandWallLD());
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
            FarmlandManager.Instance.addCells(Tool.add(centerMap, -height / 2, -width / 2), width, height);
        }
    }

    [Serializable]
    public class FarmlandWallT : Wall
    {
        public FarmlandWallT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallT");
        }
    }

    [Serializable]
    public class FarmlandWallD : Wall
    {
        public FarmlandWallD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallD");
        }
    }

    [Serializable]
    public class FarmlandWallL : Wall
    {
        public FarmlandWallL()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallL");
        }
    }

    [Serializable]
    public class FarmlandWallR : Wall
    {
        public FarmlandWallR()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallR");
        }
    }

    [Serializable]
    public class FarmlandWallRT : Wall
    {
        public FarmlandWallRT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallRT");
        }
    }

    [Serializable]
    public class FarmlandWallRD : Wall
    {
        public FarmlandWallRD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallRD");
        }
    }

    [Serializable]
    public class FarmlandWallLT : Wall
    {
        public FarmlandWallLT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallLT");
        }
    }

    [Serializable]
    public class FarmlandWallLD : Wall
    {
        public FarmlandWallLD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("FarmlandWallLD");
        }
    }
}
