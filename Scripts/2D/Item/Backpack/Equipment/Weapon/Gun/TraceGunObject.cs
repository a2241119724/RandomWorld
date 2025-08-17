namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 跟踪枪对象
    /// </summary>
    public class TraceGunObject : GunObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.attackInterval = 0.5f;
        }

        /// <inheritdoc/>
        protected override void Attack1()
        {
            GameObject g = this.FireBullet(PrefabConstant.TRACE_BULLET);
            if (g != null && EnemyManager.Instance.Count() > 0)
            {
                g.GetComponent<TraceBullet>().Target = EnemyManager.Instance.Get(0);
            }
        }
    }

    /// <summary>
    /// 跟踪枪
    /// </summary>
    [Serializable]
    public class TraceGun : Gun
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
