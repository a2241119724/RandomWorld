namespace LAB2D.Item.Build.Furniture.Bed
{
    using LAB2D;
    using LAB2D.Core;
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
            this.RectType = AWorkerTask.RectType.BottomLeft;
        }

        /// <inheritdoc/>
        public override void AddBuildTask(Vector3Int centerMap, Extra extra)
        {
            base.AddBuildTask(centerMap, extra);

            // 添加
            ServiceLocator.Get<FurnitureManager>().AddBed(centerMap);
        }
    }
}
