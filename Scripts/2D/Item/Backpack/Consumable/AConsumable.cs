namespace LAB2D.Item.Backpack.Consumable
{
    using LAB2D;
    using System;

    /// <summary>
    /// 消耗品
    /// </summary>
    [Serializable]
    public abstract class AConsumable : ABackpackItem
    {
        /// <summary>
        /// 使用消耗品
        /// </summary>
        public abstract void Use();
    }
}