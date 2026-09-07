namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Enum;
    using LAB2D.Serializable;
    using System;
    using UnityEngine;

    /// <summary>
    /// 阵营攻伐任务（P5）— 攻伐窗口期出征敌阵营核心。
    /// 两阶段：March（行军至敌核心旁 + 驻留掠夺计时）→ 掠夺结算（金库 20% 转移 +
    /// 战功贡献 + 关系恶化）→ Return（返程回家）→ 自然 Finish。
    /// 途中索敌接敌照 WorkerDefendTask：视野内出现敌阵营 Worker 且持有武器时
    /// LastAttacker 锁定进攻击状态，复用被动反击通路，打完回 Seek 续任务。
    /// 入夜由 FactionManager 相位事件 GiveUpTask 召回（未完成不出战果）。
    /// </summary>
    [Serializable]
    public class WorkerFactionRaidTask : AWorkerTask
    {
        /// <summary>抵达敌核心后的掠夺驻留时长（秒）。</summary>
        private const float LootSeconds = 8f;

        /// <summary>返程阶段的完成时长（秒，到家即结）。</summary>
        private const float ReturnSeconds = 1f;

        /// <summary>索敌视野半径（世界单位）。</summary>
        private const float SightRange = 8f;

        /// <summary>索敌节流间隔（秒）。</summary>
        private const float EnemyScanInterval = 0.5f;

        /// <summary>索敌视野半径平方（比较用）。</summary>
        private const float SightRangeSqr = SightRange * SightRange;

        // MonoBehaviour 引用禁止入存档（BinaryFormatter 序列化即抛异常），用法仅相等比较/OwnerWorkerId，null 安全
        [NonSerialized]
        private AWorker worker;

        /// <summary>敌对阵营 ID（索敌过滤）。</summary>
        private int targetFactionId;

        /// <summary>返程目标（家/出发点）。</summary>
        private Vector3IntLAB returnPos;

        /// <summary>掠夺是否已结算（阶段切换结算一次，防重复）。</summary>
        [NonSerialized]
        private bool lootSettled;

        [NonSerialized]
        private float enemyScanTimer;

        public WorkerFactionRaidTask()
            : base(WorkerTaskType.FactionRaid)
        {
            this.enemyScanTimer = -UnityEngine.Random.Range(0f, EnemyScanInterval); // 索敌错相
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = LootSeconds; // 阶段 0：行军 + 掠夺驻留
            });
            this.stageInit.Add((AWorker worker) =>
            {
                this.maxProgress = ReturnSeconds; // 阶段 1：返程
                this.TargetMap = this.returnPos;
            });
        }

        /// <inheritdoc/>
        public override void Start(AWorker worker)
        {
            base.Start(worker);
            this.ChangeStage(worker, 0);
        }

        /// <summary>
        /// 途中接敌：视野内有敌阵营 Worker 且持有武器时主动进攻击状态
        /// （复用被动反击通路，互殴好感/关系惩罚天然生效；接敌帧不推进掠夺计时）。
        /// </summary>
        public override bool Execute(AWorker worker, float deltaTime)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (this.stage == 0
                && wd?.Weapon != null
                && worker.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
            {
                this.enemyScanTimer += deltaTime;
                if (this.enemyScanTimer >= EnemyScanInterval)
                {
                    this.enemyScanTimer = 0f;
                    if (this.FindEnemyFactionWorker(worker, out AWorker foe))
                    {
                        worker.LastAttacker = foe;
                        worker.Manager.ChangeState(AWorkerState.TypeEnum.Attack);
                        AWorkerTask.LogProvider(
                            $"[FactionDiag] {worker.name} 攻伐接敌 → {foe.name}",
                            LogManager.LogLevelEnum.Debug);
                        return false; // 接敌帧不推进掠夺计时
                    }
                }
            }

            return base.Execute(worker, deltaTime);
        }

        /// <summary>
        /// 索敌：视野内最近敌阵营 Worker（攻伐窗口期仅 ≤8 人出征，0.5s 节流下
        /// 全列表距离比较可忽略，无需网格索引）。
        /// </summary>
        private bool FindEnemyFactionWorker(AWorker worker, out AWorker foe)
        {
            foe = null;
            WorkerManager workerManager = ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null)
            {
                return false;
            }

            float bestSqr = SightRangeSqr;
            foreach (AWorker other in workerManager.Characters)
            {
                if (other == null || other == worker)
                {
                    continue;
                }

                if (other.CharacterDataLAB is not AWorker.WorkerData od || od.FactionId != this.targetFactionId)
                {
                    continue;
                }

                float sqr = (other.transform.position - worker.transform.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    foe = other;
                }
            }

            return foe != null;
        }

        /// <inheritdoc/>
        protected override bool DoIsCanWork(AWorker worker)
        {
            return this.worker == worker;
        }

        /// <summary>出征不累积疲劳（行军非劳作，照防守待命）。</summary>
        protected override bool ConsumesTiredness => false;

        /// <summary>出征压力走受击路径，任务本身不累积（照防守待命）。</summary>
        protected override bool ConsumesStress => false;

        /// <summary>掠夺驻留定长推进（不吃个人效率乘数）。</summary>
        protected override bool IgnoresProgressMultiplier => true;

        /// <inheritdoc/>
        public override TaskTraits Traits => TaskTraits.WorkerSpecific;

        /// <inheritdoc/>
        public override int OwnerWorkerId => this.worker != null ? this.worker.GetInstanceID() : 0;

        /// <summary>
        /// 阶段推进：掠夺计时满 → 结算战果（一次）→ 转返程；返程到位 → 完成。
        /// </summary>
        protected override bool StageChangeRule(AWorker worker)
        {
            if (this.stage == 0)
            {
                this.SettleLootOnce(worker);
                this.ChangeStage(worker, 1);
                return false;
            }

            return true;
        }

        /// <summary>掠夺结算（防御式只结一次）：金库转移+战功贡献+关系恶化，FactionManager 统一记账。</summary>
        private void SettleLootOnce(AWorker worker)
        {
            if (this.lootSettled)
            {
                return;
            }

            this.lootSettled = true;
            if (Core.ServiceLocator.TryGet<Gameplay.FactionManager>(out Gameplay.FactionManager factions))
            {
                factions.SettleRaidLoot(worker, this.targetFactionId);
            }
        }

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
        public class RaidTaskBuilder
        {
            private readonly WorkerFactionRaidTask task;

            public RaidTaskBuilder()
            {
                this.task = new WorkerFactionRaidTask();
            }

            /// <summary>设置敌核心目标格（行军终点，FactionManager 派发时给可走邻格）。</summary>
            public RaidTaskBuilder SetTargetMap(Vector3Int posMap)
            {
                this.task.TargetMap = Vector3IntLAB.ToVector3IntLAB(posMap);
                return this;
            }

            /// <summary>设置返程目标（家/出发点）。</summary>
            public RaidTaskBuilder SetReturnPos(Vector3Int posMap)
            {
                this.task.returnPos = Vector3IntLAB.ToVector3IntLAB(posMap);
                return this;
            }

            /// <summary>设置出征者与敌对阵营。</summary>
            public RaidTaskBuilder SetRaider(AWorker raider, int enemyFactionId)
            {
                this.task.worker = raider;
                this.task.targetFactionId = enemyFactionId;
                return this;
            }

            public WorkerFactionRaidTask Build()
            {
                return this.task;
            }
        }
    }
}
