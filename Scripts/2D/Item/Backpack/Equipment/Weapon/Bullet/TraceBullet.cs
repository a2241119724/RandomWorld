using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class TraceBullet : Bullet
    {
        public Character Target { get; set; }

        /// <summary>
        /// 转弯速度
        /// </summary>
        private float turnSpeed = 0.1f;
        /// <summary>
        /// 旋转的圆心
        /// </summary>
        private Vector3 center = default;
        private static readonly int[] d = new int[] { -1, 1 };
        private int index;
        private float recordTime = 1.0f;
        private float _recordTime;

        protected override void Awake()
        {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Enemy");
        }

        protected override void Update()
        {
            if (Target != null && center == default)
            {
                // TODO Lerp
                Vector3 offset = new Vector3(Target.transform.position.x - transform.position.x - direction.x,
                Target.transform.position.y - transform.position.y - direction.y,
                Target.transform.position.z - transform.position.z - direction.z);
                direction = new Vector3(direction.x + offset.x * turnSpeed * Time.deltaTime, direction.y + offset.y * turnSpeed * Time.deltaTime,
                    direction.z + offset.z * turnSpeed * Time.deltaTime).normalized;
                if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.998f)
                {
                    index = UnityEngine.Random.Range(0, 2);
                    // 垂直线
                    Vector3 _direction = new Vector3(d[index] * direction.y,
                        d[(index + 1) % 2] * direction.x, direction.z).normalized;
                    center = transform.position + _direction;
                }
            }
            else if(center != default)
            {
                // 垂直线
                Vector3 _direction = transform.position - center;
                direction = new Vector3(d[index] * _direction.y, d[(index+1)%2] * _direction.x, 0.0f).normalized;
                _recordTime += Time.deltaTime;
                if (_recordTime >= recordTime)
                {
                    _recordTime = 0.0f;
                    center = default;
                }
            }
            base.Update();
        }

        public override void hitObject()
        {
            if (rayCastHit2D.transform.gameObject.CompareTag("Enemy")) // 击中敌人处理
            {
                Enemy e = rayCastHit2D.transform.GetComponent<Enemy>();
                e.Target = Origin;
                e.reduceHp(Damage);
            }
        }
    }
}
