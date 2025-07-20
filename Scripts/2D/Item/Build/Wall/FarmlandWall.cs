namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田墙
    /// </summary>
    [Serializable]
    public class FarmlandWall : Wall
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
