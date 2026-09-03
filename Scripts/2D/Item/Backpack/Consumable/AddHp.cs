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
        /// <summary>单次回复量（回合制道具菜单与 Use 共用同一来源）。</summary>
        public const float HealAmount = 10f;

        public AddHp()
        {
            this.Tile = (TileBase)ServiceLocator.Get<ResourceManager>().GetAsset("AddHp");
        }

        /// <inheritdoc/>
        public override void Use()
        {
            ServiceLocator.Get<PlayerManager>().Mine.AddHp(AddHp.HealAmount);
        }
    }
}