namespace LAB2D.Gameplay.TurnBattle
{
    using System.Collections.Generic;
    using LAB2D.Character;
    using LAB2D.Character.Enemy;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.State;
    using LAB2D.Core;
    using UnityEngine;

    /// <summary>
    /// 大世界交战检测器 — 0.5s 轮询聚合 Worker↔Enemy 交战对为连通分量。
    /// 检测信号：Worker 处于 Attack 状态且目标为存活 Enemy（M2B 防守接敌走此路径）；
    /// 补扫 Enemy 侧（锁定 Worker 为 Target 或最近被 Worker 打过），覆盖 Worker 逃跑/反击间隙。
    /// HUD 提示用滞回（连续 2 轮存在/消失才切换）防闪烁；G 键加入走 DetectNow 实时检测。
    /// </summary>
    public sealed class BattleEncounterDetector : ITickable
    {
        /// <summary>轮询间隔（秒）。</summary>
        private const float DetectInterval = 0.5f;

        /// <summary>滞回轮数：连续 N 轮存在才提示、连续 N 轮消失才隐藏。</summary>
        private const int HysteresisRounds = 2;

        /// <summary>玩家可加入战斗的判定半径（世界单位）。</summary>
        public const float JoinBattleRadius = 8f;

        private float detectTimer;
        private int presentStreak;
        private int absentStreak;

        /// <summary>滞回后的"附近存在交战"（HUD 提示条用）。</summary>
        public bool HasNearbyBattle { get; private set; }

        /// <summary>最近一次聚合出的交战组列表（每轮检测重建）。</summary>
        public List<BattleEncounter> Encounters { get; } = new List<BattleEncounter>();

        public void Tick(float deltaTime)
        {
            // 战斗面板打开期间大世界冻结，状态不会变化，跳过检测
            if (TurnBattleManager.Instance.IsActive)
            {
                return;
            }

            this.detectTimer += deltaTime;
            if (this.detectTimer < DetectInterval)
            {
                return;
            }

            this.detectTimer = 0f;
            this.DetectNow();
        }

        /// <summary>
        /// 立即执行一次检测并更新滞回状态（轮询超时或 G 键按下时调用）。
        /// 返回是否存在聚合出的交战组。
        /// </summary>
        public bool DetectNow()
        {
            this.RebuildEncounters();
            bool present = this.Encounters.Count > 0;
            if (present)
            {
                this.presentStreak++;
                this.absentStreak = 0;
            }
            else
            {
                this.absentStreak++;
                this.presentStreak = 0;
            }

            if (!this.HasNearbyBattle && this.presentStreak >= HysteresisRounds)
            {
                this.HasNearbyBattle = true;
            }
            else if (this.HasNearbyBattle && this.absentStreak >= HysteresisRounds)
            {
                this.HasNearbyBattle = false;
            }

            return present;
        }

        /// <summary>
        /// 取距玩家指定半径内最近的交战组（双方仍有存活成员）。
        /// </summary>
        public bool TryGetNearbyEncounter(Vector2 playerPos, out BattleEncounter nearest)
        {
            nearest = null;
            float bestSqr = JoinBattleRadius * JoinBattleRadius;
            foreach (BattleEncounter encounter in this.Encounters)
            {
                if (!encounter.HasAliveOnBothSides)
                {
                    continue;
                }

                float sqr = (encounter.Center - playerPos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    nearest = encounter;
                }
            }

            return nearest != null;
        }

        /// <summary>收集交战边并聚合连通分量，重建 Encounters。</summary>
        private void RebuildEncounters()
        {
            this.Encounters.Clear();

            List<AWorker> workers = GetAliveWorkers();
            List<AEnemy> enemies = GetAliveEnemies();
            if (workers.Count == 0 || enemies.Count == 0)
            {
                return;
            }

            // workerIndex/enemyIndex → 邻接边（交战对）
            Dictionary<AWorker, int> workerIndex = new Dictionary<AWorker, int>();
            for (int i = 0; i < workers.Count; i++)
            {
                workerIndex[workers[i]] = i;
            }

            Dictionary<AEnemy, int> enemyIndex = new Dictionary<AEnemy, int>();
            for (int i = 0; i < enemies.Count; i++)
            {
                enemyIndex[enemies[i]] = i;
            }

            HashSet<long> edges = new HashSet<long>();
            void AddEdge(AWorker w, AEnemy e)
            {
                if (w == null || e == null || !workerIndex.TryGetValue(w, out int wi) || !enemyIndex.TryGetValue(e, out int ei))
                {
                    return;
                }

                // worker 数 < 10^6，long key = wi * 大质数 + ei 无碰撞
                edges.Add((wi * 1000003L) + ei);
            }

            // 主信号：Worker 处于 Attack 状态且目标为 Enemy（含防守夜主动接敌）
            foreach (AWorker worker in workers)
            {
                if (worker.Manager != null
                    && worker.Manager.CurrentStateType == AWorkerState.TypeEnum.Attack
                    && worker.AttackTarget is AEnemy target
                    && enemyIndex.ContainsKey(target))
                {
                    AddEdge(worker, target);
                }
            }

            // 补扫：Enemy 锁定 Worker（追击中）或最近被 Worker 打过（反击间隙）
            foreach (AEnemy enemy in enemies)
            {
                if (enemy.Target is AWorker hunted && workerIndex.ContainsKey(hunted))
                {
                    AddEdge(hunted, enemy);
                }

                if (enemy.LastAttacker is AWorker attacker && workerIndex.ContainsKey(attacker))
                {
                    AddEdge(attacker, enemy);
                }
            }

            if (edges.Count == 0)
            {
                return;
            }

            // 连通分量聚合：BFS（节点 = worker 下标 + enemy 下标，边 = edges）
            bool[,] adjacency = new bool[workers.Count, enemies.Count];
            foreach (long edge in edges)
            {
                int wi = (int)(edge / 1000003L);
                int ei = (int)(edge % 1000003L);
                adjacency[wi, ei] = true;
            }

            bool[] workerVisited = new bool[workers.Count];
            bool[] enemyVisited = new bool[enemies.Count];
            for (int start = 0; start < workers.Count; start++)
            {
                if (workerVisited[start])
                {
                    continue;
                }

                BattleEncounter encounter = new BattleEncounter();
                Queue<int> workerQueue = new Queue<int>();
                workerQueue.Enqueue(start);
                workerVisited[start] = true;
                while (workerQueue.Count > 0)
                {
                    int wi = workerQueue.Dequeue();
                    encounter.Workers.Add(workers[wi]);
                    for (int ei = 0; ei < enemies.Count; ei++)
                    {
                        if (enemyVisited[ei] || !adjacency[wi, ei])
                        {
                            continue;
                        }

                        enemyVisited[ei] = true;
                        encounter.Enemies.Add(enemies[ei]);
                        for (int wj = 0; wj < workers.Count; wj++)
                        {
                            if (!workerVisited[wj] && adjacency[wj, ei])
                            {
                                workerVisited[wj] = true;
                                workerQueue.Enqueue(wj);
                            }
                        }
                    }
                }

                if (encounter.Enemies.Count > 0)
                {
                    this.Encounters.Add(encounter);
                }
            }
        }

        private static List<AWorker> GetAliveWorkers()
        {
            List<AWorker> result = new List<AWorker>();
            if (ServiceLocator.TryGet(out WorkerManager wm) && wm.Characters != null)
            {
                foreach (AWorker worker in wm.Characters)
                {
                    if (worker != null && worker.CharacterDataLAB != null && worker.CharacterDataLAB.Hp > 0f)
                    {
                        result.Add(worker);
                    }
                }
            }

            return result;
        }

        private static List<AEnemy> GetAliveEnemies()
        {
            List<AEnemy> result = new List<AEnemy>();
            if (ServiceLocator.TryGet(out EnemyManager em) && em.Characters != null)
            {
                foreach (AEnemy enemy in em.Characters)
                {
                    if (enemy != null && enemy.CharacterDataLAB != null && enemy.CharacterDataLAB.Hp > 0f)
                    {
                        result.Add(enemy);
                    }
                }
            }

            return result;
        }
    }
}
