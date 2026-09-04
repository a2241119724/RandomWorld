namespace LAB2D.Character.Worker.Task.Individual
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Serializable;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 防守待命任务（M2A 包 2.1）— 夜袭时走到指定位置驻守至黎明。
    /// 行为分化（参战守核心/躲回床家/趁乱溜边）由 WorkerDefenceManager 按
    /// DefenceDraftRuleService 决策后派发，本任务只负责"到位 + 待命整夜"。
    /// 防守接敌（M2B）：待命期间索敌视野内有敌人且持有武器时主动进攻击状态，
    /// 复用既有被动反击通路（ReduceHp → Attack）；被打反击照旧，任务保持。
    /// 任务时长 = 距黎明秒数，到点自然 Finish 收工（墙钟推进，接敌帧暂停补时）。
    /// </summary>
    [Serializable]
    public class WorkerDefendTask : AWorkerTask
    {
        // MonoBehaviour 引用禁止入存档（BinaryFormatter 序列化即抛异常），用法仅相等比较/OwnerWorkerId，null 安全
        [NonSerialized]
        private AWorker worker;

        /// <summary>驻守时长（秒）— builder 按"距黎明秒数"设置。</summary>
        private float defendSeconds = WorkerTaskTimeConfig.DefaultTaskSeconds;

        /// <summary>防守索敌半径（世界单位，略大于箭塔射程）。</summary>
        private const float DefendSightRange = 8f;

        /// <summary>索敌节流间隔（秒）— Execute 每帧被调，索敌列表分配按节流控制。</summary>
        private const float EnemyScanInterval = 0.5f;

        [NonSerialized]
        private float enemyScanTimer;

        public WorkerDefendTask()
            : base(WorkerTaskType.Defend)
        {
            // 索敌错相：入夜 RunNightDraft 同帧给全员派任务，0 起点会保持同相位齐扫；
            // 负随机起点把各 Worker 的 0.5s 扫描帧错开（同 SeekEnemyMoveState.roamRestSeconds 抖动思路）。
            this.enemyScanTimer = -UnityEngine.Random.Range(0f, EnemyScanInterval);
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

        /// <summary>
        /// 防守接敌（M2B，治 M2A 防守夜罚站空转）：待命期间索敌视野内出现敌人
        /// 且自身持有武器时，主动进入攻击状态——复用被动反击通路：
        /// AttackState.OnEnter 据 LastAttacker 锁定 AttackTarget（直接设 AttackTarget
        /// 会被覆盖）、无武器自动转 Escape、打完回 Seek 续岗且任务保持。
        /// 无武器/无敌 → 纯待命计时（M2A 行为）。
        /// </summary>
        public override bool Execute(AWorker worker, float deltaTime)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd?.Weapon != null
                && worker.Manager.CurrentStateType != AWorkerState.TypeEnum.Attack)
            {
                this.enemyScanTimer += deltaTime;
                if (this.enemyScanTimer >= EnemyScanInterval)
                {
                    this.enemyScanTimer = 0f;
                    // 网格最近邻查询（零分配，O(局部敌人数)，替代原全列表扫描+取最近）
                    AEnemy enemy = SkillTool.GetNearestEnemyInRadius(worker.transform.position, DefendSightRange, out _);
                    if (enemy != null)
                    {
                        worker.LastAttacker = enemy;
                        worker.Manager.ChangeState(AWorkerState.TypeEnum.Attack);
                        AWorkerTask.LogProvider(
                            $"[DefenceDiag] {worker.name} 防守接敌 → {enemy.name}@({enemy.transform.position.x:F0},{enemy.transform.position.y:F0})",
                            LogManager.LogLevelEnum.Debug);
                        return false; // 接敌帧不推进驻守时长
                    }
                }
            }

            return base.Execute(worker, deltaTime);
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

        /// <summary>驻守按墙钟推进（M2A 审查中 7）：天亮时间与个人效率无关，不吃乘数链。</summary>
        protected override bool IgnoresProgressMultiplier => true;

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
