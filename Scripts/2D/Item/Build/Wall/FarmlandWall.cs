namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田,地块
    /// </summary>
    [Serializable]
    public class FarmlandWall : WallItem
    {
        public FarmlandWall()
        {
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            BuildMap.Instance.DirectBuild(centerMap, this.Tile).AddTask();
        }
    }
}
