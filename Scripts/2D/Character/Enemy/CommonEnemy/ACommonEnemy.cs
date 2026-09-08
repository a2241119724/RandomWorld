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
        /// 敌人前方阻挡探测距离(短距离).
        /// </summary>
        public readonly float ProbeDistance = 0.6f;

        /// <summary>
        /// 墙体层(与 ASeek 墙壁探测一致):前方探测只看地形/建筑墙,不看角色.
        /// 注:LayerMask.GetMask 不能在静态初始化器调用(MonoBehaviour cctor 限制),Start 中赋值.
        /// </summary>
        private LayerMask wallLayers;

        /// <summary>
        /// 角色层(Player/Worker/Enemy):MoveToForward 移动落点的避让探测用.
        /// 同为 Start 赋值(静态初始化器限制,同 wallLayers).
        /// </summary>
        private LayerMask characterLayers;

        /// <summary>
        /// 角色避让探测结果缓冲(OverlapCircleNonAlloc 共享,避免每帧分配,
        /// 仿 AWeaponObject.retCollider2Ds 共享数组惯例).
        /// </summary>
        private static readonly Collider2D[] characterProbeBuffer = new Collider2D[8];

        /// <summary>
        /// 矩形碰撞体缓存(Start 中获取).
        /// </summary>
        private BoxCollider2D boxCollider;

        /// <summary>
        /// 前方探测射线可视化开关(Scene 视图:红=命中 绿=通畅),诊断用.
        /// </summary>
        public static bool ShowForwardProbe { get; set; } = true;

        /// <summary>
        /// 最近一次前方探测的首个命中(供受阻换向日志打印命中物详情).
        /// </summary>
        public RaycastHit2D LastProbeHit { get; private set; }

        /// <summary>
        /// 目标锁定截止时刻（Time.time 轴）：被打锁定最近攻击者后 <see cref="AEnemy.ChangeTarget"/>
        /// 秒内被其他目标打不换目标——新攻击者需持续攻击到锁定到期才能抢走目标
        ///（2026-09-07 用户要求；锁定中被当前目标再打会刷新，语义同 Worker.OnHit）。
        /// </summary>
        private float targetFocusEndTime;

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

            // 物理配置（对齐 ASeek 构造器惯例，100 敌人卡顿治理 2026-09-06）：
            // gravityScale=0 —— 语义防御：敌人 transform 直写移动（MoveToForward），
            //   无重力下落语义；当前全局重力已是 (0,0)，此设置保证将来重力改动不波及敌人。
            // freezeRotation=true —— 旋转由 RotateTo 直写 transform.rotation 驱动，
            //   冻结 rb 旋转积分防物理扭矩与直写互相打架（2026-08-15 冻结 Z 根治抖动同源）。
            // 注意：不动 interpolation（与 transform 直写移动互相打架）、不改移动方式
            //（rb.MovePosition 在 Worker 上实测"又慢又卡"被否，见 bug-fixes.md 2026-08-15）。
            Rigidbody2D rb = this.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }

            this.Manager = new CommonEnemyStateManager<ICharacterState, ACommonEnemyState.TypeEnum, ACommonEnemy>(this);
        }

        public override void Start()
        {
            base.Start();
            this.boxCollider = this.GetComponent<BoxCollider2D>();
            this.wallLayers = LayerMask.GetMask("Tile", "BuildTile");
            this.characterLayers = LayerMask.GetMask("Player", "Worker", "Enemy");
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

            // HeadUI 相机视野剔除（%32 错相，实例 id 低位）：相机外头顶 Canvas 关渲染，
            // 省掉 UGUI.Rendering.UpdateBatches 对不可见 Canvas 的每帧批次计算
            if ((Time.frameCount + (this.GetInstanceID() & 31)) % 32 == 0)
            {
                this.UpdateHeadUiVisibility();
            }
        }

        public void FixedUpdate()
        {
            this.Manager.CurrentState.OnFixedUpdate();
        }

        /// <summary>
        /// 向前移动.
        /// 移动落点被其他角色占据时本帧不动（角色避让）：本类移动是 transform 直写，
        /// 会覆盖物理解算——直写硬闯 Worker（rb.velocity 物理通道、碰撞体 20 倍轻）
        /// 时穿透修正全转嫁给 Worker，推无可推（背后是墙）即两者压叠
        ///（2026-09-08 用户报告"Worker 与 CommonEnemy 重合"）。挡住时停下等对方走开，
        /// Wander 由换向计时（12-18s）兜底、追击由目标移动自然解除。
        /// </summary>
        public void MoveToForward()
        {
            this.MoveSpeed = UnityEngine.Random.Range(1.5f, 2.0f);
            Vector3 step = this.MoveSpeed * Time.fixedDeltaTime * (this.Head.position - this.transform.position).normalized;
            if (!this.IsCharacterBlockingStep(step))
            {
                this.transform.Translate(step, Space.World); // 向前移动
            }
        }

        /// <summary>
        /// 移动落点是否被其他角色占据：新位置圆形探测（近似 BoxCollider 扫掠区，
        /// 半径 0.5≈0.8 箱半宽+余量）命中 Player/Worker/Enemy 且非自身 collider 即阻挡.
        /// 起点在自身 box 内必然命中自己，须按 collider 引用过滤.
        /// </summary>
        private bool IsCharacterBlockingStep(Vector3 step)
        {
            if (this.boxCollider == null)
            {
                return false;
            }

            Vector2 probePos = this.transform.position + step;
            int hits = Physics2D.OverlapCircleNonAlloc(probePos, 0.5f, characterProbeBuffer, this.characterLayers);
            for (int i = 0; i < hits; i++)
            {
                Collider2D collider = characterProbeBuffer[i];
                if (collider != null && collider != this.boxCollider)
                {
                    return true;
                }
            }

            return false;
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

        /// <summary>
        /// 矩形两边及中线射线探测:沿 direction 前方短距离是否有墙。
        /// 三条射线——中线(origin=中心)、两条侧边(origin=中心±垂直方向×半宽),
        /// 任一命中即视为前方受阻。仅探测 Tile/BuildTile 层,天然不命中自身与角色。
        /// </summary>
        /// <param name="direction">探测方向(单位向量)</param>
        /// <returns>true=前方短距离内有墙</returns>
        public bool IsForwardBlocked(Vector3 direction)
        {
            if (this.boxCollider == null)
            {
                return false;
            }

            Vector2 pos = this.transform.position;
            Vector2 dir = ((Vector2)direction).normalized;
            Vector2 perp = new (-dir.y, dir.x); // 垂直于探测方向(矩形宽度方向)

            // 半尺寸:BoxCollider2D.size × lossyScale;矩形在任意朝向下沿 dir/垂直 dir 的
            // 支撑半径 = hx·|投影| + hy·|投影|(正方形/长方形均成立,随旋转自适应)
            Vector2 lossy = this.transform.lossyScale;
            float hx = this.boxCollider.size.x * 0.5f * lossy.x;
            float hy = this.boxCollider.size.y * 0.5f * lossy.y;
            Vector2 right = this.transform.right;
            Vector2 up = this.transform.up;
            float halfAlong = (hx * Mathf.Abs(Vector2.Dot(dir, right))) + (hy * Mathf.Abs(Vector2.Dot(dir, up)));
            float halfAcross = (hx * Mathf.Abs(Vector2.Dot(perp, right))) + (hy * Mathf.Abs(Vector2.Dot(perp, up)));

            // 从中心出发需先覆盖矩形自身半长,再向前探测 ProbeDistance
            float probeDist = halfAlong + this.ProbeDistance;
            RaycastHit2D hitCenter = Physics2D.Raycast(pos, dir, probeDist, this.wallLayers);
            RaycastHit2D hitLeft = Physics2D.Raycast(pos + (perp * halfAcross), dir, probeDist, this.wallLayers);
            RaycastHit2D hitRight = Physics2D.Raycast(pos - (perp * halfAcross), dir, probeDist, this.wallLayers);

            this.LastProbeHit = hitCenter.collider != null ? hitCenter
                : hitLeft.collider != null ? hitLeft : hitRight;

#if UNITY_EDITOR
            if (ShowForwardProbe)
            {
                Vector2 endCenter = pos + (dir * probeDist);
                Vector2 endLeft = pos + (perp * halfAcross) + (dir * probeDist);
                Vector2 endRight = pos - (perp * halfAcross) + (dir * probeDist);
                Debug.DrawLine(pos, endCenter, hitCenter.collider != null ? Color.red : Color.green);
                Debug.DrawLine(pos + (perp * halfAcross), endLeft, hitLeft.collider != null ? Color.red : Color.green);
                Debug.DrawLine(pos - (perp * halfAcross), endRight, hitRight.collider != null ? Color.red : Color.green);
            }
#endif

            return hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            // 被打锁定最近攻击者（2026-09-07 用户要求）：被打即把 Target 锁定为攻击者——
            // 原实现只切 Seek 不设 Target，Seek 见 Target=null 秒回 Wander，围殴下敌人
            // 反复重进 Wander 转向期（对准前不移动）= 原地站着不动。锁定期（被打起
            // ChangeTarget=5s）内被其他目标打不换目标——新攻击者需持续攻击到锁定到期
            // 才能抢走；被当前锁定目标再打会刷新锁定（持续攻击 = 持续保持目标）。
            if (attacker != null && attacker.CharacterDataLAB != null && attacker.CharacterDataLAB.Hp > 0f)
            {
                bool targetInvalid = this.Target == null
                    || this.Target.CharacterDataLAB == null
                    || this.Target.CharacterDataLAB.Hp <= 0f;
                // attacker == this.Target：被当前锁定目标再打无条件刷新锁定期——原条件缺此项，
                // 锁定期内被打连 if 都不进（不刷新），5 秒整一到"谁先打到谁抢走"：围殴中
                // 贴脸打着 A，B 一刀把 Target 抢走 → 敌人转身追 B = "正在被攻击突然走开"
                //（2026-09-08 用户报告，同 ASeekEnemy.ReduceHp）。补上后：A 持续攻击 = 持续保锁。
                if (targetInvalid || Time.time >= this.targetFocusEndTime || attacker == this.Target)
                {
                    if (this.Target != attacker)
                    {
                        AWorkerTask.LogProvider(
                            $"[EnemyDiag] {this.name} 被打锁定目标 → {attacker.name}（旧={this.Target?.name ?? "null"}）",
                            LogManager.LogLevelEnum.Debug);
                    }

                    this.Target = attacker;
                    this.targetFocusEndTime = Time.time + ChangeTarget;
                }
            }

            // 被打重置攻击持续时长（2026-09-08 用户报告"被打突然走开"，同 ASeekEnemy.ReduceHp）：
            // 下方换目标判定"AttackTime > ChangeTarget"的语义从"本状态累计 5s"变为
            // "距上次挨打 5s"——贴身互殴中每刀重置，切状态分支不会在持续交战中被触发。
            if (this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Attack)
            {
                ((CommonEnemyAttackState)this.Manager.CurrentState).ResetAttackTimeOnHit();
            }

            // 被打换目标需排除"打我的就是当前攻击目标"：否则单目标战斗中敌人每攻击几秒
            // 被反击一次就切 Seek → 又切回 Attack，武器反复销毁重建，拿起瞬间方向跳变
            // （同 ASeekEnemy.ReduceHp，见 bug-fixes.md 2026-08-16）。只有被其他目标打才换。
            // 已在 Chase 不重进（同 ASeekEnemy.ReduceHp 的 Move 守卫）：ChangeState 不查同状态，
            // Chase→Chase 照样走 OnExit+OnEnter（丢感知节流缓存、重置计时）——围殴下被打
            // ~1-3 刀/s，每次都重进 = 追击反复被打断，3 秒感知不回还会经 Seek 超时进
            // Wander 清 Target 重选目标 = "被攻击就会切换目标"（2026-09-08 用户报告）。
            // Target 由上方锁定分支维护（锁定期内换打不换锁），追击由 Chase 主循环朝
            // 最新 Target 直线追 + 感知失联 0.2s 才回 Seek，无需重进状态。
            if (this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Chase)
            {
                // 已在 Chase：不重进，不打断当前追击
            }
            else if (this.Manager.CurrentStateType != ACommonEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ACommonEnemyState.TypeEnum.Attack
                && ((CommonEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget
                && attacker != this.Target))
            {
                // 被打直接进 Chase 朝锁定目标追（原切 Seek 绕一圈感知再回 Chase，还有
                // 3 秒超时丢目标风险）；Chase.OnEnter 不清 Target，锁定目标即追击目标。
                this.Manager.ChangeState(ACommonEnemyState.TypeEnum.Chase);
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
            BugCheckResult bugResult = this.collisionBugDetector.CheckBug();

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