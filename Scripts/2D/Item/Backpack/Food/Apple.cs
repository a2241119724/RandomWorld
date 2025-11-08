namespace LAB2D
{
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
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("Apple");
        }

        /// <inheritdoc/>
        public override void Eat()
        {
            throw new NotImplementedException();
        }
    }
}