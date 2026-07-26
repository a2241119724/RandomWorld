namespace LAB2D.Item.Build.Furniture.Bed
{
    using System;

    /// <summary>
    /// 单人床
    /// </summary>
    [Serializable]
    public class SingleBed : ABed
    {
        public SingleBed()
        {
            this.Width = 1;
            this.Height = 2;
        }
    }
}
