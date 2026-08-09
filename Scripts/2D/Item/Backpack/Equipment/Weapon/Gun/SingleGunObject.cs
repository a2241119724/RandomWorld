namespace LAB2D.Item.Backpack.Equipment.Weapon.Gun
{
    using LAB2D;
    using LAB2D.Constant;
    using System;

    /// <summary>
    /// 单发枪对象
    /// </summary>
    public class SingleGunObject : AGunObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.name = PrefabConstant.SINGLE_GUN;
            this.attackEffect = AttackEffectManager.EffectTypeEnum.Bullet;
        }

        /// <inheritdoc/>
        protected override void DoAttack(AttackEffect attackEffect)
        {
            attackEffect.Speed = this.bulletSpeed;
        }
    }

    /// <summary>
    /// 单发枪
    /// </summary>
    [Serializable]
    public class SingleGun : AGun
    {
        public SingleGun()
        {
            this.Attribute.ATN = this.RankRandom(5.0f, 10.0f);
            this.Attribute.INT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.CRT = this.RankRandom(0.05f, 0.1f);
            this.Attribute.CSD = this.RankRandom(0f, 1.0f);
            this.Attribute.HIT = this.RankRandom(5.0f, 10.0f);
            this.Attribute.RES = this.RankRandom(5.0f, 10.0f);
            this.Attribute.SPD = this.RankRandom(5.0f, 10.0f);
            this.Attribute.DEF = this.RankRandom(5.0f, 10.0f);
        }
    }
}