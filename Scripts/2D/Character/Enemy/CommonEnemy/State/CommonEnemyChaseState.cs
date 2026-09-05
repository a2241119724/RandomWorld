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
            if (this.Character.Target == null)
            {
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
            else
            {
                // 如果敌人感知范围内没有玩家，进入搜索状态
                this.senseTimer = 0f; // 失联换目标语义：进 Seek 前清计时，重进 Chase 立即完整感知
                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }
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