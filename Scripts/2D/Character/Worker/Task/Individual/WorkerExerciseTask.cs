namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 锻炼任务
    /// </summary>
    [Serializable]
    public class WorkerExerciseTask : AWorkerTask
    {
        // MonoBehaviour 引用禁止入存档（BinaryFormatter 序列化即抛异常），用法仅相等比较/OwnerWorkerId，null 安全
        [NonSerialized]
        private AWorker worker;

        public WorkerExerciseTask()
            : base(WorkerTaskType.Exercise)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = WorkerTaskTimeConfig.ExerciseSeconds;
                this.Init();

                // 设置Worker位置为目标位置
                this.TargetMap = Vector3IntLAB.ToVector3IntLAB(TileMapWorldToMapProvider(worker.transform.position));
            });
        }

        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        protected override bool DoIsCanWork(AWorker worker)
        {
            return this.worker == worker;
        }

        /// <inheritdoc/>
        protected override bool RequiresWalkableNeighbor => false;

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        protected override bool ConsumesStress => false;

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.WorkerSpecific;

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.worker != null ? this.worker.GetInstanceID() : 0;

        /// <summary>每次锻炼消耗的金币（训练费）</summary>
        private const int ExerciseCost = 2;

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            // 扣除锻炼费用 — 有钱才扣，没钱免费锻炼
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd != null && wd.Wallet.HasEnough(new Domain.Worker.CurrencyAmount(ExerciseCost)))
            {
                wd.Wallet -= new Domain.Worker.CurrencyAmount(ExerciseCost);
            }

            // 根据锻炼时长结算经验值
            int experienceGained = Mathf.RoundToInt(
                this.maxProgress * WorkerTaskTimeConfig.ExerciseExperiencePerSecond);
            if (experienceGained > 0)
            {
                worker.AddExperienceValue(experienceGained);
            }

            // 锻炼减压：出汗放松
            if (wd != null)
            {
                wd.CurStress = System.Math.Max(
                    0.0f,
                    wd.CurStress - Constant.WorkerConditionConstant.StressExerciseRestore);
            }

            base.Finish(worker);
        }

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[8]);
        }

        public class ExerciseTaskBuilder
        {
            private readonly WorkerExerciseTask task;

            public ExerciseTaskBuilder()
            {
                this.task = new WorkerExerciseTask();
            }

            public ExerciseTaskBuilder SetTarget(Vector3Int targetMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(targetMap);
                return this;
            }

            public ExerciseTaskBuilder SetWorker(AWorker worker)
            {
                this.task.worker = worker;
                return this;
            }

            public WorkerExerciseTask Build()
            {
                return this.task;
            }
        }
    }
}
