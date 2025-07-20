namespace LAB2D
{
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 双人床
    /// </summary>
    public class DoubleBed : BedItem
    {
        public DoubleBed()
        {
            this.Width = 2;
            this.Height = 2;
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("DoubleBed");
        }
    }
}
