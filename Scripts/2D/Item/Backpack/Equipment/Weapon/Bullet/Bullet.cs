namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 子弹
    /// </summary>
    public abstract class Bullet : MonoBehaviour
    {
        /// <summary>
        /// 射线检测层级
        /// </summary>
        [SerializeField]
        protected LayerMask layerMask;

        /// <summary>
        /// 子弹方向
        /// </summary>
        protected Vector3 direction;

        /// <summary>
        /// 射线检测返回值
        /// </summary>
        protected RaycastHit2D rayCastHit2D;
        private float bulletSpeed; // 子弹速度
        private Vector3 distance; // 每帧位移的距离

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

        /// <summary>
        /// 击中物体后做
        /// </summary>
        public abstract void HitObject();

        protected virtual void Awake()
        {
            this.name = this.GetType().Name;
        }

        protected virtual void Start()
        {
            this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 0.5f);
            this.direction = new Vector3(this.direction.x, this.direction.y, 0f);
            Destroy(this.gameObject, 5.0f); // 没碰到东西自动销毁

            // Invoke(nameof(destory), 3.0f);
        }

        // private void destory() {
        //     PhotonNetwork.Destroy(gameObject);
        // }
        protected virtual void Update()
        {
            this.distance = this.bulletSpeed * Time.deltaTime * this.direction;

            // 2D射线检测
            this.rayCastHit2D = Physics2D.Raycast(this.transform.position, this.direction, this.distance.magnitude, this.layerMask);

            // 击中目标
            if (this.rayCastHit2D.collider != null)
            {
                this.HitObject();
                this.transform.position = this.rayCastHit2D.point; // 放到射线射中的位置
                GameObject g = ResourceManager.Instance.Instantiate(PrefabConstant.DAMAGE);
                if (g == null)
                {
                    return;
                }

                g.transform.SetParent(this.rayCastHit2D.collider.transform);
                Destroy(this.gameObject, 0.25f);

                // Invoke(nameof(destory), 0.25f); // 销毁
                this.enabled = false; // 控制只执行一次
            }

            // 未击中目标
            else
            {
                // 子弹运动
                this.transform.Translate(this.distance);
            }

            // // 3D射线检测
            // Ray ray = new Ray(transform.position,direction); // 射线
            // RaycastHit hitInfo;
            // // 判断玩家和敌人之间是否存在遮挡物(未实现)
            // if (Physics.Raycast(ray,out hitInfo, 100.0f, layerMask)) // ((源,方向),返回值,距离,层级)
            // {
            // }
        }

        // private void OnTriggerEnter2D(Collider2D collision)
        // {
        //     // 排除发射子弹时与玩家的碰撞
        //     if (collision.gameObject.name != "gun" && collision.gameObject.name != "player"
        //         && collision.gameObject.name != "sword")
        //     {
        //         gameObject.GetComponent<ParticleSystem>().Play();
        //         transform.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        //         Destroy(transform.gameObject, 0.25f);
        //     }
        // }
    }
}