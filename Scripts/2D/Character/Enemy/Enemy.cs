using Photon.Pun;
using UnityEngine;

namespace LAB2D
{
    //[Serializable] // 显示类
    public abstract class Enemy : Character, IPunObservable
    {
        [HideInInspector] public float continueTiming = 0.0f; // 搜寻或攻击时间
        [HideInInspector] public bool attackFlag = false; // 搜寻或攻击时间
        [HideInInspector] public Transform enemyHead; // 敌人头的位置
        [HideInInspector] public Vector3 enemyForward; // 敌人的前方
        [HideInInspector] public EnemyStateManager<ICharacterState, EnemyStateType, Enemy> Manager { get; set; }
        [HideInInspector] public Character target; // 打击目标
        public float attackRange = 4.0f; // 敌人攻击距离
        public float rotateInterval = 8.0f; // 敌人漫游时每次转向的时间间隔
        public float moveSpeed = 1.0f; // 敌人移动随机倍数
        public float rotationSpeed = 2.0f; // 敌人旋转的速度

        [SerializeField] protected float SoundRange = 5.0f; // 听觉距离
        [SerializeField] protected float SightRange = 10.0f; // 视觉距离
        [SerializeField] protected float SightAngle = 60.0f; // 视觉角度
        [SerializeField] protected LayerMask layerMask; // 射线检测的层级
        protected int damage; // 伤害值
        private RaycastHit2D raycastHit2D; // 射线射中返回结果
        private EnemyStatusUI statusBar; // 记录实例化血条
        //MeshFilter[] meshFilters; // 需要合并的mesh
        //private CombineInstance[] combine; // 合并用的工具

        protected override void Awake()
        {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Player");
            Manager = new EnemyStateManager<ICharacterState, EnemyStateType,Enemy>(this);
            MaxHp = Hp = 100;
        }

        protected override void Start()
        {
            base.Start();
            enemyHead = transform.Find("Head");
            if (enemyHead == null) {
                Debug.LogError("enemyHead Not Found!!!");
                return;
            }
            statusBar = transform.Find("EnemyHp").GetComponent<EnemyStatusUI>();
            if (statusBar == null)
            {
                Debug.LogError("statusBar Not Found!!!");
                return;
            }
            // 更新敌人身体状况
            statusBar.updateStatus(Hp,MaxHp);
        }

        protected virtual void FixedUpdate()
        {
            enemyForward = enemyHead.position - transform.position;
            if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
            // 由于玩家顶着敌人会使敌人z不为0
            transform.position = new Vector3(transform.position.x,transform.position.y,0);
            if(target == null)
            {
                target = PlayerManager.Instance.Mine;
            }
            // 执行当前状态的函数
            Manager.CurrentState.OnUpdate();
        }

        /// <summary>
        /// 攻击
        /// </summary>
        public abstract void attack();

        /// <summary>
        /// 敌人通过视觉和听觉感知周围是否有target
        /// </summary>
        /// <returns>周围是否有玩家</returns>
        public bool SenseNearby(Transform target)
        {
            if (target == null) {
                Debug.LogError("target is null!!!");
                return false;
            }
            // 计算玩家与敌人之间的距离 
            float dist = Vector3.Distance(target.position, transform.position);
            // 如果玩家与敌人的距离小于敌人的听觉距离(一周)
            // 判断是否听到附近有玩家	
            bool isFind = dist < SoundRange;
            //如果玩家与敌人的距离小于敌人的视觉距离(前方扇形)
            //if (dist < SightRange && !isFind)
            if (dist < SightRange)
            {
                //计算玩家是否在敌人的视角内 	
                Vector3 direction = target.position - transform.position;
                float degree = Vector3.Angle(direction, enemyForward);
                if (degree < SightAngle / 2 && degree > -SightAngle / 2)
                {
                    // 判断玩家和敌人之间是否存在遮挡物
                    raycastHit2D = Physics2D.Raycast(transform.position, direction,SightRange, layerMask); // (源,方向,距离,层级)
                    if (raycastHit2D.collider != null)
                    {
                        if (raycastHit2D.transform.gameObject.CompareTag("Player"))
                        {
                            isFind = true;
                        }
                    }
                }
            }
            return isFind;
        }

        /// <summary>
        /// 向前移动
        /// </summary>
        public void MoveToForward()
        {
            moveSpeed = Random.Range(1.0f, 2.0f);
            transform.Translate(enemyForward.normalized * Time.deltaTime * moveSpeed, Space.World);//向前移动
        }

        /// <summary>
        /// 转向某个方位
        /// </summary>
        /// <param name="direction">转向的方位</param>
        public void RotateTo(Vector3 direction)
        {
            // FromToRotation得到从自定义方向到某方向旋转的角度
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, direction), Time.deltaTime * rotationSpeed);
        }

        public void setPlayer(Player player)
        {
            if(player != null)
            {
                target = player;
            }
        }

        /// <summary>
        /// 敌人掉血
        /// </summary>
        /// <param name="Hp">所掉的血量</param>
        public override void reduceHp(float Hp)
        {
            if(continueTiming > 3.0f)
            {
                attackFlag = true;
            }
            if (Manager.CurrentStateType != EnemyStateType.Attack || 
                (Manager.CurrentStateType == EnemyStateType.Attack && attackFlag))
            {
                Manager.changeState(EnemyStateType.Seek); // 进入搜索状态
            }
            base.reduceHp(Hp);
            statusBar.updateStatus(this.Hp, MaxHp);
            continueTiming = 0.0f; // 重新计时
        }

        protected override void death()
        {
            statusBar.updateStatus(Hp, MaxHp);
            if (PhotonNetwork.IsMasterClient)
            {
                EnemyManager.Instance.remove(this);
            }
            Manager.changeState(EnemyStateType.Dead); // 进入死亡状态
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(attackFlag);
                stream.SendNext(Hp);
            }
            else if(stream.IsReading)
            {
                this.attackFlag = (bool)stream.ReceiveNext();
                this.Hp = (float)stream.ReceiveNext();
                statusBar.updateStatus(this.Hp, this.MaxHp);
            }
        }

        //public void MoveToPlayer(bool isMove)
        //{
        //    Vector3 pointToPlayer = player.transform.position - transform.position;

        //    //float angle = Vector3.Angle(Vector3.up, pointToPlayer.normalized); // 求向量与y轴夹角
        //    //if (Vector3.Dot(pointToPlayer, Vector3.right) > 0.0d) // 两向量点乘,判断在y轴哪一侧
        //    //if (Vector3.Cross(Vector3.up, pointToPlayer).z < 0.0d) // 两向量差乘,判断在y轴哪一侧
        //    //{
        //    //    angle = 360.0f - angle;
        //    //}
        //    //// Euler将欧拉角转换为四元数类型,Lerp(起始方向，终止方向，旋转速度)非匀速
        //    //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * rotationSpeed);

        //    //// LookRotation得到z正方向到某方向旋转的角度
        //    ////transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(pointToPlayer), Time.deltaTime * rotationSpeed); 

        //    // FromToRotation得到从自定义方向到某方向旋转的角度
        //    //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, pointToPlayer), Time.deltaTime * rotationSpeed);
        //}
    }
}