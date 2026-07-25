namespace LAB2D.Item.Backpack.Food
{
    using LAB2D;
    using LAB2D.Core;
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 苹果
    /// </summary>
    [Serializable]
    public class Apple : AFood
    {
        public Apple()
        {
            this.Tile = (TileBase)ServiceLocator.Get<ResourceManager>().GetAsset("Apple");
        }

        /// <inheritdoc/>
        public override void Eat()
        {
            throw new NotImplementedException();
        }
    }
}