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
        public readonly float RotationSpeed = 5.0f;

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
        public override string GetStateLabel() => this.Manager.CurrentStateType switch
        {
            ACommonEnemyState.TypeEnum.Wander => "漫游",
            ACommonEnemyState.TypeEnum.Seek => "搜索",
            ACommonEnemyState.TypeEnum.Chase => "追踪",
            ACommonEnemyState.TypeEnum.Attack => "攻击",
            ACommonEnemyState.TypeEnum.Dead => "死亡",
            _ => this.Manager.CurrentStateType.ToString(),
        };

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
                AWorkerTask.LogProvider("enemyHead Not Found!!!", LogManager.LogLevelEnum.Error);
                return;
            }
        }

        public void Update()
        {
            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();

            // 由于玩家顶着敌人会使敌人z不为0
            Vector3 pos = this.transform.position;
            if (System.Math.Abs(pos.z) > 0.001f)
            {
                pos.z = 0;
                this.transform.position = pos;
            }
        }

        public void FixedUpdate()
        {
            this.Manager.CurrentState.OnFixedUpdate();
        }

        /// <summary>
        /// 向前移动.
        /// </summary>
        public void MoveToForward()
        {
            this.MoveSpeed = UnityEngine.Random.Range(4.5f, 6.0f);
            this.transform.Translate(this.MoveSpeed * Time.fixedDeltaTime * (this.Head.position - this.transform.position).normalized, Space.World); // 向前移动
        }

        /// <summary>
        /// 转向某个方位.
        /// </summary>
        /// <param name="direction">转向的方位</param>
        public void RotateTo(Vector3 direction)
        {
            // FromToRotation得到从自定义方向到某方向旋转的角度
            this.transform.rotation = Quaternion.Lerp(this.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction), Time.fixedDeltaTime * this.RotationSpeed);
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            // 被打换目标需排除"打我的就是当前攻击目标"：否则单目标战斗中敌人每攻击几秒
            // 被反击一次就切 Seek → 又切回 Attack，武器反复销毁重建，拿起瞬间方向跳变
            // （同 ASeekEnemy.ReduceHp，见 bug-fixes.md 2026-08-16）。只有被其他目标打才换。
            if (this.Manager.CurrentStateType != ACommonEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Attack
                && ((CommonEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget
                && attacker != this.Target))
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
            if (!this.NetworkView.IsOnline || this.NetworkView.IsMasterClient)
            {
                Core.GameServices.EnemyRemoveProvider(this);
            }

            this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Dead); // 进入死亡状态
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            this.collisionBugDetector.AddColliderCount(DateTime.Now.Ticks, this.transform.position);
            BugCheckResult bugResult = this.collisionBugDetector.CheckBug(this.name);

            if (bugResult == BugCheckResult.Sliding)
            {
                // 贴墙滑动 → 预防性重新寻路/换向。
                // 无目标时不能进 Seek：CommonEnemySeekState.OnUpdate 在 Target==null 时第一帧就退回
                // Wander，形成 ← Wander → Seek ← Seek → Wander 的同毫秒无效乒乓（每 ~1s 一次，
                // 刷爆 [EnemyDiag] 日志，见 bug-fixes.md 2026-08-15）。直接重入 Wander 让 OnEnter
                // (recordTime=9999) 立即换新方向。
                this.collisionBugDetector.ColliderCount = 0;
                this.Manager.ChangeState(this.Target != null
                    ? ACommonEnemyState.TypeEnum.Seek
                    : ACommonEnemyState.TypeEnum.Wander);
                return;
            }

            if (bugResult == BugCheckResult.Stuck)
            {
                this.collisionBugDetector.ColliderCount = 0; // 重置计数器

                ACommonEnemyState.TypeEnum currentState = this.Manager.CurrentStateType;

                // Wander 状态：重新随机方向
                if (currentState == ACommonEnemyState.TypeEnum.Wander)
                {
                    this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Wander);
                }
                // Chase/Attack/Seek 状态：切换到 Seek 重新搜索路径。
                // 无目标时进 Seek 会立即退回 Wander（同上），改回 Wander 避免无效乒乓。
                else if (currentState == ACommonEnemyState.TypeEnum.Chase
                    || currentState == ACommonEnemyState.TypeEnum.Attack
                    || currentState == ACommonEnemyState.TypeEnum.Seek)
                {
                    this.Manager.ChangeState(this.Target != null
                        ? ACommonEnemyState.TypeEnum.Seek
                        : ACommonEnemyState.TypeEnum.Wander);
                }
            }
        }
    }
}