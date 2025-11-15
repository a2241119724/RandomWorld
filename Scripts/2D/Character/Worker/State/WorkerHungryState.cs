namespace LAB2D
{
    using static LAB2D.AWorker;

    /// <summary>
    /// 仓库没有吃的,就一直在该状态,不能做其他事情
    /// </summary>
    public class WorkerHungryState : WorkerState
    {
        public WorkerHungryState(AWorker worker)
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

            // 如果接到了饥饿任务，则去吃饭
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (workerData.Task != null
                && workerData.Task.TaskType.Equals(WorkerTask.WorkerTaskTypeEnum.Eat))
            {
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Seek);
            }
        }
    }
}
