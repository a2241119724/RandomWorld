namespace LAB2D.Item.Backpack.Equipment.Weapon.Gun
{
    using LAB2D;
    using Character = LAB2D.Character.Character;
    using UnityEngine;

    /// <summary>
    /// 跟踪子弹
    /// </summary>
    public class TraceBulletEffect : AttackEffect
    {
        private const float RecordTime = 1.0f;
        private static readonly int[] D = new int[] { -1, 1 };
        private float turnSpeed = 0.1f; // 转弯速度
        private Vector3 center = default; // 旋转的圆心
        private int index;
        private float recordTime;
        private float attackSpeed = 20.0f;

        /// <summary>
        /// 跟踪方向
        /// </summary>
        public Vector3 Direction { get; set; } = Vector3.right;

        /// <summary>
        /// 跟踪目标
        /// </summary>
        public Character Target { get; set; }

        /// <summary>
        /// 攻击速度
        /// </summary>
        public float AttackSpeed
        {
            get
            {
                return this.attackSpeed;
            }

            set
            {
                this.AttackSpeed = value;
                this.turnSpeed = value / 500.0f;
            }
        }

        public void Update()
        {
            this.transform.localRotation = Quaternion.Euler(0, 0, 0);

            // 直线方向
            if (this.Target != null && this.center == default)
            {
                // TODO Lerp
                Vector3 offset = new (
                    this.Target.transform.position.x - this.transform.position.x - this.Direction.x,
                    this.Target.transform.position.y - this.transform.position.y - this.Direction.y,
                    this.Target.transform.position.z - this.transform.position.z - this.Direction.z);
                this.Direction = new Vector3(
                    this.Direction.x + (offset.x * this.turnSpeed * Time.deltaTime),
                    this.Direction.y + (offset.y * this.turnSpeed * Time.deltaTime),
                    this.Direction.z + (offset.z * this.turnSpeed * Time.deltaTime)).normalized;
                if (Random.Range(0.0f, 1.0f) > 0.998f)
                {
                    this.index = Random.Range(0, 2);

                    // 垂直线
                    Vector3 direction = new Vector3(
                        D[this.index] * this.Direction.y,
                        D[(this.index + 1) % 2] * this.Direction.x,
                        this.Direction.z).normalized;
                    this.center = this.transform.position + direction;
                }
            }

            // 旋转方向
            else if (this.center != default)
            {
                // 垂直线
                Vector3 direction1 = this.transform.position - this.center;
                this.Direction = new Vector3(D[this.index] * direction1.y, D[(this.index + 1) % 2] * direction1.x, 0.0f).normalized;
                this.recordTime += Time.deltaTime;
                if (this.recordTime >= RecordTime)
                {
                    this.recordTime = 0.0f;
                    this.center = default;
                }
            }

            // 运动
            if (this.ps.isPlaying)
            {
                this.transform.Translate(this.Direction * this.AttackSpeed * Time.deltaTime);
            }
        }
    }
}
