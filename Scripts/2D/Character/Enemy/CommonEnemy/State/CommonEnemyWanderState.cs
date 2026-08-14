namespace LAB2D.Character.Enemy.CommonEnemy.State
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 敌人漫游状态.
    /// </summary>
    public class CommonEnemyWanderState : ACommonEnemyState
    {
        private float recordTime = 9999.0f; // 记录时间
        private float rotationAngle; // 转向角度

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
            // 感知到周围有活着的玩家，进入追踪状态
            int count = Core.GameServices.PlayerCountProvider();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(Core.GameServices.PlayerGetProvider(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Chase);
                    this.Character.Target = Core.GameServices.PlayerGetProvider(i);

                    // 攻击目标选定事件（一次性）：Wander → Chase
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 攻击目标选定 target={this.Character.Target.name}",
                        LogManager.LogLevelEnum.Debug);
                    return;
                }
            }

            // 感知到周围有活着的Worker，进入追踪状态
            count = Core.GameServices.WorkerCountProvider();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(Core.GameServices.WorkerGetProvider(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Chase);
                    this.Character.Target = Core.GameServices.WorkerGetProvider(i);

                    // 攻击目标选定事件（一次性）：Wander → Chase
                    AWorkerTask.LogProvider(
                        $"[EnemyDiag] {this.Character.name} 攻击目标选定 target={this.Character.Target.name}",
                        LogManager.LogLevelEnum.Debug);
                    return;
                }
            }

            // 漫游：随机间隔和方向，过滤小角度变化避免"左右摇头"
            this.recordTime += this.Character.DeltaTime;
            float rotateInterval = Random.Range(12.0f, 18.0f); // 动态间隔
            if (this.recordTime >= rotateInterval)
            {
                float newAngle = Random.Range(0.0f, 360.0f);
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(this.rotationAngle, newAngle));
                if (angleDiff < 30.0f)
                {
                    newAngle = (newAngle + 180.0f) % 360.0f; // 确保显著转向
                }

                this.rotationAngle = newAngle;
                this.Character.MoveSpeed = Random.Range(4.5f, 6.0f);
                this.recordTime = 0.0f;
            }
        }

        /// <inheritdoc/>
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            Vector3 direction = new ((float)System.Math.Sin(this.rotationAngle), (float)System.Math.Cos(this.rotationAngle), 0);
            this.Character.RotateTo(direction);

            // 先转再移动
            float angle = Quaternion.Angle(this.Character.transform.rotation, Quaternion.FromToRotation(Vector3.up, direction));
            if (angle < 1.0f)
            {
                this.Character.MoveToForward();
            }
        }
    }
}