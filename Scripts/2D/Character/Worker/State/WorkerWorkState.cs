namespace LAB2D
{
    /// <summary>
    /// Worker工作状态
    /// </summary>
    public class WorkerWorkState : AWorkerState
    {
        private bool waitOneFrame; // 等待一帧

        public WorkerWorkState(AWorker worker)
            : base(worker)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.waitOneFrame = false;
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
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
                this.Character.Manager.ChangeState(TypeEnum.Seek);
                return;
            }

            base.OnUpdate();
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
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