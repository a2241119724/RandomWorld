namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D;
    using System;
    using UnityEngine;

    /// <summary>
    /// 睡觉任务
    /// </summary>
    [Serializable]
    public class WorkerSleepTask : AWorkerTask
    {
        private AWorker worker;

        public WorkerSleepTask()
            : base(WorkerTaskTypeEnum.Sleep)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.SleepSeconds;

                // 获取物资
                this.Init();
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            base.Finish(worker);
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            workerData.CurTired = workerData.MaxTired;
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 如果疲劳值低于阈值，并且有床，则可以睡觉
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return workerData.CurTired < AWorker.ThresholdTired && worker.BedItem != null && this.worker == worker;
        }

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
        }

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

            public SleepTaskBuilder SetWorker(AWorker worker)
            {
                this.task.worker = worker;
                return this;
            }

            public WorkerSleepTask Build()
            {
                return this.task;
            }
        }
    }
}
