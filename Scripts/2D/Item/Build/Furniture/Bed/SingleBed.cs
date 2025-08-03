namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 单人床
    /// </summary>
    [Serializable]
    public class SingleBed : BedItem
    {
        public SingleBed()
        {
            this.Width = 1;
            this.Height = 2;
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("SingleBed");
        }
    }
}
