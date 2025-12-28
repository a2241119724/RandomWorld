namespace LAB2D
{
    /// <summary>
    /// Worker
    /// </summary>
    public class Worker_Lv1 : AWorker
    {
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();

            // 添加状态
            this.Manager.AddState(AWorkerState.TypeEnum.Move, new WorkerMoveState(this));
            this.Manager.AddState(AWorkerState.TypeEnum.Work, new WorkerWorkState(this));
            this.Manager.AddState(AWorkerState.TypeEnum.Seek, new WorkerSeekState(this));
            this.Manager.AddState(AWorkerState.TypeEnum.Attack, new WorkerAttackState(this));
            this.Manager.AddState(AWorkerState.TypeEnum.Escape, new WorkerEscapeState(this));
            this.Manager.AddState(AWorkerState.TypeEnum.Dead, new WorkerDeadState(this));

            // 初始化状态
            this.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
        }
    }
}
