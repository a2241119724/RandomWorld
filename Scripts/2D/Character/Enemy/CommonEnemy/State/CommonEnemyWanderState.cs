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

            // LogManager.Instance.log("WanderState", LogManager.LogLevel.Info);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
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