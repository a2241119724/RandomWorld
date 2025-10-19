namespace LAB2D
{
    using System;

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
        }
    }
}
