using UnityEngine;

namespace LAB2D
{
    public abstract class Bullet : MonoBehaviour
    {
        public float BulletSpeed { set { bulletSpeed = Mathf.Abs(value); } } // 速度
        public Vector3 Direction { set { direction = value.normalized; } } // 方向
        public float Damage { set; get; } = 5; // 伤害

        [SerializeField] protected LayerMask layerMask; // 射线检测层级
        protected RaycastHit2D rayCastHit2D; // 射线检测返回值
        private float bulletSpeed; // 子弹速度
        private Vector3 direction = Vector3.zero; // 子弹方向
        private Vector3 distance = Vector3.zero; // 每帧位移的距离
        private GameObject blood; // 掉血特效

        protected virtual void Awake() {
            blood = ResourcesManager.Instance.getPrefab("Blood");
        }

        protected virtual void Start()
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, 0.5f);
            direction = new Vector3(direction.x, direction.y, 0f);
            Destroy(gameObject, 3.0f); // 没碰到东西自动销毁
            //Invoke(nameof(destory), 3.0f);
        }

        //private void destory() {
        //    PhotonNetwork.Destroy(gameObject);
        //}

        protected virtual void Update()
        {
            distance = direction * bulletSpeed * Time.deltaTime;
            // 2D射线检测
            rayCastHit2D = Physics2D.Raycast(transform.position, direction, distance.magnitude, layerMask);
            if (rayCastHit2D.collider != null) // 击中目标
            {
                hitObject();
                transform.position = rayCastHit2D.point; // 放到射线射中的位置
                GameObject g = Instantiate(blood, transform.position, Quaternion.identity);
                if (g == null)
                {
                    Debug.LogError("blood Instantiate Error!!!");
                    return;
                }
                else 
                {
                    g.name = blood.name;
                    g.transform.SetParent(rayCastHit2D.collider.transform);
                }
                Destroy(gameObject, 0.25f);
                //Invoke(nameof(destory), 0.25f); // 销毁
                enabled = false; // 控制只执行一次
            }
            else // 未击中目标
            {
                // 子弹运动
                transform.Translate(distance);
            }
            //// 3D射线检测
            //Ray ray = new Ray(transform.position,direction); // 射线
            //RaycastHit hitInfo;
            //// 判断玩家和敌人之间是否存在遮挡物(未实现)	
            //if (Physics.Raycast(ray,out hitInfo, 100.0f, layerMask)) // ((源,方向),返回值,距离,层级)
            //{
            //}
        }

        // 击中物体后做
        public abstract void hitObject();

        //private void OnTriggerEnter2D(Collider2D collision)
        //{
        //    // 排除发射子弹时与玩家的碰撞
        //    if (collision.gameObject.name != "gun" && collision.gameObject.name != "player"
        //        && collision.gameObject.name != "sword")
        //    {
        //        gameObject.GetComponent<ParticleSystem>().Play();
        //        transform.GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        //        Destroy(transform.gameObject, 0.25f);
        //    }
        //}
    }
}