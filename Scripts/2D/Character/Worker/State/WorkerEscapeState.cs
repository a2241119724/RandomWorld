namespace LAB2D
{
    using UnityEngine;
    using static LAB2D.AWorker;

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
            this.Character.WorkerStateText.text = this.preString;
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
            this.recordTime += Time.deltaTime;
            if (this.recordTime >= WorkerTaskTimeConfig.EscapeSeconds)
            {
                this.Character.Manager.ChangeState(TypeEnum.Seek);
            }

            this.Character.Seek.LineRenderer.positionCount = 0;
            this.Character.transform.Translate(this.Character.MoveSpeed * Time.deltaTime * Vector3.up, Space.World);
        }
    }
}
