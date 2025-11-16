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
            this.Manager.AddState(WorkerState.TypeEnum.Move, new WorkerMoveState(this));
            this.Manager.AddState(WorkerState.TypeEnum.Work, new WorkerWorkState(this));
            this.Manager.AddState(WorkerState.TypeEnum.Seek, new WorkerSeekState(this));
            this.Manager.AddState(WorkerState.TypeEnum.Eat, new WorkerHungryState(this));
            this.Manager.AddState(WorkerState.TypeEnum.Attack, new WorkerAttackState(this));
            this.Manager.AddState(WorkerState.TypeEnum.Escape, new WorkerEscapeState(this));

            // 初始化状态
            this.Manager.ChangeState(WorkerState.TypeEnum.Seek);
        }
    }
}
