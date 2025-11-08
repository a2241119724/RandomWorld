namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 床
    /// </summary>
    [Serializable]
    public abstract class ABed : AFurniture
    {
        public ABed()
        {
            this.IsBottomLeft = true;
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap)
        {
            base.AddBuildTask(centerMap);

            // 添加
            FurnitureManager.Instance.AddBed(centerMap);
        }
    }
}
