namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
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
            : base(WorkerTaskType.Sleep)
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

            if (worker.BedItem != null)
            {
                // 有床睡眠：全额恢复疲劳 + 精气神奖励
                workerData.CurTired = workerData.MaxTired;
                workerData.CurSpirit = System.Math.Min(
                    workerData.MaxSpirit,
                    workerData.CurSpirit + Constant.WorkerConditionConstant.SpiritSleepRestoreBonus);
                workerData.GroundSleepCount = 0;
            }
            else
            {
                // 地面睡眠：部分恢复疲劳 + 少量精气神
                float restoreAmount = workerData.MaxTired * Constant.WorkerConditionConstant.GroundSleepTiredRestoreRatio;
                workerData.CurTired = System.Math.Min(
                    workerData.MaxTired,
                    workerData.CurTired + restoreAmount);
                workerData.CurSpirit = System.Math.Min(
                    workerData.MaxSpirit,
                    workerData.CurSpirit + Constant.WorkerConditionConstant.SpiritSleepRestoreOnGround);
                workerData.GroundSleepCount++;
            }
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 疲劳值低于阈值即可睡觉（不要求有床，无床会走地面睡眠）
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return workerData.CurTired < AWorker.ThresholdTired && this.worker == worker;
        }

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.WorkerSpecific;

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.worker != null ? this.worker.GetInstanceID() : 0;

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
