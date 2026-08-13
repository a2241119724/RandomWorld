namespace LAB2D.Character.Enemy.SeekEnemy
{
    using LAB2D;
    using LAB2D.Character.Enemy.SeekEnemy.State;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using Photon.Pun;
    using UnityEngine;

    public abstract class ASeekEnemy : AEnemy
    {
        private Vector3 direction;

        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; private set; }

        /// <summary>
        /// 获取角色朝向的方向
        /// </summary>
        public override Vector3 Direction
        {
            get
            {
                if (this.Seek.Direction == Vector3.zero)
                {
                    // 攻击方向
                    return this.direction;
                }

                // 寻路方向
                return this.Seek.Direction;
            }

            set
            {
                this.direction = value;
            }
        }

        /// <summary>
        /// 敌人状态管理器.
        /// </summary>
        [HideInInspector]
        public SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy> Manager { get; set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.CharacterDataLAB.Weapon = (AWeapon)AWorkerTask.ItemFactoryProvider(PrefabConstant.CUSTOM_SWORD);
            this.Seek = new AStar(this);
            this.Manager = new SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy>(this);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.AttackRange.SetActive(false);
        }

        public void Update()
        {
            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();
        }

        public void FixedUpdate()
        {
            this.Manager.CurrentState.OnFixedUpdate();
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (this.Manager.CurrentStateType != ASeekEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ASeekEnemyState.TypeEnum.Attack
                && ((SeekEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget))
            {
                this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Move);
            }

            base.ReduceHp(hp, attacker, isCRT);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <inheritdoc/>
        public override void ResetState()
        {
            base.ResetState();
            this.Manager.CurrentState.Reset();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString() +
                $"Target:{this.Seek.TargetMap}\n" +
                $"SeekId:{this.CharacterDataLAB.SeekId}\n";
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

            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Dead); // 进入死亡状态
        }

        /// <summary>
        /// 处理卡死：停止当前寻路，重新发起寻路。
        /// 追击（Target!=null）→ 以目标实时位置重新寻路；
        /// 漫游（Target==null）→ 以当前寻路目标重新寻路。
        /// 原实现 Target==null 时是空操作，导致漫游卡死永不自救——
        /// [StuckDiag] 结算=Stuck ratio=0.00 每秒刷屏、位置永不变化（见 bug-fixes.md 2026-08-13）。
        /// 由每秒位移检测（MovementStuckDetector）在 Move 状态下触发。
        /// </summary>
        public void HandleMovementStuck()
        {
            if (this.Seek == null)
            {
                return;
            }

            this.Seek.StopMove();
            Vector3Int targetMap = this.Target != null
                ? AWorkerTask.TileMapWorldToMapProvider(this.Target.transform.position)
                : this.Seek.TargetMap;
            this.Seek.Seek(targetMap);
        }

        /// <summary>
        /// 卡死熔断：连续多次卡死结算后放弃当前目标，回 Seek 状态换新漫游目标，
        /// 打破"卡死→重新寻路→再卡死"的静默循环。
        /// 触发场景：A* 缓存（WalkabilityCache）判定可通而物理实际被挡（树/家具/墙），
        /// 重寻路仍返回同一路径。不调用 RecordFail：失败缓存是 Worker 决策层共享状态，
        /// 敌人漫游点位记入会污染 Worker 资源目标选择。
        /// </summary>
        public void AbandonMovementStuck()
        {
            if (this.Seek == null)
            {
                return;
            }

            bool wasPursuit = this.Target != null;
            Vector3Int stuckTarget = this.Seek.TargetMap;
            this.Seek.StopMove();
            this.Seek.ResetStuckDetection(); // 清空卡死计数，避免污染下一目标

            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.name} 卡死熔断 目标=({stuckTarget.x},{stuckTarget.y}) 追击={wasPursuit} → 放弃回 Seek",
                LogManager.LogLevelEnum.Debug);

            this.Target = null;
            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Seek);
        }

        /// <summary>
        /// GameObject 销毁时停止寻路线程，
        /// 防止关闭游戏时后台 ThreadPool 线程访问已销毁对象导致卡死。
        /// </summary>
        protected void OnDestroy()
        {
            this.Seek?.StopMove();

            // 清理 LineRenderer 的材质实例
            if (this.Seek?.LineRenderer != null)
            {
                Material mat = this.Seek.LineRenderer.material;
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}
