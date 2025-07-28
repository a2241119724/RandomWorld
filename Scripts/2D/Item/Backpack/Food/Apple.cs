namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 苹果
    /// </summary>
    [Serializable]
    public class Apple : Food
    {
        public Apple()
        {
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("Apple");
        }
    }

    /// <summary>
    /// 苹果对象
    /// </summary>
    public class AppleObject : FoodObject
    {
        /// <inheritdoc/>
        public override void Eat()
        {
            throw new NotImplementedException();
        }

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