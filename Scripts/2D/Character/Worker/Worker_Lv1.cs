namespace LAB2D
{
    /// <summary>
    /// Worker
    /// </summary>
    public class Worker_Lv1 : Worker
    {
        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();

            // 添加状态
            this.Manager.AddState(WorkerStateType.Move, new WorkerMoveState(this));
            this.Manager.AddState(WorkerStateType.Work, new WorkerWorkState(this));
            this.Manager.AddState(WorkerStateType.Seek, new WorkerSeekState(this));
            this.Manager.AddState(WorkerStateType.Eat, new WorkerHungryState(this));
            this.Manager.AddState(WorkerStateType.Attack, new WorkerAttackState(this));
            this.Manager.AddState(WorkerStateType.Escape, new WorkerEscapeState(this));

            // 初始化状态
            this.Manager.ChangeState(WorkerStateType.Seek);
        }
    }
}
