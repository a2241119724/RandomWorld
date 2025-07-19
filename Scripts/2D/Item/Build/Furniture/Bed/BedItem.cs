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
            this.isBottomLeft = true;
        }

        /// <inheritdoc/>
        public override void addBuildTask(Vector3Int centerMap)
        {
            base.addBuildTask(centerMap);

            // 添加
            FurnitureManager.Instance.addBed(centerMap);
        }
    }
}
