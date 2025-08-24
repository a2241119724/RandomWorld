namespace LAB2D
{
    using static LAB2D.Worker;

    /// <summary>
    /// Worker工作状态
    /// </summary>
    public class WorkerWorkState : WorkerState
    {
        private bool waitOneFrame; // 等待一帧

        public WorkerWorkState(Worker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.waitOneFrame = false;
            WorkerData workerData = this.Character.CharacterDataLAB as WorkerData;
            if (workerData.Manager.Task == null)
            {
                return;
            }

            this.Character.WorkerStateText.text = this.preString +
                $"Target: {workerData.Manager.Task.TargetMap.x},{workerData.Manager.Task.TargetMap.y}";
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            WorkerData workerData = this.Character.CharacterDataLAB as WorkerData;
            if (this.waitOneFrame)
            {
                // 等待一帧后再进入寻路状态,先去接任务
                workerData.Manager.ChangeState(WorkerStateTypeEnum.Seek);
                return;
            }

            base.OnUpdate();
            if (workerData.Manager.Task == null)
            {
                return;
            }

            bool isComplete = workerData.Manager.Task.Execute(this.Character);
            if (isComplete)
            {
                // 完成任务
                workerData.Manager.Task = null;
                this.waitOneFrame = true;
            }
        }
    }
}