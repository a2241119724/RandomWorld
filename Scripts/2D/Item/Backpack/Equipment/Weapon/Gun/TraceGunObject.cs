namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 跟踪枪对象
    /// </summary>
    public class TraceGunObject : AGunObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.attackInterval = 0.5f;
            this.attackEffect = AttackEffectManager.EffectTypeEnum.TraceBullet;
        }

        /// <inheritdoc/>
        protected override void DoAttack(AttackEffect attackEffect)
        {
            TraceBulletEffect traceBulletEffect = attackEffect as TraceBulletEffect;
            traceBulletEffect.Target = EnemyManager.Instance.Get(0);
            traceBulletEffect.Direction = this.GetDirection();
        }
    }

    /// <summary>
    /// 跟踪枪
    /// </summary>
    [Serializable]
    public class TraceGun : AGun
    {
        public TraceGun()
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
