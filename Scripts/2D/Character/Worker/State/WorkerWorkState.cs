namespace LAB2D
{
    /// <summary>
    /// 工作者工作状态
    /// </summary>
    public class WorkerWorkState : WorkerState
    {
        public WorkerWorkState(Worker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            if (this.Character.Manager.Task == null)
            {
                return;
            }

            this.Character.WorkerState.text = this.preString +
                $"Target: {this.Character.Manager.Task.TargetMap.x},{this.Character.Manager.Task.TargetMap.y}";
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
            if (this.Character.Manager.Task == null)
            {
                return;
            }

            bool isComplete = this.Character.Manager.Task.execute(this.Character);
            if (!isComplete)
            {
                return;
            }

            // 完成任务
            this.Character.Manager.Task = null;
            this.Character.Manager.changeState(WorkerStateType.Seek);
        }
    }
}