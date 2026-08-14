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

            // 救援传送：若敌人当前格不可通行（被新完成建筑/床的碰撞体困住，可通行=False），
            // 即使放弃目标回 Seek，敌人仍被物理困在原地无法移动。
            // 螺旋搜索最近可通行格并传送脱困，镜像 AWorker.TryRescueFromUnwalkableTile。
            this.TryRescueFromUnwalkableTile();

            this.Target = null;
            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Seek);
        }

        /// <summary>
        /// 救援传送：若敌人当前所在格不可通行（被新完成建筑/床的碰撞体困住），
        /// 螺旋搜索在附近找最近的可通行格并传送过去。
        /// 避免敌人卡在碰撞体上导致"站着不动"且移动状态持续结算卡死。
        /// 镜像 AWorker.TryRescueFromUnwalkableTile。
        /// </summary>
        private void TryRescueFromUnwalkableTile()
        {
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.transform.position);
            if (ASeek.IsCanReach(posMap))
            {
                return; // 当前格可通行，无需救援
            }

            // 螺旋搜索：从内向外按 Chebyshev 距离层遍历，找最近的可行走格。
            // 半径 6 足够覆盖房间家具（床/仓库 3x2 块）附近的空地，且避免远距离瞬移。
            const int maxRadius = 6;
            for (int layer = 1; layer <= maxRadius; layer++)
            {
                for (int dx = -layer; dx <= layer; dx++)
                {
                    for (int dy = -layer; dy <= layer; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != layer)
                        {
                            continue;
                        }

                        Vector3Int candidate = new Vector3Int(posMap.x + dx, posMap.y + dy, 0);
                        if (ASeek.IsCanReach(candidate))
                        {
                            this.transform.position = AWorkerTask.TileMapPositionProvider(candidate);
                            AWorkerTask.LogProvider(
                                $"[EnemyDiag] {this.name} 卡死在不可通行格({posMap.x},{posMap.y}) → 救援传送至({candidate.x},{candidate.y})",
                                LogManager.LogLevelEnum.Warning);
                            return;
                        }
                    }
                }
            }

            // 附近全不可通行：兜底记录，不做传送（避免传送到远处不连贯位置）
            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.name} 卡死在不可通行格({posMap.x},{posMap.y}) 但附近{maxRadius}格无可行走格，无法救援",
                LogManager.LogLevelEnum.Warning);
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
