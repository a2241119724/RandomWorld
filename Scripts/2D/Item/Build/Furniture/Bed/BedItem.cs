using UnityEngine;

namespace LAB2D
{
    public abstract class BedItem : FurnitureItem
    {
        public BedItem()
        {
            isBottomLeft = true;
        }

        public override void addBuildTask(Vector3Int centerMap)
        {
            base.addBuildTask(centerMap);
            // Ìí¼Ó
            FurnitureManager.Instance.addBed(centerMap);
        }
    }
}
