namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// const ~ static readonly
    /// 编译时，运行时.
    /// </summary>
    public abstract class Enemy : Character, IPunObservable
    {
        /// <summary>
        /// 敌人漫游时每次转向的时间间隔.
        /// </summary>
        public readonly float RotateInterval = 20.0f;

        /// <summary>
        /// 敌人旋转的速度.
        /// </summary>
        public readonly float RotationSpeed = 2.0f;

        /// <summary>
        /// 伤害值.
        /// </summary>
        protected int damage;

        private const float ChangeTarget = 3.0f; // 超过当前时间被攻击会被吸引仇恨
        private RaycastHit2D raycastHit2D; // 射线射中返回的结果
        private CharacterStatusUI statusBar; // 记录实例化血条

        // MeshFilter[] meshFilters; // 需要合并的mesh
        // private CombineInstance[] combine; // 合并用的工具

        /// <summary>
        /// 敌人头的位置.
        /// </summary>
        [HideInInspector]
        public Transform Head { get; set; }

        /// <summary>
        /// 敌人状态管理器.
        /// </summary>
        [HideInInspector]
        public EnemyStateManager<ICharacterState, EnemyState.EnemyStateTypeEnum, Enemy> Manager { get; set; }

        /// <summary>
        /// 敌人攻击目标.
        /// </summary>
        [HideInInspector]
        public Character Target { get; set; } // 打击目标

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.AttackLayers = LayerMask.GetMask("Tile", LayerConstant.PLAYER_LAAYER, LayerConstant.WORKER_LAAYER);
            this.AttackTags = new List<string>
            {
                "Player",
                "Worker",
            };
            this.Manager = new EnemyStateManager<ICharacterState, EnemyState.EnemyStateTypeEnum, Enemy>(this);
            this.CharacterDataLAB = new EnemyData();
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.Head = this.transform.Find("Head");
            if (this.Head == null)
            {
                LogManager.Instance.Log("enemyHead Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            this.statusBar = this.transform.Find("Hp").GetComponent<CharacterStatusUI>();
            if (this.statusBar == null)
            {
                LogManager.Instance.Log("statusBar Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }

            // 更新敌人身体状况
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        public void Update()
        {
            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();

            // 由于玩家顶着敌人会使敌人z不为0
            Vector3 pos = this.transform.position;
            if (Mathf.Abs(pos.z) > 0.001f)
            {
                pos.z = 0;
                this.transform.position = pos;
            }
        }

        /// <summary>
        /// 敌人通过视觉和听觉感知周围是否有target.
        /// </summary>
        /// <param name="target">目标.</param>
        /// <returns>周围是否有目标.</returns>
        public bool SenseNearby(Transform target)
        {
            if (target == null)
            {
                LogManager.Instance.Log("target is null!!!", LogManager.LogLevelEnum.Error);
                return false;
            }

            EnemyData enemyData = this.CharacterDataLAB as EnemyData;

            // 计算玩家与敌人之间的距离
            float dist = Vector3.Distance(target.position, this.transform.position);

            // 如果玩家与敌人的距离小于敌人的听觉距离(一周)
            // 判断是否听到附近有玩家
            bool isFind = dist < enemyData.SoundRange;

            // 如果玩家与敌人的距离小于敌人的视觉距离(前方扇形)
            if (dist < enemyData.SightRange && !isFind)
            {
                // 计算玩家是否在敌人的视角内
                Vector3 direction = target.position - this.transform.position;
                float degree = Vector3.Angle(direction, this.Head.position - this.transform.position);
                if (degree < enemyData.SightAngle / 2 && degree > -enemyData.SightAngle / 2)
                {
                    isFind = true;
                }
            }

            if (isFind)
            {
                // 判断玩家和敌人之间是否存在遮挡物
                Vector3 direction = target.position - this.transform.position;
                this.raycastHit2D = Physics2D.Raycast(this.transform.position, direction, enemyData.SightRange, this.AttackLayers); // (源,方向,距离,层级)

                // 如果有碰撞体并且不是目标，是障碍物
                if (this.raycastHit2D.collider != null && this.raycastHit2D.transform != target)
                {
                    isFind = false;
                }
            }

            return isFind;
        }

        /// <summary>
        /// 向前移动.
        /// </summary>
        public void MoveToForward()
        {
            this.MoveSpeed = UnityEngine.Random.Range(1.0f, 2.0f);
            this.transform.Translate(this.MoveSpeed * Time.deltaTime * (this.Head.position - this.transform.position).normalized, Space.World); // 向前移动
        }

        /// <summary>
        /// 转向某个方位.
        /// </summary>
        /// <param name="direction">转向的方位</param>
        public void RotateTo(Vector3 direction)
        {
            // FromToRotation得到从自定义方向到某方向旋转的角度
            this.transform.rotation = Quaternion.Lerp(this.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction), Time.deltaTime * this.RotationSpeed);
        }

        /// <summary>
        /// 敌人掉血
        /// </summary>
        /// <param name="hp">所掉的血量</param>
        public override void ReduceHp(float hp)
        {
            // ((EnemyAttackState)Manager.CurrentState)
            if (this.Manager.CurrentStateType != EnemyState.EnemyStateTypeEnum.Attack ||
                (this.Manager.CurrentStateType == EnemyState.EnemyStateTypeEnum.Attack
                && ((EnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget))
            {
                this.Manager.ChangeState(EnemyState.EnemyStateTypeEnum.Seek); // 进入搜索状态
            }

            base.ReduceHp(hp);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.CharacterDataLAB.Hp);
            }
            else if (stream.IsReading)
            {
                this.CharacterDataLAB.Hp = (float)stream.ReceiveNext();
                this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            }
        }

        /// <summary>
        /// 死亡.
        /// </summary>
        protected override void Death()
        {
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            if (!NetworkConnect.Instance.IsOnline || PhotonNetwork.IsMasterClient)
            {
                EnemyManager.Instance.Remove(this);
            }

            this.Manager.ChangeState(EnemyState.EnemyStateTypeEnum.Dead); // 进入死亡状态
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.checkBug.AddColliderCount(DateTime.Now.Ticks);
            if (this.checkBug.IsBug(this.name, 200) && this.Manager.CurrentStateType == EnemyState.EnemyStateTypeEnum.Wander)
            {
                this.Manager.ChangeState(EnemyState.EnemyStateTypeEnum.Wander);
            }
        }

        // public void MoveToPlayer(bool isMove)
        // {
        //     Vector3 pointToPlayer = player.transform.position - transform.position;
        //     //float angle = Vector3.Angle(Vector3.up, pointToPlayer.normalized); // 求向量与y轴夹角
        //     //if (Vector3.Dot(pointToPlayer, Vector3.right) > 0.0d) // 两向量点乘,判断在y轴哪一侧
        //     //if (Vector3.Cross(Vector3.up, pointToPlayer).z < 0.0d) // 两向量差乘,判断在y轴哪一侧
        //     //{
        //     //    angle = 360.0f - angle;
        //     //}
        //     //// Euler将欧拉角转换为四元数类型,Lerp(起始方向，终止方向，旋转速度)非匀速
        //     //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * rotationSpeed);
        //     //// LookRotation得到z正方向到某方向旋转的角度
        //     ////transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(pointToPlayer), Time.deltaTime * rotationSpeed);
        //     // FromToRotation得到从自定义方向到某方向旋转的角度
        //     //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(Vector3.up, pointToPlayer), Time.deltaTime * rotationSpeed);
        // }

        /// <summary>
        /// 敌人数据
        /// </summary>
        [Serializable]
        public class EnemyData : CharacterData
        {
            /// <summary>
            /// 敌人攻击范围.
            /// </summary>
            public float AttackRange = 4.0f;

            /// <summary>
            /// 听觉距离.
            /// </summary>
            public float SoundRange = 5.0f;

            /// <summary>
            /// 视野距离.
            /// </summary>
            public float SightRange = 10.0f;

            /// <summary>
            /// 视野角度.
            /// </summary>
            public float SightAngle = 60.0f;

            /// <summary>
            /// 发射子弹的速度.
            /// </summary>
            public float BulletSpeed = 50.0f;
        }
    }
}