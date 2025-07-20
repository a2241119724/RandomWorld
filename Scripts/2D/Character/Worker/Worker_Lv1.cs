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
            this.Manager.addStates(WorkerStateType.Move, new WorkerMoveState(this));
            this.Manager.addStates(WorkerStateType.Work, new WorkerWorkState(this));
            this.Manager.addStates(WorkerStateType.Seek, new WorkerSeekState(this));
            this.Manager.addStates(WorkerStateType.Hungry, new WorkerHungryState(this));
            this.Manager.addStates(WorkerStateType.Attack, new WorkerAttackState(this));
            this.Manager.addStates(WorkerStateType.Escape, new WorkerEscapeState(this));

            // 初始化状态
            this.Manager.changeState(WorkerStateType.Seek);
        }
    }
}
