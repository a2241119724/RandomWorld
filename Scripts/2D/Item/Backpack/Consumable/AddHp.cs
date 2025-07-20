namespace LAB2D
{
    using System;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 血瓶
    /// </summary>
    [Serializable]
    public class AddHp : Consumable
    {
        public AddHp()
        {
            this.Tile = (TileBase)ResourcesManager.Instance.GetAsset("AddHp");
        }
    }

    /// <summary>
    /// 加血对象
    /// </summary>
    public class AddHpObject : ConsumableObject
    {
        /// <summary>
        /// 加血量
        /// </summary>
        public float Value;

        /// <inheritdoc/>
        public override void Use()
        {
            PlayerManager.Instance.Mine.AddHp(this.Value);
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.name = "AddHp";
        }
    }
}