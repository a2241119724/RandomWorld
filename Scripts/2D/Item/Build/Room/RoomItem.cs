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

        public override void addBuildTask(Vector3Int centerMap, int width = 10, int height = 7)
        {
            throw new System.NotImplementedException();
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
