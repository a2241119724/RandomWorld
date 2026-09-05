namespace LAB2D.Character.Enemy.SeekEnemy.State
{
    using LAB2D;
    using UnityEngine;

    public class SeekEnemyMoveState : ASeekEnemyState
    {
        private float recordTime = 0.0f;

        /// <summary>
        /// 感知目标轮询索引 — 每帧只检查一个目标（跨帧轮询所有玩家+Worker）
        /// </summary>
        private int senseTargetIndex = 0;

        /// <summary>
        /// 感知帧相位（实例 id 低 2 位）：frameCount % 4 是全局相位，全部 SeekEnemy 会在
        /// 第 4n 帧同帧集中感知（尖峰）；按实例错开后每敌人仍每 4 帧感知一次，仅相位摊开。
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

        public SeekEnemyMoveState(ASeekEnemy character)
        : base(character)
        {
            // 补码低 2 位对负 id 也非负
            this.sensePhase = character.GetInstanceID() & 3;
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

            // 重置卡死熔断计数（新目标/新进入移动 → 从头计数）
            this.stuckStreak = 0;
            this.stuckTarget = default;
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
            // 感知检查降频：每4帧检查一次（按实例相位错开，见 sensePhase 注释），每帧只检查一个目标（轮询）
            if (this.Character.CharacterDataLAB.Weapon != null
                && ((UnityEngine.Time.frameCount + this.sensePhase) & 3) == 0)
            {
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
                        // 先赋 Target 再切攻击状态：ChangeState 同步触发 OnEnter，若 Target
                        // 在 ChangeState 之后才赋值，OnEnter 初始朝向读不到目标，武器保持
                        // prefab 默认朝上（z=0），同帧 AttackRange 跟随武器朝空方向闪一下
                        // 再拐回——即"攻击时拐向没有角色的地方攻击一次"（见 bug-fixes.md 2026-08-16）。
                        this.Character.Target = target.GetComponent<LAB2D.Character.Character>();
                        this.Character.Manager.ChangeState(TypeEnum.Attack);

                        // 攻击目标选定事件（一次性）
                        AWorkerTask.LogProvider(
                            $"[EnemyDiag] {this.Character.name} 攻击目标选定 target={target.name}",
                            LogManager.LogLevelEnum.Debug);
                        return;
                    }
                }
            }

            // 设置视觉角度
            this.Character.SightRange.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.Character.Direction);

            if (this.isTargetReached)
            {
                this.recordTime += this.Character.DeltaTime;

                // 休息 2s + 随机抖动（打散重寻路提交相位，见 roamRestSeconds 注释）
                if (this.recordTime < this.roamRestSeconds)
                {
                    return;
                }

                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }
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
