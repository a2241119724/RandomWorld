namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;

    /// <summary>
    /// 防守待命任务（M2A 包 2.1）— 夜袭时走到指定位置驻守至黎明。
    /// 行为分化（参战守核心/躲回床家/趁乱溜边）由 WorkerDefenceManager 按
    /// DefenceDraftRuleService 决策后派发，本任务只负责"到位 + 待命整夜"。
    /// 迎敌零新逻辑：被打时走既有被动反击（ReduceHp → Attack 状态），
    /// 反击结束回 Seek 续岗；任务时长 = 距黎明秒数，到点自然 Finish 收工。
    /// </summary>
    [Serializable]
    public class WorkerDefendTask : AWorkerTask
    {
        private AWorker worker;

        /// <summary>驻守时长（秒）— builder 按"距黎明秒数"设置。</summary>
        private float defendSeconds = WorkerTaskTimeConfig.DefaultTaskSeconds;

        public WorkerDefendTask()
            : base(WorkerTaskType.Defend)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = this.defendSeconds;
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
        protected override bool DoIsCanWork(AWorker worker)
        {
            return this.worker == worker;
        }

        /// <summary>站岗不累积疲劳（非劳作）。</summary>
        protected override bool ConsumesTiredness => false;

        /// <summary>站岗不累积压力（战斗压力由受击/反击路径自然产生）。</summary>
        protected override bool ConsumesStress => false;

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.WorkerSpecific;

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.worker != null ? this.worker.GetInstanceID() : 0;

        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            this.AvailableNeighborPos.Add(Neighbors[0]);
            this.AvailableNeighborPos.Add(Neighbors[1]);
            this.AvailableNeighborPos.Add(Neighbors[2]);
            this.AvailableNeighborPos.Add(Neighbors[3]);
        }

        /// <summary>
        /// 建造者
        /// </summary>
        public class DefendTaskBuilder
        {
            private readonly WorkerDefendTask task;

            public DefendTaskBuilder()
            {
                this.task = new WorkerDefendTask();
            }

            public DefendTaskBuilder SetTarget(UnityEngine.Vector3Int posMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(posMap);
                return this;
            }

            public DefendTaskBuilder SetWorker(AWorker worker)
            {
                this.task.worker = worker;
                return this;
            }

            /// <summary>设置驻守时长（秒），到点任务自然完成。</summary>
            public DefendTaskBuilder SetDuration(float seconds)
            {
                this.task.defendSeconds = seconds;
                return this;
            }

            public WorkerDefendTask Build()
            {
                return this.task;
            }
        }
    }
}
