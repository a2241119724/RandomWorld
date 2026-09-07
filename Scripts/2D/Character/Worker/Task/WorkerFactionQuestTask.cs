namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Gameplay.Faction;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 阵营任务 — FactionManager 日更生成（核心附近资源点），包装内层采集任务，
    /// 只有同阵营成员可接（DoIsCanWork 校验 FactionId）。完成时结算：
    /// 金库付款进执行者钱包 + 执行者阵营贡献 +10（FactionManager.TrySettleQuest，
    /// 双写走 Manager 权威账本）。产物归执行者本人（非悬赏发布者模式）。
    /// 生命周期照 WorkerBountyTask 委托 innerTask；无过期（日更生成前清理未接取旧任务）。
    /// </summary>
    [Serializable]
    public class WorkerFactionQuestTask : AWorkerTask
    {
        [SerializeField]
        private AWorkerTask innerTask;

        [SerializeField]
        private int questFactionId;

        private bool accepted;

        public WorkerFactionQuestTask()
            : base(WorkerTaskType.FactionQuest)
        {
        }

        public AWorkerTask InnerTask => this.innerTask;

        /// <summary>所属阵营 ID（接取资格校验 + 日更清理按阵营过滤）。</summary>
        public int QuestFactionId => this.questFactionId;

        // ---- 关键委托：TargetMap 始终返回 innerTask 的当前值 ----

        /// <inheritdoc/>
        /// <remarks>委托给 innerTask，保证多阶段任务切换目标位置时 Seek.OnEnter 读到正确值。</remarks>
        public override Vector3IntLAB TargetMap
        {
            get => this.innerTask != null ? this.innerTask.TargetMap : base.TargetMap;
            protected set => base.TargetMap = value;
        }

        /// <inheritdoc/>
        public override TaskTraits Traits
        {
            get
            {
                TaskTraits combined = TaskTraits.SettleOnComplete;
                if (this.innerTask != null)
                {
                    TaskTraits innerTraits = this.innerTask.Traits;
                    if ((innerTraits & TaskTraits.OnePerPosition) != 0)
                        combined |= TaskTraits.OnePerPosition;
                    if ((innerTraits & TaskTraits.TrackPositions) != 0)
                        combined |= TaskTraits.TrackPositions;
                }
                return combined;
            }
        }

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        protected override bool BlocksWhenHungry => false;

        /// <inheritdoc/>
        protected override bool RequiresWalkableNeighbor => false;

        // ---- 生命周期 ----

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.accepted = true;
            this.innerTask?.Start(worker);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 委托 innerTask.Execute（含多阶段流转）。innerTask.Finish 会清除 workerData.Task，
        /// 此处立即恢复为 this 再做阵营结算 — 与 WorkerBountyTask 同款防打断模式。
        /// </remarks>
        public override bool Execute(AWorker worker, float deltaTime)
        {
            if (this.innerTask == null)
            {
                return base.Execute(worker, deltaTime);
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            bool innerComplete = this.innerTask.Execute(worker, deltaTime);

            // innerTask.Finish 清除了 workerData.Task，恢复为阵营任务本体（不重启）
            if (workerData != null && workerData.Task == null)
            {
                worker.SetTask(this, WorkerTaskSource.BountyRestore);
            }

            if (innerComplete && !this.settled)
            {
                this.settled = true;
                this.SettleQuest(worker);

                LogProvider(
                    $"[TaskDiag] {worker.name} 完成阵营任务 inner={this.innerTask.TaskType} faction={this.questFactionId} target=({this.innerTask.TargetMap.X},{this.innerTask.TargetMap.Y})",
                    LogManager.LogLevelEnum.Debug);

                TaskCompletionProvider(this);
                TaskLifecycleProvider(this, worker, false);
                if (workerData != null)
                {
                    worker.SetTask(null, WorkerTaskSource.Clear);
                }
            }

            return innerComplete;
        }

        private bool settled;

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            // 防御式结算：外部直接调 Finish（非 Execute 路径完成）时也不漏账
            if (this.accepted && !this.settled)
            {
                this.settled = true;
                this.SettleQuest(worker);
            }

            base.Finish(worker);
        }

        /// <inheritdoc/>
        public override void GiveUpTask(AWorker worker)
        {
            this.innerTask?.GiveUpTask(worker);
            base.GiveUpTask(worker);
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            if (this.accepted || this.innerTask == null || this.questFactionId <= 0)
            {
                return false;
            }

            // 只有同阵营成员可接（阵营任务是内部工单）
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            return wd != null && wd.FactionId == this.questFactionId;
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            this.AvailableNeighborPos.Clear();
            if (this.innerTask != null)
            {
                // 复用 innerTask 的列表引用，innerTask 阶段变更时自动同步（Bounty 同款）
                this.AvailableNeighborPos = this.innerTask.AvailableNeighborPos;
            }
        }

        /// <summary>
        /// 日更清理未接取任务时释放内层采集认领（GatherMap），防止资源格被死任务占位。
        /// 已接取的任务由执行者的正常 GiveUp/Finish 路径释放，勿调此方法。
        /// </summary>
        public void ReleaseUnclaimed()
        {
            if (this.accepted || this.settled)
            {
                return;
            }

            if (this.innerTask is WorkerGatherTask && this.innerTask.TargetMap != null)
            {
                Vector3Int posMap = Vector3IntLAB.ToVector3Int(this.innerTask.TargetMap);
                GatherMapProvider()?.CancelGather(posMap);
            }
        }

        private void SettleQuest(AWorker executor)
        {
            try
            {
                if (Core.ServiceLocator.TryGet(out Gameplay.FactionManager factionManager))
                {
                    factionManager.TrySettleQuest(executor, this.questFactionId);
                }
            }
            catch (Exception e)
            {
                LogProvider($"阵营任务结算失败: {e.Message}", LogManager.LogLevelEnum.Error);
            }
        }

        // ---- Builder ----

        public class QuestTaskBuilder
        {
            private AWorkerTask innerTask;
            private int factionId;

            public QuestTaskBuilder SetInnerTask(AWorkerTask task)
            {
                this.innerTask = task;
                return this;
            }

            public QuestTaskBuilder SetFaction(int factionId)
            {
                this.factionId = factionId;
                return this;
            }

            public WorkerFactionQuestTask Build()
            {
                if (this.innerTask == null)
                {
                    throw new InvalidOperationException("WorkerFactionQuestTask: innerTask 不能为空");
                }

                if (this.factionId <= 0)
                {
                    throw new InvalidOperationException("WorkerFactionQuestTask: factionId 必须为正");
                }

                var task = new WorkerFactionQuestTask
                {
                    innerTask = this.innerTask,
                    questFactionId = this.factionId,
                };

                task.Name = $"阵营任务-{this.innerTask.TaskType}";
                task.Init();
                return task;
            }
        }
    }
}
