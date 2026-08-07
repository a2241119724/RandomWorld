namespace LAB2D.Item.Backpack.Equipment.Weapon.Gun
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 枪
    /// </summary>
    [Serializable]
    public abstract class AGun : AWeapon
    {
    }

    /// <summary>
    /// 枪对象
    /// </summary>
    public abstract class AGunObject : AWeaponObject
    {
        /// <summary>
        /// 子弹速度
        /// </summary>
        protected float bulletSpeed = 30.0f;

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.raduis = 5.0f;
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();
        }
    }
}
