namespace LAB2D
{
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
            this.Tile = (TileBase)ResourceManager.Instance.GetAsset("AddHp");
        }

        /// <inheritdoc/>
        public override void Use()
        {
            PlayerManager.Instance.Mine.AddHp(10);
        }
    }
}