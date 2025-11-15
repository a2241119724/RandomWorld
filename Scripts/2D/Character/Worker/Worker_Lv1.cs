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
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Move, new WorkerMoveState(this));
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Work, new WorkerWorkState(this));
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Seek, new WorkerSeekState(this));
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Eat, new WorkerHungryState(this));
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Attack, new WorkerAttackState(this));
            this.Manager.AddState(WorkerState.WorkerStateTypeEnum.Escape, new WorkerEscapeState(this));

            // 初始化状态
            this.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
        }
    }
}
