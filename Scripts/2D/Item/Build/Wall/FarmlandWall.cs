namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田,地块
    /// </summary>
    [Serializable]
    public class FarmlandWall : AWall
    {
        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            BuildMap.Instance.AddBuild(centerMap, this.TileName);
        }
    }
}
