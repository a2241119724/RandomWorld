namespace LAB2D
{
    /// <summary>
    /// Worker
    /// </summary>
    public class Worker_Lv1 : Worker
    {
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();

            // 添加状态
            Worker.WorkerData workerData = this.CharacterDataLAB as Worker.WorkerData;
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Move, new WorkerMoveState(this));
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Work, new WorkerWorkState(this));
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Seek, new WorkerSeekState(this));
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Eat, new WorkerHungryState(this));
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Attack, new WorkerAttackState(this));
            workerData.Manager.AddState(WorkerState.WorkerStateTypeEnum.Escape, new WorkerEscapeState(this));

            // 初始化状态
            workerData.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
        }
    }
}
