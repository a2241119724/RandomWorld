namespace LAB2D
{
    /// <summary>
    /// Worker攻击状态
    /// </summary>
    public class WorkerAttackState : WorkerState
    {
        public WorkerAttackState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
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
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Weapon == null)
            {
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Escape);
                return;
            }

            // 拿出武器
            // 攻击
        }
    }
}
