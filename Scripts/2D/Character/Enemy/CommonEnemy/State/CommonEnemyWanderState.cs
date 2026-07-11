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
            int count = PlayerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(PlayerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Chase);
                    this.Character.Target = PlayerManager.Instance.Get(i);
                    return;
                }
            }

            // 感知到周围有活着的Worker，进入追踪状态
            count = WorkerManager.Instance.Count();
            for (int i = 0; i < count; i++)
            {
                if (this.Character.SenseNearby(WorkerManager.Instance.Get(i).transform))
                {
                    this.Character.Manager.ChangeState(TypeEnum.Chase);
                    this.Character.Target = WorkerManager.Instance.Get(i);
                    return;
                }
            }

            // 漫游
            this.recordTime += Time.deltaTime;
            if (this.recordTime >= this.Character.RotateInterval)
            {
                this.rotationAngle = Random.Range(0.0f, 360.0f);
                this.Character.MoveSpeed = Random.Range(3.0f, 4.0f);
                this.recordTime = 0.0f;
            }

            Vector3 direction = new (Mathf.Sin(this.rotationAngle), Mathf.Cos(this.rotationAngle), 0);
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