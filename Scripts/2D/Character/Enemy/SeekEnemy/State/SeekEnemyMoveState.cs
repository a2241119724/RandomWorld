namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    using LAB2D.Domain.Time;
    using UnityEngine;

    public class SeekEnemyMoveState : ASeekEnemyState
    {
        private float recordTime = 0.0f;

        /// <summary>
        /// 感知目标轮询索引 — 每帧只检查一个目标（跨帧轮询所有玩家+Worker）
        /// </summary>
        private int senseTargetIndex = 0;

        /// <summary>
        /// 感知帧相位（实例 id 低 6 位）：感知周期 30 帧（0.5s），全局相位会让全部
        /// SeekEnemy 同帧集中感知（尖峰）；按实例错开相位摊到 30 帧窗口内不同帧。
        /// </summary>
        private readonly int sensePhase;

        private bool isTargetReached = false;

        // 卡死熔断：同一目标连续卡死（Sliding/Stuck 结算）次数上限，超过则放弃当前目标换新。
        // 镜像 WorkerMoveState 的 Sliding 熔断（Worker 侧累计 4 次→HandleMovementStuck）。
        private const int MaxStuckStreak = 4;
        private Vector3Int stuckTarget;
        private int stuckStreak;

        /// <summary>
        /// 到达后的漫游休息秒数（2s 基础 + 随机抖动）：抖动打散多个敌人的重寻路提交相位，
        /// 避免同波敌人（同帧生成/同帧脱离战斗）固定间隔导致重寻路同帧共振提交。
        /// </summary>
        private readonly float roamRestSeconds = 2f + UnityEngine.Random.Range(0f, 1.5f);

        /// <summary>攻城触发阈值：夜间连续这么多秒未感知到任何角色 → 攻向最近阵营核心（P4）。</summary>
        private const float SiegeDelaySeconds = 8f;

        /// <summary>未感知角色累计秒数（感知命中清零；达 SiegeDelaySeconds 且夜间有核心 → Siege）。</summary>
        private float noSenseSeconds;

        /// <summary>
        /// 追击重寻路间隔（秒）：感知命中但目标超出攻击距离时，朝目标邻格周期刷新路径
        ///（目标在移动，单次寻路会走空）。检测范围（Sight=10/Sound=4）只负责锁目标，
        /// 不再作为开火门槛——修"实际攻击范围与检测范围不一致"（2026-09-08 用户报告：
        /// 敌人视野边缘看见就站桩空挥，武器实际打不到）。
        /// </summary>
        private const float PursuitRepathInterval = 1f;

        /// <summary>追击重寻路计时（每帧累加，达 PursuitRepathInterval 朝目标邻格重寻路）。</summary>
        private float pursuitRepathTimer;

        /// <summary>上次追击寻路的目标格：同格不重复提交 A*（现有路径仍指向它）。</summary>
        private Vector3Int lastPursuitMap;

        public SeekEnemyMoveState(ASeekEnemy character)
        : base(character)
        {
            // 补码低 6 位对负 id 也非负（30 帧周期 64 个相位足够错开）
            this.sensePhase = character.GetInstanceID() & 63;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            this.senseTargetIndex = 0;

            // 状态切换：进入移动状态（高频漫游事件，节流 2s/条，见 bug-fixes.md 2026-08-15）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|MoveIn", 2f,
                $"[EnemyDiag] {this.Character.name} → Move",
                LogManager.LogLevelEnum.Debug);

            // 追击进入（被打锁定/脱战重入）：Target 非空且存活 → 朝目标附近可走格重新寻路。
            // 不寻路则无活动路径，MoveByPath 判"已到达"原地休息 2s 回 Seek——
            // 被打后全程站桩挨打的根因（2026-09-07 用户报告）。寻的是目标邻格而非
            // 目标本格：A* 不考虑目标占格排斥，直寻本格会走到与目标重合（2026-09-08
            // 用户报告"靠近直到重合然后一直摆头"）。
            if (this.Character.Target != null
                && this.Character.Target.CharacterDataLAB != null
                && this.Character.Target.CharacterDataLAB.Hp > 0f)
            {
                this.RepathPursuit();
            }

            // 重置卡死熔断计数（新目标/新进入移动 → 从头计数）
            this.stuckStreak = 0;
            this.stuckTarget = default;
            this.lastPursuitMap = default;
            this.pursuitRepathTimer = 0f;
        }

        public override void OnExit()
        {
            base.OnExit();
            this.Character.Seek.StopMove();

            // 状态切换：离开移动状态（节流 2s/条）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|MoveOut", 2f,
                $"[EnemyDiag] {this.Character.name} ← Move",
                LogManager.LogLevelEnum.Debug);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 没有武器, 不进入攻击状态
            // 感知检查降频：每 30 帧检查一次 ≈0.5s（按实例相位错开，见 sensePhase 注释），
            // 每次只检查一个目标（轮询，与 Worker 侧 0.5s 索敌对齐）
            if (this.Character.CharacterDataLAB.Weapon != null
                && (UnityEngine.Time.frameCount + this.sensePhase) % 30 == 0)
            {
                // 锁定目标优先快查：Target 非空且存活先感知它——追击/被打锁定场景下
                // 等全目标轮询圈（100 Worker ≈50s）轮到攻击者太久，围殴者贴脸却反击不了
                bool targetAlive = this.Character.Target != null
                    && this.Character.Target.CharacterDataLAB != null
                    && this.Character.Target.CharacterDataLAB.Hp > 0f;
                if (targetAlive && this.Character.SenseNearby(this.Character.Target.transform))
                {
                    this.noSenseSeconds = 0f;
                    if (this.TryEnterAttack(this.Character.Target.transform))
                    {
                        return;
                    }

                    // 仍在攻击距离外：锁定目标留在 Move 追击贴近（下方节流重寻路）
                }

                int playerCount = Core.GameServices.PlayerCountProvider();
                int workerCount = Core.GameServices.WorkerCountProvider();
                int totalTargets = playerCount + workerCount;

                if (totalTargets > 0)
                {
                    // 轮询到下一个目标索引
                    this.senseTargetIndex = (this.senseTargetIndex + 1) % totalTargets;

                    UnityEngine.Transform target = null;
                    if (this.senseTargetIndex < playerCount)
                    {
                        target = Core.GameServices.PlayerGetProvider(this.senseTargetIndex).transform;
                    }
                    else
                    {
                        target = Core.GameServices.WorkerGetProvider(this.senseTargetIndex - playerCount).transform;
                    }

                    if (target != null && this.Character.SenseNearby(target))
                    {
                        this.noSenseSeconds = 0f;
                        if (!this.Character.IsTargetFocusActive)
                        {
                            // 锁定不生效（无目标/已死/锁过期）才允许轮询覆写 Target——
                            // 锁定期内被旁观者抢走 = "被攻击就换目标"残留（2026-09-08，
                            // 换锁正当途径只有 ReduceHp 的"打我"）。先赋 Target 再切攻击状态：
                            // ChangeState 同步触发 OnEnter，若 Target 在 ChangeState 之后才赋值，
                            // OnEnter 初始朝向读不到目标，武器保持 prefab 默认朝上（z=0），
                            // 同帧 AttackRange 跟随武器朝空方向闪一下再拐回（见 bug-fixes.md 2026-08-16）。
                            this.Character.Target = target.GetComponent<LAB2D.Character.Character>();
                            if (this.TryEnterAttack(target))
                            {
                                return;
                            }
                        }

                        // 超出攻击距离/锁定中追原目标：留在 Move 追击贴近（下方节流重寻路）
                    }
                }
            }

            // 攻城判定（P4→W3 修正）：连续 SiegeDelaySeconds 秒没感知到任何角色 → 转 Siege。
            // 不分昼夜、不要求有阵营核心——目标选择交给 SiegeState.OnEnter（最近核心优先，
            // 无核心拆最近房间墙格，全无回 Seek）。此前入口要求 Night+HasPlacedCore，
            // 无核心的局 Enemy 永远进不了 Siege（夜袭拆家看不到的根因）。
            this.noSenseSeconds += this.Character.DeltaTime;
            if (this.noSenseSeconds >= SiegeDelaySeconds)
            {
                this.noSenseSeconds = 0f;
                this.Character.Manager.ChangeState(TypeEnum.Siege);
                return;
            }

            // 设置视觉角度
            this.Character.SightRange.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.Character.Direction);

            // 追击贴近：锁定存活目标后周期朝目标邻格重寻路（目标在移动，见 PursuitRepathInterval 注释）；
            // 进入攻击距离由上方感知分支的 TryEnterAttack 切换
            this.pursuitRepathTimer += this.Character.DeltaTime;
            if (this.pursuitRepathTimer >= PursuitRepathInterval
                && this.Character.Target != null
                && this.Character.Target.CharacterDataLAB != null
                && this.Character.Target.CharacterDataLAB.Hp > 0f)
            {
                this.RepathPursuit();
            }

            if (this.isTargetReached)
            {
                // 追击中到达目标格：目标多已移走，立即试切攻击/朝新位置重贴近——
                // 不休息不回 Seek（漫游休息只属于无目标场景）
                if (this.Character.Target != null
                    && this.Character.Target.CharacterDataLAB != null
                    && this.Character.Target.CharacterDataLAB.Hp > 0f)
                {
                    if (!this.TryEnterAttack(this.Character.Target.transform))
                    {
                        this.RepathPursuit();
                    }

                    return;
                }

                this.recordTime += this.Character.DeltaTime;

                // 休息 2s + 随机抖动（打散重寻路提交相位，见 roamRestSeconds 注释）
                if (this.recordTime < this.roamRestSeconds)
                {
                    return;
                }

                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }
        }

        /// <summary>
        /// 感知命中后按距离分流（修"实际攻击范围与检测范围不一致"2026-09-08）：
        /// 目标进入攻击距离（EnemyData.AttackRange，红色扇形视觉同源）→ 切 Attack 返回 true；
        /// 仍在攻击距离外 → 返回 false，调用方留在 Move 追击贴近——检测范围
        ///（SightRange=10/SoundRange=4）只负责锁目标，不再作为开火门槛
        ///（此前视野边缘看见就进攻击站桩空挥，武器实际打不到那么远）。
        /// </summary>
        private bool TryEnterAttack(Transform target)
        {
            AEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as AEnemy.EnemyData;
            // 门槛 = min(EnemyData.AttackRange, 武器实际射程 AttackReach)：红色扇形上限与
            // 特效命中范围双重约束——剑光原地闪真实命中 ~1 格（AttackReach=1.5），单用
            // AttackRange=4 拿剑敌人贴到 4 格仍空挥（2026-09-08 第二轮）。进攻门槛须与
            // SeekEnemyAttackState 脱攻门槛（本门槛×1.2）构成滞回，防 Attack↔Move 抖动。
            float attackGate = Mathf.Min(
                enemyData.AttackRange,
                this.Character.CharacterDataLAB.Weapon?.AttackReach ?? enemyData.AttackRange);
            float dist = Vector3.Distance(target.position, this.Character.transform.position);
            if (dist > attackGate)
            {
                return false;
            }

            // 攻击目标选定事件（一次性）
            AWorkerTask.LogProvider(
                $"[EnemyDiag] {this.Character.name} 目标进入攻击距离 dist={dist:F1} ≤ {attackGate:F1} target={target.name} → Attack",
                LogManager.LogLevelEnum.Debug);
            this.Character.Manager.ChangeState(TypeEnum.Attack);
            return true;
        }

        /// <summary>
        /// 朝目标邻格重寻路（追击贴近）并重置节流计时；目标格未变时跳过
        ///（现有路径仍指向它，重复提交只浪费 A*）。寻邻格而非目标本格：
        /// A* 不考虑目标占格排斥，直寻本格会走到与目标重合（2026-09-08 用户报告）。
        /// </summary>
        private void RepathPursuit()
        {
            this.pursuitRepathTimer = 0f;
            Vector3Int pursuitMap = AWorkerTask.GenCanReachPosProvider(
                AWorkerTask.TileMapWorldToMapProvider(this.Character.Target.transform.position));
            if (pursuitMap == this.lastPursuitMap)
            {
                return;
            }

            this.lastPursuitMap = pursuitMap;
            this.Character.Seek.Seek(pursuitMap);
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            this.isTargetReached = this.Character.Seek.MoveByPath();

            if (this.isTargetReached)
            {
                return;
            }

            BugCheckResult stuckResult = this.Character.Seek.LastStuckResult;
            if (stuckResult == BugCheckResult.None)
            {
                return; // 有实质进展
            }

            // 目标变化 → 重置熔断计数（镜像 WorkerMoveState 的 lastSlidingTarget 逻辑）
            if (this.stuckTarget != this.Character.Seek.TargetMap)
            {
                this.stuckTarget = this.Character.Seek.TargetMap;
                this.stuckStreak = 0;
            }

            if (++this.stuckStreak >= MaxStuckStreak)
            {
                // 熔断：连续卡死 → 放弃当前漫游/追击目标，回 Seek 状态换新目标。
                // 否则"卡死→重新寻路→再卡死"无限循环（原实现漫游时 HandleMovementStuck 为空操作，
                // [StuckDiag] 每秒刷屏 ratio=0.00 永不消散，见 bug-fixes.md 2026-08-13）。
                this.stuckStreak = 0;
                this.Character.AbandonMovementStuck();
                return;
            }

            // 位移不足/卡死 → 停止当前寻路，以当前目标重新寻路（与旧 OnCollisionStay2D 一致）
            this.Character.HandleMovementStuck();
        }
    }
}
