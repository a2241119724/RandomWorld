namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 床
    /// </summary>
    public abstract class BedItem : FurnitureItem
    {
        public BedItem()
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
