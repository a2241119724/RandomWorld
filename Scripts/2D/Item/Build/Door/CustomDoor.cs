using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class CustomDoor : DoorItem
    {
        public CustomDoor()
        {
            tile = (TileBase)ResourcesManager.Instance.getAsset("CustomDoor");
        }

        public override void addBuildTask(Vector3Int centerMap, int width = 1, int height = 1)
        {
            BuildMap.Instance.addPassBuild(centerMap, tile).addTask();
        }
    }
}
