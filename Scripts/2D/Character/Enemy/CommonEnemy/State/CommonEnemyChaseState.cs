namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 敌人追击状态.
    /// </summary>
    public class CommonEnemyChaseState : ACommonEnemyState
    {
        /// <summary>
        /// 感知节流间隔（秒）。SenseNearby 含 Bresenham 网格 LOS（逐格 IsCanReach）+
        /// 物理射线，追击态原本每帧全量执行（100 敌人 = 每秒 6000 次逐格扫描）；
        /// 缓存感知结果 0.2s（失联切 Seek 最多延迟 0.2s，远小于 SeekEnemy 侧 2s 容忍）。
        /// 攻击距离判定（一次 Distance）不节流，保证进攻击及时。
        /// </summary>
        private const float SenseInterval = 0.2f;

        private float senseTimer;
        private bool lastSenseResult;

        public CommonEnemyChaseState(ACommonEnemy character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.senseTimer = 0f; // 立即做一次完整感知
            this.lastSenseResult = true;

            // 状态切换：进入追击状态（节流 2s/条，见 bug-fixes.md 2026-08-15）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|ChaseIn", 2f,
                $"[EnemyDiag] {this.Character.name} → Chase target={this.Character.Target?.name}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();

            // 状态切换：离开追击状态（节流 2s/条）
            AWorkerTask.LogProviderThrottled(
                $"{this.Character.name}|ChaseOut", 2f,
                $"[EnemyDiag] {this.Character.name} ← Chase",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            // 目标死亡/销毁才放弃（清引用防追尸体）
            if (this.Character.Target == null
                || this.Character.Target.CharacterDataLAB == null
                || this.Character.Target.CharacterDataLAB.Hp <= 0f)
            {
                this.Character.Target = null;
                this.Character.Manager.ChangeState(TypeEnum.Wander);
                return;
            }

            // 感知节流（见 SenseInterval 注释）：间隔内复用上次结果，间隔到才做完整 SenseNearby
            this.senseTimer += this.Character.DeltaTime;
            if (this.senseTimer >= SenseInterval)
            {
                this.senseTimer = 0f;
                this.lastSenseResult = this.Character.SenseNearby(this.Character.Target.transform);
            }

            if (this.lastSenseResult)
            {
                ACommonEnemy.EnemyData enemyData = this.Character.CharacterDataLAB as ACommonEnemy.EnemyData;

                // 如果玩家与敌人的距离小于敌人的攻击距离，那么进入攻击状态
                if (Vector3.Distance(this.Character.Target.transform.position, this.Character.transform.position) <= enemyData.AttackRange)
                {
                    this.Character.Manager.ChangeState(TypeEnum.Attack);
                    return;
                }
            }

            // 感知失联不切状态、不丢目标（对齐 SeekEnemy.Move 语义，2026-09-08 用户报告
            // "被攻击就会切换目标"残留）：原实现失联 0.2s → Seek → 3 秒超时 → Wander 清
            // Target 重选——Worker 站 4 格外（Sound=4 边界外/扇形外）打它时链路必触发，
            // 观感"我打它，它转头去打别人"。追击移动不依赖感知（OnFixedUpdate 朝 Target
            // 直线移动），失联唯一作用是不进 Attack（看不见不打）；放弃目标只走目标死亡
            // 或卡死熔断（OnCollisionStay2D），换锁唯一正当途径是 ReduceHp 的"打我"。
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (this.Character.Target == null) return;

            this.Character.RotateTo(this.Character.Target.transform.position - this.Character.transform.position);
            this.Character.MoveToForward();
        }
    }
}