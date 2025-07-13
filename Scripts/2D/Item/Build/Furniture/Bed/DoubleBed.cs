using UnityEngine.Tilemaps;

namespace LAB2D
{
    public class DoubleBed : BedItem
    {
        public DoubleBed()
        {
            width = 2;
            height = 2;
            tile = (TileBase)ResourcesManager.Instance.GetAsset("DoubleBed");
        }
    }
}
