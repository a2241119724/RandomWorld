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
            this.boxCollider = this.GetComponent<BoxCollider2D>();
            this.wallLayers = LayerMask.GetMask("Tile", "BuildTile");
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
            this.MoveSpeed = UnityEngine.Random.Range(1.5f, 2.0f);
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