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
            Worker.WorkerData workerData = this.Character.CharacterDataLAB as Worker.WorkerData;
            if (workerData.Task == null)
            {
                return;
            }

            this.Character.WorkerStateText.text = this.preString +
                $"Target: {workerData.Task.TargetMap.X},{workerData.Task.TargetMap.Y}";
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            if (this.waitOneFrame)
            {
                // 等待一帧后再进入寻路状态,先去接任务
                this.Character.Manager.ChangeState(WorkerStateTypeEnum.Seek);
                return;
            }

            base.OnUpdate();
            Worker.WorkerData workerData = this.Character.CharacterDataLAB as Worker.WorkerData;
            if (workerData.Task == null)
            {
                return;
            }

            bool isComplete = workerData.Task.Execute(this.Character);
            if (isComplete)
            {
                // 完成任务
                workerData.Task = null;
                this.waitOneFrame = true;
            }
        }
    }
}