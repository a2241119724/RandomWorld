namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 剑
    /// </summary>
    [Serializable]
    public abstract class Sword : Weapon
    {
    }

    /// <summary>
    /// 剑对象
    /// 该脚本被玩家装备才会激活
    /// </summary>
    public abstract class SwordObject : WeaponObject
    {
        /// <summary>
        /// 掉血特效
        /// </summary>
        protected GameObject blood;

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.raduis = 8.0f;
            this.blood = ResourceManager.Instance.GetPrefab("Blood");
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