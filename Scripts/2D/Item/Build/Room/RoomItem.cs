using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    [Serializable]
    public abstract class RoomItem : BuildItem
    {
        public Dictionary<WallDirection, Wall> walls;

        public override void addBuildTask(Vector3Int centerMap)
        {
            throw new System.NotImplementedException();
        }

        public int[] getXBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.x - height / 2, centerMap.x + height - 1 - height / 2 };
        }

        public int[] getYBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.y - width / 2, centerMap.y + width - 1 - width / 2 };
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
}
