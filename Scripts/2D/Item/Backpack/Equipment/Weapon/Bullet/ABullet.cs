namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 子弹
    /// </summary>
    [Serializable]
    public abstract class ABullet : AEquipment
    {
    }

    /// <summary>
    /// 子弹对象
    /// </summary>
    public abstract class ABulletObject : AWeaponObject
    {
        /// <summary>
        /// 子弹方向
        /// </summary>
        protected Vector3 direction;

        private float bulletSpeed; // 子弹速度
        private new ParticleSystem particleSystem;

        /// <summary>
        /// 子弹速度
        /// </summary>
        public float BulletSpeed
        {
            set
            {
                this.bulletSpeed = Mathf.Abs(value);
            }
        }

        /// <summary>
        /// 子弹方向
        /// </summary>
        public Vector3 Direction
        {
            set
            {
                this.direction = value.normalized;
            }
        }

        /// <summary>
        /// 伤害
        /// </summary>
        public float Damage { get; set; } = 5;

        /// <summary>
        /// 发射子弹的角色
        /// </summary>
        public Character Origin { get; set; }

        protected override void Awake()
        {
            base.Awake();
            this.name = this.GetType().Name;
            this.particleSystem = this.GetComponent<ParticleSystem>();
        }

        protected override void Start()
        {
            base.Start();
            this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 0.5f);
            this.direction = new Vector3(this.direction.x, this.direction.y, 0f);
            Destroy(this.gameObject, 5.0f); // 没碰到东西自动销毁

            // Invoke(nameof(destory), 3.0f);
        }
    }
}