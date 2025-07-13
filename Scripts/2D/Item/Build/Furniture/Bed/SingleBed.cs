using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class SingleBed : BedItem
    {
        public SingleBed()
        {
            width = 1;
            height = 2;
            tile = (TileBase)ResourcesManager.Instance.GetAsset("SingleBed");
        }
    }
}
