namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Map;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 行为决策类型。
    /// </summary>
    public enum WorkerDecisionType
    {
        /// <summary>什么都不做，等待</summary>
        Idle,

        /// <summary>自己采集资源</summary>
        SelfGather,

        /// <summary>发布悬赏让别人采集</summary>
        PostBounty,

        /// <summary>接受已有悬赏</summary>
        AcceptBounty,

        /// <summary>吃饭恢复饥饿</summary>
        Eat,

        /// <summary>睡觉恢复疲劳</summary>
        Sleep,
    }

    /// <summary>
    /// Worker 自主决策引擎 — 综合人格、状态、环境信息决定下一步行动。
    /// 纯 C# 服务，不依赖 MonoBehaviour，可在测试中独立实例化。
    ///
    /// 决策优先级（从高到低）：
    /// 1. 生存优先 — 太饿→吃，太累→睡
    /// 2. 赚钱驱动 — 事业心驱使自己干活
    /// 3. 社交代劳 — 有钱+社交→发悬赏
    /// 4. 勤劳接单 — 勤奋→接别人的悬赏
    /// 5. 默认空闲 — 锻炼/等待
    /// </summary>
    public class WorkerBrain
    {
        // ---- 阈值配置 ----

        /// <summary>饥饿阈值：低于此值优先找食物</summary>
        public float HungryThreshold = 30f;

        /// <summary>疲劳阈值：低于此值优先睡觉</summary>
        public float TiredThreshold = 30f;

        /// <summary>事业心阈值：高于此值驱动自主赚钱</summary>
        public float AmbitionThreshold = 50f;

        /// <summary>社交阈值：高于此值倾向发悬赏/接悬赏</summary>
        public float SocialityThreshold = 45f;

        /// <summary>勤奋阈值：高于此值倾向主动工作</summary>
        public float DiligenceThreshold = 45f;

        /// <summary>心情低阈值：低于此值倾向休息消费</summary>
        public float MoodLowThreshold = 35f;

        /// <summary>最小钱包余额（发悬赏后保留）</summary>
        public CurrencyAmount MinimumWalletReserve = new CurrencyAmount(20);

        /// <summary>环境扫描半径（地图格子）</summary>
        public int ScanRadius = 20;

        // ---- 人格权重配置 ----

        /// <summary>心情对自我采集概率的加成（每点偏差）</summary>
        public float MoodSelfGatherBonus = 0.003f;

        /// <summary>事业心对自我采集概率的加成</summary>
        public float AmbitionSelfGatherBonus = 0.005f;

        /// <summary>社交对发布悬赏概率的加成</summary>
        public float SocialityPostBountyBonus = 0.005f;

        /// <summary>勤奋对接悬赏概率的加成</summary>
        public float DiligenceAcceptBountyBonus = 0.005f;

        // ---- 决策结果 ----

        /// <summary>
        /// 决策结果 — 包含决策类型和可选的候选位置/资源。
        /// </summary>
        public struct Decision
        {
            public WorkerDecisionType Type;
            public Vector3Int TargetPosition;
            public ResourceInfo Resource;
            public string Description;

            public static Decision Make(WorkerDecisionType type, string desc = "")
            {
                return new Decision { Type = type, Description = desc };
            }

            public static Decision MakeGather(Vector3Int pos, ResourceInfo resource, string desc = "")
            {
                return new Decision
                {
                    Type = WorkerDecisionType.SelfGather,
                    TargetPosition = pos,
                    Resource = resource,
                    Description = desc,
                };
            }
        }

        /// <summary>
        /// 轻量候选 — 扫描时收集的环境资源信息。
        /// </summary>
        private struct ResourceCandidate
        {
            public Vector3Int Position;
            public ResourceInfo Resource;
        }

        // ---- 核心决策 ----

        /// <summary>
        /// 综合决策：根据 Worker 状态和人格决定下一步行动。
        /// </summary>
        /// <param name="worker">Worker 实例</param>
        /// <returns>决策结果</returns>
        public Decision Decide(AWorker worker)
        {
            if (worker == null)
            {
                return Decision.Make(WorkerDecisionType.Idle, "Worker is null");
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return Decision.Make(WorkerDecisionType.Idle, "WorkerData is null");
            }

            WorkerPersonality p = workerData.Personality;

            // === 第1层：生存优先 ===

            // 饥饿 → 吃东西
            if (workerData.CurHungry < this.HungryThreshold)
            {
                // 有钱就买食物，没钱自己去采集食物
                if (workerData.Wallet.HasEnough(new CurrencyAmount(5)))
                {
                    return Decision.Make(WorkerDecisionType.Eat,
                        $"饥饿({workerData.CurHungry:F0}/{workerData.MaxHungry:F0}), 找食物");
                }

                // 没钱，自己去采集食物
                ResourceCandidate? foodCandidate = this.ScanForFood(worker);
                if (foodCandidate.HasValue)
                {
                    return Decision.MakeGather(foodCandidate.Value.Position, foodCandidate.Value.Resource,
                        $"饥饿且没钱，自己采集食物");
                }

                return Decision.Make(WorkerDecisionType.Idle, "饥饿但找不到食物");
            }

            // 疲劳 → 睡觉
            if (workerData.CurTired < this.TiredThreshold)
            {
                return Decision.Make(WorkerDecisionType.Sleep,
                    $"疲劳({workerData.CurTired:F0}/{workerData.MaxTired:F0}), 需要休息");
            }

            // === 第2层：赚钱驱动（事业心 + 好心情） ===

            float selfGatherProb = this.CalculateSelfGatherProbability(p);
            bool wantsToWork = p.Ambition > this.AmbitionThreshold && p.Mood > this.MoodLowThreshold;

            if (wantsToWork && Random.value < selfGatherProb)
            {
                ResourceCandidate? candidate = this.ScanForResources(worker);
                if (candidate.HasValue)
                {
                    return Decision.MakeGather(candidate.Value.Position, candidate.Value.Resource,
                        $"事业心({p.Ambition:F0})驱使自主采集");
                }
            }

            // === 第3层：社交代劳（有钱 + 社交倾向 + 不想自己干） ===

            float postBountyProb = this.CalculatePostBountyProbability(p);
            bool canAffordBounty = workerData.Wallet.HasEnough(
                this.DetermineMinReward() + this.MinimumWalletReserve);

            if (canAffordBounty && p.Sociality > this.SocialityThreshold && Random.value < postBountyProb)
            {
                ResourceCandidate? candidate = this.ScanForResources(worker);
                if (candidate.HasValue)
                {
                    return new Decision
                    {
                        Type = WorkerDecisionType.PostBounty,
                        TargetPosition = candidate.Value.Position,
                        Resource = candidate.Value.Resource,
                        Description = $"社交({p.Sociality:F0})倾向发布悬赏",
                    };
                }
            }

            // === 第4层：勤劳接单（勤奋 + 空闲） ===

            float acceptBountyProb = this.CalculateAcceptBountyProbability(p);
            if (p.Diligence > this.DiligenceThreshold && Random.value < acceptBountyProb)
            {
                return Decision.Make(WorkerDecisionType.AcceptBounty,
                    $"勤奋({p.Diligence:F0})驱使接悬赏");
            }

            // === 第5层：默认 ===

            // 心情极差时优先休息
            if (p.Mood < this.MoodLowThreshold && workerData.CurTired < 50f)
            {
                return Decision.Make(WorkerDecisionType.Sleep, $"心情差({p.Mood:F0}), 休息调整");
            }

            return Decision.Make(WorkerDecisionType.Idle, $"无特别需求, {p}");
        }

        // ---- 概率计算 ----

        private float CalculateSelfGatherProbability(WorkerPersonality p)
        {
            float baseProb = 0.15f;
            baseProb += (p.Ambition - 50f) * this.AmbitionSelfGatherBonus;
            baseProb += (p.Mood - 50f) * this.MoodSelfGatherBonus;
            baseProb += (p.Diligence - 50f) * 0.003f;
            return Mathf.Clamp01(baseProb);
        }

        private float CalculatePostBountyProbability(WorkerPersonality p)
        {
            float baseProb = 0.10f;
            baseProb += (p.Sociality - 50f) * this.SocialityPostBountyBonus;
            baseProb -= (p.Diligence - 50f) * 0.002f; // 勤奋的人更倾向自己干
            baseProb += (p.Ambition - 50f) * 0.002f;   // 事业心强也倾向发悬赏（高效）
            return Mathf.Clamp01(baseProb);
        }

        private float CalculateAcceptBountyProbability(WorkerPersonality p)
        {
            float baseProb = 0.10f;
            baseProb += (p.Diligence - 50f) * this.DiligenceAcceptBountyBonus;
            baseProb += (p.Sociality - 50f) * 0.003f;
            return Mathf.Clamp01(baseProb);
        }

        // ---- 环境扫描 ----

        /// <summary>
        /// 扫描周围可采集的资源。
        /// </summary>
        private ResourceCandidate? ScanForResources(AWorker worker)
        {
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();

            if (resourceMap == null || resourceMap.ResourceMapDataLAB == null)
            {
                return null;
            }

            List<ResourceCandidate> candidates = new List<ResourceCandidate>();

            for (int dx = -this.ScanRadius; dx <= this.ScanRadius; dx++)
            {
                for (int dy = -this.ScanRadius; dy <= this.ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);

                    if (!resourceMap.ResourceMapDataLAB.PosMap.ContainsKey(posLAB))
                    {
                        continue;
                    }

                    string resourceName = resourceMap.ResourceMapDataLAB.PosMap[posLAB];
                    if (string.IsNullOrEmpty(resourceName))
                    {
                        continue;
                    }

                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resourceName, out ItemData itemData))
                    {
                        continue;
                    }

                    candidates.Add(new ResourceCandidate
                    {
                        Position = pos,
                        Resource = new ResourceInfo(itemData.Id),
                    });
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // 返回最近的一个
            ResourceCandidate best = candidates[0];
            float bestDist = (best.Position - workerPos).sqrMagnitude;
            for (int i = 1; i < candidates.Count; i++)
            {
                float dist = (candidates[i].Position - workerPos).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidates[i];
                }
            }

            return best;
        }

        /// <summary>
        /// 扫描周围可采集的食物资源。
        /// </summary>
        private ResourceCandidate? ScanForFood(AWorker worker)
        {
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();

            if (resourceMap == null || resourceMap.ResourceMapDataLAB == null)
            {
                return null;
            }

            ResourceCandidate? best = null;
            float bestDist = float.MaxValue;

            for (int dx = -this.ScanRadius; dx <= this.ScanRadius; dx++)
            {
                for (int dy = -this.ScanRadius; dy <= this.ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);

                    if (!resourceMap.ResourceMapDataLAB.PosMap.ContainsKey(posLAB))
                    {
                        continue;
                    }

                    string resourceName = resourceMap.ResourceMapDataLAB.PosMap[posLAB];
                    if (string.IsNullOrEmpty(resourceName))
                    {
                        continue;
                    }

                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resourceName, out ItemData itemData))
                    {
                        continue;
                    }

                    // 检查是否为食物类型
                    AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(itemData.Id);
                    if (itemType != AItem.ItemTypeEnum.Food)
                    {
                        continue;
                    }

                    float dist = (pos - workerPos).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = new ResourceCandidate
                        {
                            Position = pos,
                            Resource = new ResourceInfo(itemData.Id),
                        };
                    }
                }
            }

            return best;
        }

        // ---- 工具方法 ----

        private CurrencyAmount DetermineMinReward()
        {
            return new CurrencyAmount(5);
        }

        /// <summary>
        /// 获取决策的可读描述（供 UI/日志使用）。
        /// </summary>
        public static string GetDecisionLabel(Decision decision)
        {
            return $"[{decision.Type}] {decision.Description}";
        }
    }
}
