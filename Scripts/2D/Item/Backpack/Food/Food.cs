namespace LAB2D
{
    using System;

    /// <summary>
    /// 食物
    /// </summary>
    [Serializable]
    public abstract class Food : BackpackItem
    {
    }

    /// <summary>
    /// 食物对象
    /// </summary>
    public abstract class FoodObject : BackpackItemObject
    {
        /// <summary>
        /// 吃食物
        /// </summary>
        public abstract void Eat();

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
