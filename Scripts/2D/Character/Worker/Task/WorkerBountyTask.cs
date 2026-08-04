namespace LAB2D.Character.Worker.Task
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Domain.Worker;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 悬赏任务 — 包装一个底层任务（Build/Carry/Gather/Plant 等），添加悬赏机制。
    ///
    /// 核心设计：组合/包装模式。一个 WorkerBountyTask 包装任意 AWorkerTask 子类，
    /// 避免为每种任务类型创建独立的 Bounty* 子类（N×M 类爆炸问题）。
    ///
    /// Execute 委托给 innerTask 执行实际工作逻辑，并拦截 innerTask.Finish()
    /// 对 workerData.Task 的清除操作，确保结算正确完成。
    ///
    /// 生命周期：
    ///   Posted ──→ Accepted ──→ Completed（结算: issuer→executor）
    ///      │           │
    ///      └──→ Expired / Cancelled（退款给 issuer）
    /// </summary>
    [Serializable]
    public class WorkerBountyTask : AWorkerTask
    {
        /// <summary>
        /// 被包装的底层任务（Build/Carry/Gather/Plant 等）。
        /// </summary>
        [SerializeField]
        private AWorkerTask innerTask;

        /// <summary>
        /// 悬赏数据（金额、发布者、过期时间、状态）。
        /// </summary>
        private BountyData bountyData;

        /// <summary>
        /// 构造函数 — 悬赏任务固定使用 WorkerTaskType.Bounty。
        /// </summary>
        public WorkerBountyTask()
            : base(WorkerTaskType.Bounty)
        {
        }

        /// <summary>
        /// 被包装的底层任务。外部只读。
        /// </summary>
        public AWorkerTask InnerTask => this.innerTask;

        /// <summary>
        /// 悬赏元数据。外部只读。
        /// </summary>
        public BountyData BountyInfo => this.bountyData;

        /// <inheritdoc/>
        /// <remarks>
        /// 合并 Expirable + SettleOnComplete 与 innerTask 的 traits。
        /// 排除 WorkerSpecific 和 ReturnToIdle（悬赏应对所有非发布者 Worker 开放）。
        /// </remarks>
        public override TaskTraits Traits
        {
            get
            {
                TaskTraits combined = TaskTraits.Expirable | TaskTraits.SettleOnComplete;

                if (this.innerTask != null)
                {
                    TaskTraits innerTraits = this.innerTask.Traits;
                    if ((innerTraits & TaskTraits.OnePerPosition) != 0)
                    {
                        combined |= TaskTraits.OnePerPosition;
                    }

                    if ((innerTraits & TaskTraits.TrackPositions) != 0)
                    {
                        combined |= TaskTraits.TrackPositions;
                    }
                }

                return combined;
            }
        }

        // 以下属性设置为 false，因为 Execute 会完全委托给 innerTask，
        // innerTask 内部已自行处理这些检查。避免基类重复应用。

        /// <inheritdoc/>
        protected override bool ConsumesTiredness => false;

        /// <inheritdoc/>
        protected override bool BlocksWhenHungry => false;

        /// <inheritdoc/>
        protected override bool RequiresWalkableNeighbor => false;

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);

            // 标记为已接受
            this.bountyData = this.bountyData.WithState(BountyState.Accepted);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 关键流程：委托 innerTask.Execute 执行实际工作。
        /// innerTask 完成时会自动调用 innerTask.Finish()（清除 workerData.Task），
        /// 因此需要立即恢复 workerData.Task 指向本悬赏任务，然后自行处理结算和清理。
        /// </remarks>
        public override bool Execute(AWorker worker, float deltaTime)
        {
            if (this.innerTask == null)
            {
                return base.Execute(worker, deltaTime);
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;

            // 委托给内部任务执行实际工作
            // innerTask.Execute 内部会在完成时调用 innerTask.Finish()
            // 后者会清除 workerData.Task — 我们需要拦截并恢复
            bool innerComplete = this.innerTask.Execute(worker, deltaTime);

            // 恢复 workerData.Task（innerTask.Finish 将其设为 null）
            if (workerData != null)
            {
                workerData.Task = this;
            }

            if (innerComplete)
            {
                // 悬赏结算：托管资金从发布者转给执行者
                this.SettleReward(worker);
                this.bountyData = this.bountyData.WithState(BountyState.Completed);

                // 调用基类清理逻辑（从队列移除、记录统计）
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
        /// <remarks>
        /// 结算已在 Execute() 中处理。此方法作为安全网，处理 Execute 未调用的情况。
        /// </remarks>
        public override void Finish(AWorker worker)
        {
            // 如果悬赏已被接受但 Execute 未结算（异常路径）
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
            if (this.innerTask != null)
            {
                this.innerTask.GiveUpTask(worker);
            }

            base.GiveUpTask(worker);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 只检查悬赏任务自身的条件（发布者、状态、任务有效性），
        /// 不委托 innerTask.IsCanWork()——那会检查 inner task 类型的 TaskToggle。
        /// 悬赏是"花钱请人干活"，不应受 inner task 类型开关的限制。
        /// Worker 只需 TaskToggle[Bounty] 启用即可接取（base.IsCanWork 已检查）。
        /// </remarks>
        protected override bool DoIsCanWork(AWorker worker)
        {
            // 发布者不能接自己的悬赏
            if (worker.GetInstanceID() == this.bountyData.IssuerWorkerId)
            {
                return false;
            }

            // 只能接 Posted 状态的悬赏
            if (this.bountyData.State != BountyState.Posted)
            {
                return false;
            }

            // 内部任务必须存在
            if (this.innerTask == null)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void Init()
        {
            // 复制内部任务的可用位置到外层
            this.AvailableNeighborPos.Clear();
            if (this.innerTask != null)
            {
                this.AvailableNeighborPos.AddRange(this.innerTask.AvailableNeighborPos);
            }
        }

        /// <summary>
        /// 结算悬赏：托管资金从发布者转给执行者。
        /// </summary>
        /// <param name="executor">执行任务的 Worker</param>
        private void SettleReward(AWorker executor)
        {
            try
            {
                var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
                currencyManager.CompleteBounty(this.bountyData.IssuerWorkerId, executor, this.bountyData.Reward);
            }
            catch (System.Exception e)
            {
                LogProvider(
                    $"悬赏结算失败: issuer={this.bountyData.IssuerWorkerId}, reward={this.bountyData.Reward}, error={e.Message}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        // ---- Builder ----

        /// <summary>
        /// 悬赏任务构建器。
        /// <code>
        /// var task = new WorkerBountyTask.BountyTaskBuilder()
        ///     .SetInnerTask(new WorkerGatherTask.GatherTaskBuilder().SetTarget(pos).SetResourceInfo(info).Build())
        ///     .SetReward(new CurrencyAmount(10))
        ///     .SetIssuer(worker.GetInstanceID())
        ///     .SetExpiration(120f)
        ///     .Build();
        /// </code>
        /// </summary>
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

                // 复制目标位置和名称
                task.TargetMap = this.innerTask.TargetMap;
                task.Name = $"悬赏-{this.innerTask.TaskType}";

                // 重新初始化（此时 innerTask 已赋值，Init() 会复制位置）
                task.Init();

                return task;
            }
        }
    }
}
