namespace LAB2D
{
    using System;

    /// <summary>
    /// 单发枪对象
    /// </summary>
    public class SingleGunObject : GunObject
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            this.attackInterval = 0.5f;
            this.name = "SingleGun";
        }

        /// <inheritdoc/>
        protected override void Attack1()
        {
            this.FireBullet(PrefabConstant.PLAYER_BULLET);
        }
    }

    /// <summary>
    /// 单发枪
    /// </summary>
    [Serializable]
    public class SingleGun : Gun
    {
        public SingleGun()
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