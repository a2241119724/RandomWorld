namespace LAB2D
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 睡觉任务
    /// </summary>
    [Serializable]
    public class WorkerSleepTask : WorkerTask
    {
        private Worker worker;

        public WorkerSleepTask()
            : base(WorkerTaskTypeEnum.Sleep)
        {
            this.stageInit.Add((Worker worker) =>
            {
                WorkerTask.maxProgress = 10.0f;

                // 获取物资
                this.AvailableNeighborPos.Clear();
                this.AvailableNeighborPos.Add(Neighbors[8]);

                // 进入工作状态
                worker.Manager.ChangeState(WorkerState.WorkerStateTypeEnum.Seek);
            });
        }

        /// <inheritdoc/>
        public override void Start(Worker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void DoExecute()
        {
            base.DoExecute();
        }

        /// <inheritdoc/>
        public override void Finish(Worker worker)
        {
            base.Finish(worker);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(Worker worker)
        {
            // 如果疲劳值低于阈值，并且有床，则可以睡觉
            Worker.WorkerData workerData = worker.CharacterDataLAB as Worker.WorkerData;
            return workerData.CurTired < Worker.ThresholdTired && worker.BedItem != null && this.worker == worker;
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
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(posMap);
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
