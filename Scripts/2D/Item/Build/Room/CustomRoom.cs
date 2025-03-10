using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    [Serializable]
    public class CustomRoom : RoomItem
    {
        public DoorItem door;

        public CustomRoom() {
            width = 10;
            height = 7;
            walls = new Dictionary<WallDirection, Wall>();
            walls.Add(WallDirection.TOP, new CustomRoomWallT());
            walls.Add(WallDirection.DOWN, new CustomRoomWallD());
            walls.Add(WallDirection.LEFT, new CustomRoomWallL());
            walls.Add(WallDirection.RIGHT, new CustomRoomWallR());
            walls.Add(WallDirection.RIGHT_TOP, new CustomRoomWallRT());
            walls.Add(WallDirection.RIGHT_DOWN, new CustomRoomWallRD());
            walls.Add(WallDirection.LEFT_TOP, new CustomRoomWallLT());
            walls.Add(WallDirection.LEFT_DOWN, new CustomRoomWallLD());
            door = new CustomDoor();
        }

        public override void addBuildTask(Vector3Int centerMap)
        {
            int[] xB = getXBoundary(centerMap);
            int[] yB = getYBoundary(centerMap);
            RoomInfo roomInfo = new RoomInfo();
            for (int i = 1; i < width - 1; i++)
            {
                BuildMap.Instance.addBuilding(new Vector3Int(xB[0], yB[0] + i, 0), walls[WallDirection.DOWN].tile)
                    .addBuilding(new Vector3Int(xB[1], yB[0] + i, 0), walls[WallDirection.TOP].tile);
                roomInfo.Points.Add(new Vector3Int(xB[0], yB[0] + i, 0));
                roomInfo.Points.Add(new Vector3Int(xB[1], yB[0] + i, 0));
            }
            for (int i = 1; i < height - 1; i++)
            {
                BuildMap.Instance.addBuilding(new Vector3Int(xB[0] + i, yB[0], 0), walls[WallDirection.LEFT].tile)
                    .addBuilding(new Vector3Int(xB[0] + i, yB[1], 0), walls[WallDirection.RIGHT].tile);
                roomInfo.Points.Add(new Vector3Int(xB[0] + i, yB[0], 0));
                roomInfo.Points.Add(new Vector3Int(xB[0] + i, yB[1], 0));
            }
            BuildMap.Instance
                .addBuilding(new Vector3Int(xB[0], yB[1], 0), walls[WallDirection.RIGHT_DOWN].tile)
                .addBuilding(new Vector3Int(xB[0], yB[0], 0), walls[WallDirection.LEFT_DOWN].tile)
                .addBuilding(new Vector3Int(xB[1], yB[1], 0), walls[WallDirection.RIGHT_TOP].tile)
                .addBuilding(new Vector3Int(xB[1], yB[0], 0), walls[WallDirection.LEFT_TOP].tile)
                .addBuilding(new Vector3Int(xB[0], centerMap.y, 0), door.tile,false)
                .addTask();
            roomInfo.Points.Add(new Vector3Int(xB[0], yB[1], 0));
            roomInfo.Points.Add(new Vector3Int(xB[0], yB[0], 0));
            roomInfo.Points.Add(new Vector3Int(xB[1], yB[1], 0));
            roomInfo.Points.Add(new Vector3Int(xB[1], yB[0], 0));
            roomInfo.Points.Add(new Vector3Int(xB[0], centerMap.y, 0));
            // 由于多计算了一次墙,门覆盖了前面的墙
            roomInfo.Progress = roomInfo.Points.Count - 1;
            RoomManager.Instance.addRoom(Guid.NewGuid().ToString(), roomInfo);
        }
    }

    public class RoomObject : BuildItemObject
    {
    }

    [Serializable]
    public class CustomRoomWallT : Wall
    {
        public CustomRoomWallT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallT");
        }
    }

    [Serializable]
    public class CustomRoomWallD : Wall
    {
        public CustomRoomWallD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallD");
        }
    }

    [Serializable]
    public class CustomRoomWallL : Wall
    {
        public CustomRoomWallL()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallL");
        }
    }

    [Serializable]
    public class CustomRoomWallR : Wall
    {
        public CustomRoomWallR()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallR");
        }
    }

    [Serializable]
    public class CustomRoomWallRT : Wall
    {
        public CustomRoomWallRT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallRT");
        }
    }

    [Serializable]
    public class CustomRoomWallRD : Wall
    {
        public CustomRoomWallRD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallRD");
        }
    }

    [Serializable]
    public class CustomRoomWallLT : Wall
    {
        public CustomRoomWallLT()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallLT");
        }
    }

    [Serializable]
    public class CustomRoomWallLD : Wall
    {
        public CustomRoomWallLD()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomRoomWallLD");
        }
    }

    public class CustomRoomWallObject : BuildItemObject
    {
    }
}
