namespace LAB2D.Item.Backpack.Food
{
    using LAB2D;
    using System;

    /// <summary>
    /// 食物
    /// </summary>
    [Serializable]
    public abstract class AFood : ABackpackItem
    {
        /// <summary>
        /// 吃食物
        /// </summary>
        public abstract void Eat();
    }
}
