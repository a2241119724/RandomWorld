namespace LAB2D.Item.Backpack.Equipment.Weapon.Gun
{
    using System;
    using UnityEngine;
    using Random = UnityEngine.Random;

    /// <summary>
    /// 枪
    /// </summary>
    [Serializable]
    public abstract class AGun : AWeapon
    {
        /// <summary>
        /// 枽弹实际射程（VOL 速度 30 × 粒子寿命 1s = 30 格）远超瞄准范围，
        /// 攻击距离取瞄准范围（AGunObject.raduis=5）：瞄得到的必打得着。
        /// </summary>
        public override float AttackReach => 5.0f;
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

        /// <summary>
        /// 漂移角度（度），子弹发射方向在此范围内随机偏移。0 = 无漂移。
        /// </summary>
        protected float driftAngle = 5.0f;

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

        /// <summary>
        /// 攻击时应用漂移：在发射角度上叠加随机偏移。
        /// </summary>
        [Photon.Pun.PunRPC]
        public override void Attack()
        {
            if (this.driftAngle > 0.01f)
            {
                // 保存原始角度，攻击结束后恢复（不影响武器朝向展示）
                float originalAngle = this.transform.rotation.eulerAngles.z;
                float drift = Random.Range(-this.driftAngle / 2f, this.driftAngle / 2f);
                float driftedAngle = originalAngle + drift;
                this.transform.rotation = Quaternion.Euler(0, 0, driftedAngle);

                base.Attack();

                // 恢复原始角度
                this.transform.rotation = Quaternion.Euler(0, 0, originalAngle);
            }
            else
            {
                base.Attack();
            }
        }
    }
}
