namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 探索洞府任务（M4 包 4 地图兴趣点轮 3）— 寻路到洞府邻格驻留推进（60s 基准，
    /// 实际吃 ProgressMultiplierProvider 全乘数链：天气×修饰符×状态×熟练度×生活技能），
    /// 到点 AncientCaveManager.CompleteExplore 结算风险（妖兽/塌方/平安）与奖励
    /// （功法/物资/装备/双倍）。任务不存档（既有约定）：读档后洞府状态已持久，可重派。
    /// </summary>
    [Serializable]
    public class WorkerExploreTask : AWorkerTask
    {
        // MonoBehaviour 引用禁止入存档（BinaryFormatter 序列化即抛异常），用法仅相等比较/OwnerWorkerId，null 安全
        [NonSerialized]
        private AWorker worker;

        /// <summary>探索的洞府索引（AncientCaveManager.Caves 下标）。</summary>
        private int caveIndex = -1;

        public WorkerExploreTask()
            : base(WorkerTaskType.Explore)
        {
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = Domain.Gameplay.AncientCave.CaveExploreRuleService.WorkerExploreSeconds;
                this.Init();
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <summary>完成结算交 Manager（占用释放 + 风险/奖励 roll + 状态 Explored）。</summary>
        public override void Finish(AWorker worker)
        {
            if (Core.ServiceLocator.TryGet(out AncientCaveManager caveManager))
            {
                caveManager.CompleteExplore(this.caveIndex, explorer: worker);
            }

            base.Finish(worker);
        }

        /// <summary>放弃/失败回滚：Exploring→Revealed（洞府可再被探索）。</summary>
        public override void GiveUpTask(AWorker worker)
        {
            if (Core.ServiceLocator.TryGet(out AncientCaveManager caveManager))
            {
                caveManager.CancelWorkerExplore(this.caveIndex);
            }

            base.GiveUpTask(worker);
        }

        /// <summary>仅指派者可做（系统专属任务，不进公共领取池）。</summary>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return this.worker == worker;
        }

        /// <summary>洞中探索累积疲劳（对比防守站岗不累）。</summary>
        protected override bool ConsumesTiredness => true;

        /// <summary>幽暗洞府精神压力（受击压力另计）。</summary>
        protected override bool ConsumesStress => true;

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
        public class ExploreTaskBuilder
        {
            private readonly WorkerExploreTask task;

            public ExploreTaskBuilder()
            {
                this.task = new WorkerExploreTask();
            }

            public ExploreTaskBuilder SetTarget(Vector3Int posMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(posMap);
                return this;
            }

            public ExploreTaskBuilder SetWorker(AWorker worker)
            {
                this.task.worker = worker;
                return this;
            }

            /// <summary>设置探索的洞府索引（结算时定位洞府与占用标记）。</summary>
            public ExploreTaskBuilder SetCaveIndex(int caveIndex)
            {
                this.task.caveIndex = caveIndex;
                return this;
            }

            public WorkerExploreTask Build()
            {
                return this.task;
            }
        }
    }
}
