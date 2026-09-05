namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using Photon.Pun;
    using UnityEngine;

    /// <summary>
    /// 敌人漫游状态.
    /// </summary>
    public class CommonEnemyWanderState : ACommonEnemyState
    {
        private float recordTime = 9999.0f; // 记录时间
        private float rotationAngle; // 转向角度

        /// <summary>
        /// 换向候选尝试次数:预判挑选时最多随机试几个方向.
        /// </summary>
        private const int PickDirectionAttempts = 8;

        /// <summary>
        /// 每帧最多感知的候选目标数:SenseNearby 内含 Bresenham LOS + Physics2D.Raycast,
        /// 原实现每帧对全部玩家+Worker 全量感知,N=100 目标 × M=50 敌人即每帧数千次射线;
        /// 改为跨帧轮询(照 SeekEnemyMoveState 的 senseTargetIndex 模式),每帧每敌人只查 8 个.
        /// </summary>
        private const int SenseCandidatesPerFrame = 8;

        /// <summary>感知目标轮询索引:跨帧依次扫过全部玩家+Worker.</summary>
        private int senseTargetIndex;

        public CommonEnemyWanderState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.Character.Target = null;

            // 为了再一次进入会直接转动方向
            this.recordTime = 9999.0f;

            // 状态切换：进入漫游状态（高频漫游事件，节流 2s/条，见 bug-fixes.md 2026-08-15）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|WanderIn", 2f,
                $"[EnemyDiag] {this.Character.name} → Wander",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();

            // 状态切换：离开漫游状态（节流 2s/条）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|WanderOut", 2f,
                $"[EnemyDiag] {this.Character.name} ← Wander",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            // 感知降频：跨帧轮询玩家+Worker（玩家/Worker 数运行中会变，取模防越界）。
            // 命中则与原逻辑一致：切 Chase + 赋 Target + 一次性日志 + return。
            int playerCount = Core.GameServices.PlayerCountProvider();
            int workerCount = Core.GameServices.WorkerCountProvider();
            int total = playerCount + workerCount;
            for (int n = 0; total > 0 && n < SenseCandidatesPerFrame; n++)
            {
                int idx = this.senseTargetIndex % total;
                this.senseTargetIndex = (this.senseTargetIndex + 1) % total;
                Character target = idx < playerCount
                    ? Core.GameServices.PlayerGetProvider(idx)
                    : Core.GameServices.WorkerGetProvider(idx - playerCount);
                if (this.Character.SenseNearby(target.transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Chase);
                    this.Character.Target = target;

                    // 攻击目标选定事件（一次性）：Wander → Chase
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 攻击目标选定 target={this.Character.Target.name}",
                        LogManager.LogLevelEnum.Debug);
                    return;
                }
            }

            // 漫游：随机间隔换向（预判挑选避墙方向，见 PickNewDirection）
            this.recordTime += this.Character.DeltaTime;
            float rotateInterval = Random.Range(12.0f, 18.0f); // 动态间隔
            if (this.recordTime >= rotateInterval)
            {
                this.PickNewDirection();
            }
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Vector3 direction = new ((float)System.Math.Sin(this.rotationAngle), (float)System.Math.Cos(this.rotationAngle), 0);
            this.Character.RotateTo(direction);

            // 移动前探测：目标朝向上矩形两边及中线短距离内有墙 → 立即换向（转向期间本就不移动，
            // 新方向下帧继续检测）。撞墙不再只依赖 OnCollisionStay2D 事后熔断。
            if (this.Character.IsForwardBlocked(direction))
            {
                // 先保存命中详情（PickNewDirection 的候选预判会覆盖 LastProbeHit），再换向
                Collider2D hitCol = this.Character.LastProbeHit.collider;
                string hitDesc = hitCol != null
                    ? $"{hitCol.name}@{LayerMask.LayerToName(hitCol.gameObject.layer)}"
                    : "无";
                this.PickNewDirection();
                AWorkerTask.LogProviderThrottled(
                    $"{this.Character.name}|WanderReroute", 2f,
                    // 惰性求值：受阻检测每帧运行，hitDesc 须在 PickNewDirection 前取（覆盖 LastProbeHit）
                    // 故保持立即求值；位置与角度的插值串被节流时不再构造
                    () =>
                    {
                        Vector3 pos = this.Character.transform.position;
                        return $"[EnemyDiag] {this.Character.name} 前方受阻换向 hit={hitDesc} pos=({pos.x:F1},{pos.y:F1}) newAngle={this.rotationAngle:F0}";
                    },
                    LogManager.LogLevelEnum.Debug);
                return;
            }

            // 先转再移动
            float angle = Quaternion.Angle(this.Character.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction));
            if (angle < 1.0f)
            {
                this.Character.MoveToForward();
            }
        }

        /// <summary>
        /// 挑选新漫游方向：随机候选角（过滤小角度变化避免"左右摇头"），
        /// 用矩形前方射线探测预判，选第一个不通墙的方向；全部受阻则用最后一个候选（死角兜底，
        /// 交由 OnFixedUpdate 移动前探测继续换向）。
        /// </summary>
        private void PickNewDirection()
        {
            float lastAngle = this.rotationAngle;
            for (int i = 0; i < PickDirectionAttempts; i++)
            {
                float newAngle = Random.Range(0.0f, 360.0f);
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(this.rotationAngle, newAngle));
                if (angleDiff < 30.0f)
                {
                    newAngle = (newAngle + 180.0f) % 360.0f; // 确保显著转向
                }

                Vector3 dir = new ((float)System.Math.Sin(newAngle), (float)System.Math.Cos(newAngle), 0);
                if (!this.Character.IsForwardBlocked(dir))
                {
                    this.ApplyNewDirection(newAngle);
                    return;
                }

                lastAngle = newAngle;
            }

            this.ApplyNewDirection(lastAngle);
        }

        /// <summary>
        /// 应用新方向：记录角度、随机移速并重置换向计时.
        /// </summary>
        private void ApplyNewDirection(float angle)
        {
            this.rotationAngle = angle;
            this.Character.MoveSpeed = Random.Range(4.5f, 6.0f);
            this.recordTime = 0.0f;
        }
    }
}