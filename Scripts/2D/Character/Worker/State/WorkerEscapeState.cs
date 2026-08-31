namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using UnityEngine;
    using static LAB2D.Character.Worker.AWorker;

    /// <summary>
    /// Worker逃跑状态
    /// </summary>
    public class WorkerEscapeState : AWorkerState
    {
        private float recordTime = 0.0f;

        public WorkerEscapeState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.recordTime = 0.0f;
            // 逃跑用 transform.Translate 直移（见 OnUpdate），必须先停止移动服务层的意图驱动，
            // 否则 Locomotion.TickFixed 与本状态直移双驱动互相打架（见计划风险 3）。
            this.Character.Locomotion.Stop();
            this.Character.WorkerStateText.text = this.preString;
            // 逃跑入口诊断（事件点）：记录进入逃跑状态（通常由攻击无武器或受到威胁触发）。
            AWorkerTask.LogProvider(
                $"[StateDiag] {this.Character.name} 进入逃跑状态",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();
            this.recordTime += this.Character.DeltaTime;
            if (this.recordTime >= WorkerTaskTimeConfig.EscapeSeconds)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }

            this.Character.Seek.LineRenderer.positionCount = 0;
            this.Character.transform.Translate(this.Character.MoveSpeed * this.Character.DeltaTime * Vector3.up, Space.World);
        }
    }
}
