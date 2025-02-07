using Photon.Pun;
using System;
using UnityEngine;

namespace LAB2D
{
    /// <summary>
    /// const ~ static readonly
    /// 编译时，运行时
    /// </summary>
    public abstract class Enemy : Character, IPunObservable
    {
        [HideInInspector] public Transform EnemyHead { get; set; } // 敌人头的位置
        [HideInInspector] public EnemyStateManager<ICharacterState, EnemyStateType, Enemy> Manager { get; set; }
        [HideInInspector] public Character Target { get; set; } // 打击目标
        public readonly float rotateInterval = 8.0f; // 敌人漫游时每次转向的时间间隔
        public readonly float rotationSpeed = 2.0f; // 敌人旋转的速度
        public readonly float attackRange = 4.0f; // 敌人攻击距离
        
        protected readonly float SoundRange = 5.0f; // 听觉距离
        protected readonly float SightRange = 10.0f; // 视觉距离
        protected readonly float SightAngle = 60.0f; // 视觉角度
        protected LayerMask layerMask; // 射线检测的层级
        protected int damage; // 伤害值
        protected readonly float bulletSpeed = 50.0f; // 发射子弹的速度

        private RaycastHit2D raycastHit2D; // 射线射中返回的结果
        private CharacterStatusUI statusBar; // 记录实例化血条
        private const float changeTarget = 3.0f; // 超过当前时间被攻击会被吸引仇恨
        //MeshFilter[] meshFilters; // 需要合并的mesh
        //private CombineInstance[] combine; // 合并用的工具

        protected override void Awake()
        {
            base.Awake();
            layerMask = LayerMask.GetMask("Tile", "Player");
            Manager = new EnemyStateManager<ICharacterState, EnemyStateType,Enemy>(this);
            CharacterDataLAB.MaxHp = CharacterDataLAB.Hp = 100;
        }

        protected override void Start()
        {
            base.Start();
            EnemyHead = transform.Find("Head");
            if (EnemyHead == null) {
                Debug.LogError("enemyHead Not Found!!!");
                return;
            }
            statusBar = transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (statusBar == null)
            {
                Debug.LogError("statusBar Not Found!!!");
                return;
            }
            // 更新敌人身体状况
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
        }

        private void Update()
        {
            // 执行当前状态的函数
            Manager.CurrentState.OnUpdate();
            // 由于玩家顶着敌人会使敌人z不为0
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
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
            if (dist < SightRange && !isFind)
            {
                //计算玩家是否在敌人的视角内 	
                Vector3 direction = target.position - transform.position;
                float degree = Vector3.Angle(direction, EnemyHead.position - transform.position);
                if (degree < SightAngle / 2 && degree > -SightAngle / 2)
                {
                    isFind = true;
                }
            }
            if (isFind)
            {
                // 判断玩家和敌人之间是否存在遮挡物
                Vector3 direction = target.position - transform.position;
                raycastHit2D = Physics2D.Raycast(transform.position, direction, SightRange, layerMask); // (源,方向,距离,层级)
                // 如果有碰撞体并且不是目标，是障碍物
                if (raycastHit2D.collider != null && raycastHit2D.transform != target)
                {
                   isFind = false;
                }
            }
            return isFind;
        }

        /// <summary>
        /// 向前移动
        /// </summary>
        public void MoveToForward()
        {
            moveSpeed = UnityEngine.Random.Range(1.0f, 2.0f);
            transform.Translate(moveSpeed * Time.deltaTime * (EnemyHead.position - transform.position).normalized, Space.World);//向前移动
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

        /// <summary>
        /// 敌人掉血
        /// </summary>
        /// <param name="Hp">所掉的血量</param>
        public override void reduceHp(float Hp)
        {
            //((EnemyAttackState)Manager.CurrentState)
            if (Manager.CurrentStateType != EnemyStateType.Attack || 
                (Manager.CurrentStateType == EnemyStateType.Attack 
                && ((EnemyAttackState)Manager.CurrentState).AttackTime > changeTarget))
            {
                Manager.changeState(EnemyStateType.Seek); // 进入搜索状态
            }
            base.reduceHp(Hp);
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
        }

        protected override void death()
        {
            statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
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
                stream.SendNext(CharacterDataLAB.Hp);
            }
            else if(stream.IsReading)
            {
                CharacterDataLAB.Hp = (float)stream.ReceiveNext();
                statusBar.updateStatus(CharacterDataLAB.Hp, CharacterDataLAB.MaxHp);
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