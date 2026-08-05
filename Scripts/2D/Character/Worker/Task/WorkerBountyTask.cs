namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Worker;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 悬赏任务 — 包装一个底层任务（Gather/Build/Carry 等），只添加悬赏/金钱逻辑。
    ///
    /// 核心设计：
    /// - innerTask 负责全部工作逻辑（多阶段、寻路、进度、工作效果）
    /// - WorkerBountyTask 只负责金钱（托管、结算）和过滤（不接自己的悬赏）
    ///
    /// TargetMap 委托给 innerTask.TargetMap，确保多阶段任务（如 Build 先取材料再建造）
    /// 切换 TargetMap 时，Seek.OnEnter 始终读取到正确的当前阶段目标位置。
    ///
    /// AvailableNeighborPos 共享 innerTask 的同一 List 引用，innerTask 阶段切换时
    /// 修改列表，悬赏任务自动同步。
    /// </summary>
    [Serializable]
    public class WorkerBountyTask : AWorkerTask
    {
        [SerializeField]
        private AWorkerTask innerTask;

        private BountyData bountyData;

        public WorkerBountyTask()
            : base(WorkerTaskType.Bounty)
        {
        }

        public AWorkerTask InnerTask => this.innerTask;
        public BountyData BountyInfo => this.bountyData;

        // ---- 关键委托：TargetMap 始终返回 innerTask 的当前值 ----

        /// <inheritdoc/>
        /// <remarks>委托给 innerTask，保证多阶段任务切换目标位置时 Seek.OnEnter 读到正确值。</remarks>
        public override Vector3IntLAB TargetMap
        {
            get => this.innerTask != null ? this.innerTask.TargetMap : base.TargetMap;
            protected set => base.TargetMap = value;
        }

        // ---- Traits：合并业务标志 ----

        /// <inheritdoc/>
        public override TaskTraits Traits
        {
            get
            {
                TaskTraits combined = TaskTraits.Expirable | TaskTraits.SettleOnComplete;
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

        // ---- lifecycle 属性：委托给 innerTask ----

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        protected override bool BlocksWhenHungry => false;

        /// <inheritdoc/>
        protected override bool RequiresWalkableNeighbor => false;

        // ---- 生命周期 ----

        /// <inheritdoc/>
        /// <remarks>
        /// 委托 innerTask.Start：innerTask 内部的 ChangeStage → ChangeState(Seek)
        /// 会正确触发 Worker 寻路到 innerTask 当前阶段的目标位置。
        /// </remarks>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.bountyData = this.bountyData.WithState(BountyState.Accepted);
            this.innerTask?.Start(worker);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 委托 innerTask.Execute 执行实际工作（包括多阶段流转）。
        /// innerTask 完成时会调用 innerTask.Finish → 清除 workerData.Task，
        /// 这里立即恢复为 this，然后处理悬赏结算。
        /// </remarks>
        public override bool Execute(AWorker worker, float deltaTime)
        {
            if (this.innerTask == null)
            {
                return base.Execute(worker, deltaTime);
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            bool innerComplete = this.innerTask.Execute(worker, deltaTime);

            // innerTask.Finish 清除了 workerData.Task，恢复为悬赏任务
            if (workerData != null)
            {
                workerData.Task = this;
            }

            if (innerComplete)
            {
                this.SettleReward(worker);
                this.bountyData = this.bountyData.WithState(BountyState.Completed);

                TaskCompletionProvider(this);
                TaskLifecycleProvider(this, worker, false);
                if (workerData != null)
                {
                    workerData.Task = null;
                }
            }

            return innerComplete;
        }

        /// <inheritdoc/>
        public override void Finish(AWorker worker)
        {
            if (this.bountyData.State == BountyState.Accepted)
            {
                this.SettleReward(worker);
                this.bountyData = this.bountyData.WithState(BountyState.Completed);
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
            if (worker.GetInstanceID() == this.bountyData.IssuerWorkerId)
                return false;
            if (this.bountyData.State != BountyState.Posted)
                return false;
            return this.innerTask != null;
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            // AvailableNeighborPos 共享 innerTask 的引用，innerTask 阶段变更时自动同步
            this.AvailableNeighborPos.Clear();
            if (this.innerTask != null)
            {
                // 直接复用 innerTask 的列表引用，而非拷贝
                // 这样 innerTask.ChangeStage → stageInit 修改列表时，悬赏任务自动同步
                this.AvailableNeighborPos = this.innerTask.AvailableNeighborPos;
            }
        }

        private void SettleReward(AWorker executor)
        {
            try
            {
                var cm = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
                cm.CompleteBounty(this.bountyData.IssuerWorkerId, executor, this.bountyData.Reward);
            }
            catch (Exception e)
            {
                LogProvider($"悬赏结算失败: {e.Message}", LogManager.LogLevelEnum.Error);
            }
        }

        // ---- Builder ----

        public class BountyTaskBuilder
        {
            private AWorkerTask innerTask;
            private CurrencyAmount reward;
            private int issuerWorkerId;
            private float expirationGameTime;

            public BountyTaskBuilder SetInnerTask(AWorkerTask task)
            {
                this.innerTask = task;
                return this;
            }

            public BountyTaskBuilder SetReward(CurrencyAmount reward)
            {
                this.reward = reward;
                return this;
            }

            public BountyTaskBuilder SetIssuer(int workerId)
            {
                this.issuerWorkerId = workerId;
                return this;
            }

            public BountyTaskBuilder SetExpiration(float gameTimeSeconds)
            {
                this.expirationGameTime = gameTimeSeconds;
                return this;
            }

            public WorkerBountyTask Build()
            {
                if (this.innerTask == null)
                {
                    throw new InvalidOperationException("WorkerBountyTask: innerTask 不能为空");
                }

                var task = new WorkerBountyTask
                {
                    innerTask = this.innerTask,
                    bountyData = new BountyData(this.reward, this.issuerWorkerId, this.expirationGameTime),
                };

                task.Name = $"悬赏-{this.innerTask.TaskType}";
                task.Init();
                return task;
            }
        }
    }
}
