namespace LAB2D.Item.Backpack.Consumable
{
    using LAB2D;
    using LAB2D.Core;
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 血瓶
    /// </summary>
    [Serializable]
    public class AddHp : AConsumable
    {
        public AddHp()
        {
            this.Tile = (TileBase)ServiceLocator.Get<ResourceManager>().GetAsset("AddHp");
        }

        /// <inheritdoc/>
        public override void Use()
        {
            ServiceLocator.Get<PlayerManager>().Mine.AddHp(10);
        }
    }
}