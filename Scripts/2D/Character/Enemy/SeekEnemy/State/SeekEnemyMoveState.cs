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

        public SeekEnemyMoveState(ASeekEnemy character)
        : base(character)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            this.senseTargetIndex = 0;
        }

        public override void OnExit()
        {
            base.OnExit();
            this.Character.Seek.StopMove();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // 没有武器, 不进入攻击状态
            // 感知检查降频：每4帧检查一次，每帧只检查一个目标（轮询）
            if (this.Character.CharacterDataLAB.Weapon != null
                && UnityEngine.Time.frameCount % 4 == 0)
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
                        this.Character.Manager.ChangeState(TypeEnum.Attack);
                        this.Character.Target = target.GetComponent<LAB2D.Character.Character>();
                        return;
                    }
                }
            }

            // 设置视觉角度
            this.Character.SightRange.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.Character.Direction);
            bool isTarget = this.Character.Seek.MoveByPath();
            if (isTarget)
            {
                this.recordTime += this.Character.DeltaTime;

                // 休息2秒
                if (this.recordTime < 2)
                {
                    return;
                }

                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }
        }
    }
}
