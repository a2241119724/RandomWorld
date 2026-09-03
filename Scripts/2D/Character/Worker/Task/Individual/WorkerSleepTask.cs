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
        // MonoBehaviour 引用禁止入存档（BinaryFormatter 序列化即抛异常），用法仅相等比较/OwnerWorkerId，null 安全
        [NonSerialized]
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
                // 有床睡眠：疲劳清零 + 精气神奖励 + 压力大减 + 士气回升
                workerData.CurTired = 0.0f;
                workerData.CurSpirit = System.Math.Min(
                    workerData.MaxSpirit,
                    workerData.CurSpirit + Constant.WorkerConditionConstant.SpiritSleepRestoreBonus);
                workerData.CurStress = System.Math.Max(
                    0.0f,
                    workerData.CurStress - Constant.WorkerConditionConstant.StressSleepRestoreBonus);
                workerData.CurMorale = System.Math.Min(
                    workerData.MaxMorale,
                    workerData.CurMorale + Constant.WorkerConditionConstant.MoraleSleepRestoreBonus);
                workerData.GroundSleepCount = 0;
            }
            else
            {
                // 地面睡眠：部分降低疲劳 + 少量精气神 + 部分减压 + 少量士气
                float restoreAmount = workerData.MaxTired * Constant.WorkerConditionConstant.GroundSleepTiredRestoreRatio;
                workerData.CurTired = System.Math.Max(
                    0.0f,
                    workerData.CurTired - restoreAmount);
                workerData.CurSpirit = System.Math.Min(
                    workerData.MaxSpirit,
                    workerData.CurSpirit + Constant.WorkerConditionConstant.SpiritSleepRestoreOnGround);
                workerData.CurStress = System.Math.Max(
                    0.0f,
                    workerData.CurStress - Constant.WorkerConditionConstant.StressSleepRestoreOnGround);
                workerData.CurMorale = System.Math.Min(
                    workerData.MaxMorale,
                    workerData.CurMorale + Constant.WorkerConditionConstant.MoraleSleepRestoreOnGround);
                workerData.GroundSleepCount++;
            }

            // 睡觉即打坐吐纳：按睡眠时长积累灵气（床睡全额，地面睡半额——鼓励建床）；
            // 突破不在此结算（被打断走 GiveUpTask 不会 Finish），由 CultivationManager 扫描统一处理
            CultivationManager.Instance.MeditateFor(
                workerData,
                WorkerTaskTimeConfig.SleepSeconds,
                worker.BedItem != null ? 1f : CultivationManager.GroundSleepQiScale);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 疲劳值高于 MaxTired-阈值 即可睡觉（不要求有床，无床会走地面睡眠）
            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            return workerData.CurTired > workerData.MaxTired - AWorker.ThresholdTired && this.worker == worker;
        }

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        protected override bool ConsumesStress => false;

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
