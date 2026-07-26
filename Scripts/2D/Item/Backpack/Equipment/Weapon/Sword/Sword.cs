namespace LAB2D.Item.Backpack.Equipment.Weapon.Sword
{
    using System;

    /// <summary>
    /// 剑
    /// </summary>
    [Serializable]
    public abstract class Sword : AWeapon
    {
    }

    /// <summary>
    /// 剑对象
    /// 该脚本被玩家装备才会激活
    /// </summary>
    public abstract class SwordObject : AWeaponObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.raduis = 8.0f;
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