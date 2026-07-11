namespace LAB2D.Item.Build.Wall
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 农田,地块
    /// </summary>
    [Serializable]
    public class FarmlandWall : AWall
    {
        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            BuildMap.Instance.AddBuild(centerMap, this.TileName);
        }
    }
}
