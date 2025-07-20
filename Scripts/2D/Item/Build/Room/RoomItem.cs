namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public abstract class RoomItem : BuildItem
    {
        public Dictionary<WallDirection, Wall> walls;

        public override void AddBuildTask(Vector3Int centerMap)
        {
            throw new System.NotImplementedException();
        }

        public int[] getXBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.x - this.Height / 2, centerMap.x + this.Height - 1 - this.Height / 2 };
        }

        public int[] getYBoundary(Vector3Int centerMap)
        {
            return new int[] { centerMap.y - (this.Width / 2), centerMap.y + this.Width - 1 - (this.Width / 2) };
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
