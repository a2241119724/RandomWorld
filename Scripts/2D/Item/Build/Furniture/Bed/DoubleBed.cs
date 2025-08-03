namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 双人床
    /// </summary>
    [Serializable]
    public class DoubleBed : BedItem
    {
        public DoubleBed()
        {
            this.Width = 2;
            this.Height = 2;
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("DoubleBed");
        }
    }
}
