using static LAB2D.Worker;

namespace LAB2D
{
    /// <summary>
    /// Worker工作状态
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
            base.OnUpdate();
            WorkerData workerData = this.Character.CharacterDataLAB as WorkerData;
            if (workerData.Manager.Task == null)
            {
                return;
            }

            bool isComplete = workerData.Manager.Task.Execute(this.Character);
            if (!isComplete)
            {
                return;
            }

            // 完成任务
            workerData.Manager.Task = null;
            workerData.Manager.ChangeState(WorkerStateTypeEnum.Seek);
        }
    }
}