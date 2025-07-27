namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 睡觉任务
    /// </summary>
    public class WorkerSleepTask : WorkerTask
    {
        private Worker worker;

        public WorkerSleepTask()
            : base(TaskType.Sleep)
        {
            this.stageInit.Add((Worker worker) =>
            {
                this.maxProgress = 10.0f;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 进入工作状态
                worker.Manager.ChangeState(WorkerStateType.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Execute1()
        {
            base.Execute1();
        }

        /// <inheritdoc/>
        public override bool IsCanWork(Worker worker)
        {
            if (!base.IsCanWork(worker))
            {
                return false;
            }

            // 如果疲劳值低于阈值，并且有床，则可以睡觉
            return worker.CurTired < Worker.ThresholdTired && worker.BedItem != null && this.worker == worker;
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            base.Finish(worker);
        }

#pragma warning disable SA1600 // Elements should be documented
        /// <summary>
        /// 建造者
        /// </summary>
        public class SleepTaskBuilder
        {
            private readonly WorkerSleepTask task;

            public SleepTaskBuilder()
            {
                this.task = new WorkerSleepTask();
            }

            public SleepTaskBuilder SetTarget(Vector3Int posMap)
            {
                this.task.TargetMap = posMap;
                return this;
            }

            public SleepTaskBuilder SetWorker(Worker worker)
            {
                this.task.worker = worker;
                return this;
            }

            public WorkerSleepTask Build()
            {
                return this.task;
            }
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
