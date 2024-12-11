using System;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public class Room : BuildItem
    {
        public Dictionary<WallDirection, Wall> walls;
        public Door door;

        public Room() {
            walls = new Dictionary<WallDirection, Wall>();
            walls.Add(WallDirection.TOP, new WallT());
            walls.Add(WallDirection.DOWN, new WallD());
            walls.Add(WallDirection.LEFT, new WallL());
            walls.Add(WallDirection.RIGHT, new WallR());
            walls.Add(WallDirection.RIGHT_TOP, new WallRT());
            walls.Add(WallDirection.RIGHT_DOWN, new WallRD());
            walls.Add(WallDirection.LEFT_TOP, new WallLT());
            walls.Add(WallDirection.LEFT_DOWN, new WallLD());
            door = new Door();
        }

        public void addRoomBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            for (int i = 1; i < width - 1; i++)
            {
                BuildMap.Instance.addBuilding(new Vector3Int(centerMap.x - height / 2, centerMap.y - width / 2 + i, 0), walls[WallDirection.DOWN].tile)
                    .addBuilding(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y - width / 2 + i, 0), walls[WallDirection.TOP].tile);
            }
            for (int i = 1; i < height - 1; i++)
            {
                BuildMap.Instance.addBuilding(new Vector3Int(centerMap.x - height / 2 + i, centerMap.y - width / 2, 0), walls[WallDirection.LEFT].tile)
                    .addBuilding(new Vector3Int(centerMap.x - height / 2 + i, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT].tile);
            }
            BuildMap.Instance
                .addBuilding(new Vector3Int(centerMap.x - height / 2, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT_DOWN].tile)
                .addBuilding(new Vector3Int(centerMap.x - height / 2, centerMap.y - width / 2, 0), walls[WallDirection.LEFT_DOWN].tile)
                .addBuilding(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y + width - 1 - width / 2, 0), walls[WallDirection.RIGHT_TOP].tile)
                .addBuilding(new Vector3Int(centerMap.x + height - 1 - height / 2, centerMap.y - width / 2, 0), walls[WallDirection.LEFT_TOP].tile)
                .addBuilding(new Vector3Int(centerMap.x - height / 2, centerMap.y, 0), door.tile,false)
                .addTask();
        }

        public enum WallDirection
        {
            TOP,
            DOWN,
            LEFT,
            RIGHT,
            RIGHT_TOP,
            RIGHT_DOWN,
            LEFT_TOP,
            LEFT_DOWN
        }
    }

    public class RoomObject : BuildItemObject
    {
    }
}
