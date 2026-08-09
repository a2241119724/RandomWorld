namespace LAB2D.Item.Backpack.Equipment.Weapon.Sword
{
    using LAB2D;
    using LAB2D.Constant;
    using System;

    /// <summary>
    /// 自定义剑对象
    /// </summary>
    public class CustomSwordObject : SwordObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.name = PrefabConstant.CUSTOM_SWORD;
            this.attackEffect = AttackEffectManager.EffectTypeEnum.KnifeLight;
        }

        /// <inheritdoc/>
        protected override void DoAttack(AttackEffect attackEffect)
        {
        }
    }

    /// <summary>
    /// 自定义剑
    /// </summary>
    [Serializable]
    public class CustomSword : Sword
    {
        public CustomSword()
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