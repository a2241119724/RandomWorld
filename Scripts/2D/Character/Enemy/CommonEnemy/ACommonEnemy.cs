namespace LAB2D.Character.Enemy.CommonEnemy
{
    using LAB2D;
    using LAB2D.Character.Enemy.CommonEnemy.State;
    using System;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// const ~ static readonly
    /// 编译时，运行时.
    /// </summary>
    public abstract class ACommonEnemy : AEnemy, IPunObservable
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
        /// 敌人状态管理器.
        /// </summary>
        [HideInInspector]
        public CommonEnemyStateManager<ICharacterState, ACommonEnemyState.TypeEnum, ACommonEnemy> Manager { get; set; }

        /// <summary>
        /// 敌人头的位置.
        /// </summary>
        [HideInInspector]
        public Transform Head { get; set; }

        /// <summary>
        /// 获取角色朝向的方向
        /// </summary>
        public override Vector3 Direction
        {
            get
            {
                return this.Head.position - this.transform.position;
            }
        }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.Manager = new CommonEnemyStateManager<ICharacterState, ACommonEnemyState.TypeEnum, ACommonEnemy>(this);
        }

        public override void Start()
        {
            base.Start();
            this.Head = this.transform.Find("Head");
            if (this.Head == null)
            {
                LogManager.Instance.Log("enemyHead Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
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

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (this.Manager.CurrentStateType != ACommonEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Attack
                && ((CommonEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget))
            {
                this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Seek); // 进入搜索状态
            }

            base.ReduceHp(hp, attacker, isCRT);
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

        /// <inheritdoc/>
        protected override void Death()
        {
            base.Death();
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            if (!NetworkConnect.Instance.IsOnline || PhotonNetwork.IsMasterClient)
            {
                EnemyManager.Instance.Remove(this);
            }

            this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Dead); // 进入死亡状态
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.checkBug.AddColliderCount(DateTime.Now.Ticks);
            if (this.checkBug.IsBug(this.name, 1000) && this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Wander)
            {
                this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Wander);
            }
        }
    }
}