namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 跟踪子弹
    /// </summary>
    public class TraceBullet : ABulletObject
    {
        private const float RecordTime = 1.0f;
        private static readonly int[] D = new int[] { -1, 1 };
        private readonly float turnSpeed = 0.1f; // 转弯速度
        private Vector3 center = default; // 旋转的圆心
        private int index;
        private float recordTime;

        /// <summary>
        /// 跟踪目标
        /// </summary>
        public Character Target { get; set; }

        protected override void DoAttack()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            if (this.Target != null && this.center == default)
            {
                // TODO Lerp
                Vector3 offset = new (
                    this.Target.transform.position.x - this.transform.position.x - this.direction.x,
                    this.Target.transform.position.y - this.transform.position.y - this.direction.y,
                    this.Target.transform.position.z - this.transform.position.z - this.direction.z);
                this.direction = new Vector3(
                    this.direction.x + (offset.x * this.turnSpeed * Time.deltaTime),
                    this.direction.y + (offset.y * this.turnSpeed * Time.deltaTime),
                    this.direction.z + (offset.z * this.turnSpeed * Time.deltaTime)).normalized;
                if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.998f)
                {
                    this.index = UnityEngine.Random.Range(0, 2);

                    // 垂直线
                    Vector3 direction = new Vector3(
                        D[this.index] * this.direction.y,
                        D[(this.index + 1) % 2] * this.direction.x,
                        this.direction.z).normalized;
                    this.center = this.transform.position + direction;
                }
            }
            else if (this.center != default)
            {
                // 垂直线
                Vector3 direction1 = this.transform.position - this.center;
                this.direction = new Vector3(D[this.index] * direction1.y, D[(this.index + 1) % 2] * direction1.x, 0.0f).normalized;
                this.recordTime += Time.deltaTime;
                if (this.recordTime >= RecordTime)
                {
                    this.recordTime = 0.0f;
                    this.center = default;
                }
            }

            base.Update();
        }
    }
}
