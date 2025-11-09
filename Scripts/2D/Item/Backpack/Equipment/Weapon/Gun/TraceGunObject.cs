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
            this.attackEffect = AttackEffectManager.EffectType.TraceBullet;
        }

        /// <inheritdoc/>
        protected override void DoAttack(AttackEffect attackEffect)
        {
            TraceBulletEffect traceBulletEffect = attackEffect as TraceBulletEffect;
            traceBulletEffect.Target = EnemyManager.Instance.Get(0);
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
            this.ATN = this.RankRandom(5.0f, 10.0f);
            this.ATK = this.RankRandom(5.0f, 10.0f);
            this.INT = this.RankRandom(5.0f, 10.0f);
            this.CRT = this.RankRandom(5.0f, 10.0f);
            this.CSD = this.RankRandom(5.0f, 10.0f);
            this.HIT = this.RankRandom(5.0f, 10.0f);
            this.RES = this.RankRandom(5.0f, 10.0f);
        }
    }
}
