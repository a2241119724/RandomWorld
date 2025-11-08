namespace LAB2D
{
    using System;

    /// <summary>
    /// 双人床
    /// </summary>
    [Serializable]
    public class DoubleBed : ABed
    {
        public DoubleBed()
        {
            this.Width = 2;
            this.Height = 2;
        }
    }
}
