namespace LAB2D
{
    using System;

    /// <summary>
    /// 消耗品
    /// </summary>
    [Serializable]
    public abstract class Consumable : BackpackItem
    {
    }

    /// <summary>
    /// 消耗品对象
    /// </summary>
    public abstract class ConsumableObject : BackpackItemObject
    {
        /// <summary>
        /// 使用消耗品
        /// </summary>
        public abstract void Use();

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();
        }
    }
}