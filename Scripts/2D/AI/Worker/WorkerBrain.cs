namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
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

        /// <summary>自己去捡地上属于自己的物品</summary>
        SelfCarry,

        /// <summary>去任务栏取回属于自己的悬赏物品</summary>
        PickUpFromBoard,
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
        public Decision Decide(AWorker worker)
        {
            if (worker == null)
                return Decision.Make(WorkerDecisionType.Idle, "Worker is null");

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
                return Decision.Make(WorkerDecisionType.Idle, "WorkerData is null");

            WorkerPersonality p = workerData.Personality;

            // === 第1层：生存优先 ===

            if (workerData.CurHungry < this.HungryThreshold)
            {
                if (workerData.Wallet.HasEnough(new CurrencyAmount(5)))
                    return Decision.Make(WorkerDecisionType.Eat,
                        $"饥饿({workerData.CurHungry:F0}/{workerData.MaxHungry:F0}), 找食物");

                ResourceCandidate? foodCandidate = this.ScanForFood(worker);
                if (foodCandidate.HasValue)
                    return Decision.MakeGather(foodCandidate.Value.Position, foodCandidate.Value.Resource, "饥饿且没钱，自己采集食物");

                return Decision.Make(WorkerDecisionType.Idle, "饥饿但找不到食物");
            }

            if (workerData.CurTired < this.TiredThreshold)
                return Decision.Make(WorkerDecisionType.Sleep,
                    $"疲劳({workerData.CurTired:F0}/{workerData.MaxTired:F0}), 需要休息");

            // === 定期刷新目标 ===
            this.RefreshGoal(workerData);

            // === 优先：地上有自己悬赏得来的物品 → 去捡 ===
            if (p.Diligence > 35f)
            {
                Decision? carryDecision = this.TryMakeSelfCarryDecision(worker);
                if (carryDecision.HasValue) return carryDecision.Value;
            }

            // === 优先：任务栏有自己悬赏的物品 → 去任务栏取 ===
            if (p.Diligence > 30f)
            {
                Decision? boardDecision = this.TryMakePickUpFromBoardDecision(worker);
                if (boardDecision.HasValue) return boardDecision.Value;
            }

            // === 预扫描：一次扫描供后续复用 ===
            ResourceCandidate? nearbyResource = this.ScanForResources(worker);
            bool canAfford = workerData.Wallet.HasEnough(
                this.DetermineMinReward() + this.MinimumWalletReserve);

            // === 目标驱动悬赏：Worker 缺什么就发什么悬赏 ===
            Decision? goalBounty = this.TryMakeGoalDrivenBounty(worker, workerData, canAfford);
            if (goalBounty.HasValue) return goalBounty.Value;

            // === 第2层：一般性赚钱/干活 ===
            bool hasNearbyResource = nearbyResource.HasValue;
            float selfProb = this.CalculateSelfGatherProbability(p);
            float postProb = this.CalculatePostBountyProbability(p);

            // 有钱+社交 → 发悬赏让别人干
            if (canAfford && p.Sociality > this.SocialityThreshold && hasNearbyResource && Random.value < postProb)
            {
                return new Decision
                {
                    Type = WorkerDecisionType.PostBounty,
                    TargetPosition = nearbyResource.Value.Position,
                    Resource = nearbyResource.Value.Resource,
                    Description = $"社交({p.Sociality:F0})倾向发布悬赏",
                };
            }

            // 事业型：自己干
            if (p.Ambition > this.AmbitionThreshold && p.Mood > this.MoodLowThreshold && hasNearbyResource && Random.value < selfProb)
            {
                return Decision.MakeGather(nearbyResource.Value.Position, nearbyResource.Value.Resource,
                    $"事业心({p.Ambition:F0})驱使自主采集");
            }

            // 有钱+社交 → 发悬赏兜底
            if (canAfford && p.Sociality > this.SocialityThreshold && hasNearbyResource)
            {
                return new Decision
                {
                    Type = WorkerDecisionType.PostBounty,
                    TargetPosition = nearbyResource.Value.Position,
                    Resource = nearbyResource.Value.Resource,
                    Description = $"自己不想干，发悬赏",
                };
            }

            // === 第3层：勤劳接单 ===
            float acceptProb = this.CalculateAcceptBountyProbability(p);
            if (p.Diligence > this.DiligenceThreshold && Random.value < acceptProb)
                return Decision.Make(WorkerDecisionType.AcceptBounty, $"勤奋({p.Diligence:F0})驱使接悬赏");

            // === 第4层：默认 ===
            if (p.Mood < this.MoodLowThreshold && workerData.CurTired < 50f)
                return Decision.Make(WorkerDecisionType.Sleep, $"心情差({p.Mood:F0}), 休息调整");

            return Decision.Make(WorkerDecisionType.Idle, $"无特别需求, {p}");
        }

        // ---- 目标系统 ----

        /// <summary>目标刷新间隔（决策次数）</summary>
        private int goalRefreshCounter;

        /// <summary>
        /// 根据人格定期刷新 Worker 目标。
        /// 高事业心 → 想盖房；中等 → 囤粮；低 → 赚钱。
        /// </summary>
        private void RefreshGoal(AWorker.WorkerData workerData)
        {
            if (++this.goalRefreshCounter % 5 != 0) return; // 每5次决策刷新一次

            WorkerPersonality p = workerData.Personality;

            if (p.Ambition > 65 && workerData.Wallet.Gold >= 30)
            {
                // 大老板：想盖房 → 需要木材+石材
                var materials = new System.Collections.Generic.Dictionary<int, int>();
                // 尝试从 ItemDataManager 获取常见建材 ID
                materials[0] = 10; // 占位：实际需要根据项目配置
                workerData.CurrentGoal = WorkerGoal.BuildStructure("想盖新房", materials);
            }
            else if (p.Ambition > 50 && p.Sociality > 50)
            {
                workerData.CurrentGoal = WorkerGoal.StockFood(3);
            }
            else
            {
                workerData.CurrentGoal = WorkerGoal.EarnMoney();
            }
        }

        /// <summary>
        /// 目标驱动的悬赏：Worker 缺什么材料就发布什么悬赏。
        /// 返回 null 表示没有目标驱动需求，走一般逻辑。
        /// </summary>
        private Decision? TryMakeGoalDrivenBounty(AWorker worker, AWorker.WorkerData wd, bool canAfford)
        {
            if (!canAfford) return null;
            WorkerGoal goal = wd.CurrentGoal;

            // 赚钱目标 → 没有特定材料需求，走一般逻辑
            if (goal.Type == WorkerGoalType.EarnMoney || !goal.HasMaterialNeeds)
                return null;

            // 找到第一个缺失的材料
            foreach (var kv in goal.RequiredMaterials)
            {
                int neededId = kv.Key;
                int neededCount = kv.Value;

                // 检查自己身上+仓库有没有
                int have = worker.GetResourceCountById(neededId);
                if (worker.HasInStorage(neededId, 1))
                {
                    var stored = worker.GetStorageResources();
                    foreach (var s in stored)
                        if (s.Id == neededId) { have += s.Count; break; }
                }

                if (have >= neededCount) continue; // 够了，看下一个

                // 缺这个材料 → 扫描地图上有没有这个资源
                ResourceCandidate? candidate = this.ScanForSpecificResource(worker, neededId);
                if (candidate.HasValue)
                {
                    return new Decision
                    {
                        Type = WorkerDecisionType.PostBounty,
                        TargetPosition = candidate.Value.Position,
                        Resource = candidate.Value.Resource,
                        Description = $"目标「{goal.Description}」缺材料(id={neededId}), 发悬赏求购",
                    };
                }

                // 地图上没有这个资源，跳过
            }

            // 有目标但找不到需要的资源 → 退化为一般采集
            return null;
        }

        /// <summary>扫描特定物品ID的资源。</summary>
        private ResourceCandidate? ScanForSpecificResource(AWorker worker, int targetItemId)
        {
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            if (resourceMap?.ResourceMapDataLAB == null) return null;

            for (int dx = -this.ScanRadius; dx <= this.ScanRadius; dx++)
            {
                for (int dy = -this.ScanRadius; dy <= this.ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                    if (!resourceMap.ResourceMapDataLAB.PosMap.ContainsKey(posLAB)) continue;

                    string resName = resourceMap.ResourceMapDataLAB.PosMap[posLAB];
                    if (string.IsNullOrEmpty(resName)) continue;
                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resName, out ItemData itemData)) continue;
                    if (itemData.Id != targetItemId) continue;

                    return new ResourceCandidate
                    {
                        Position = pos,
                        Resource = new ResourceInfo(itemData.Id),
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 扫描地上属于该 Worker 的物品，创建 Carry 任务捡回来。
        /// 用于悬赏完成后，发布者去捡属于自己的资源。
        /// </summary>
        private Decision? TryMakeSelfCarryDecision(AWorker worker)
        {
            int ownerId = worker.GetInstanceID();
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

            var dropManager = Core.ServiceLocator.Get<DropManager>();
            if (dropManager == null) return null;

            // 扫描周围是否有属于该 Worker 的掉落物
            for (int dx = -this.ScanRadius; dx <= this.ScanRadius; dx++)
            {
                for (int dy = -this.ScanRadius; dy <= this.ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    ResourceInfo drop = dropManager.GetDropByAll(pos);
                    if (drop == null || drop.Count <= 0) continue;
                    if (drop.OwnerId != ownerId) continue; // 不是自己的

                    // 找到了！去捡
                    return new Decision
                    {
                        Type = WorkerDecisionType.SelfCarry,
                        TargetPosition = pos,
                        Resource = new ResourceInfo(drop.Id, drop.Count, drop.OwnerId),
                        Description = $"捡回属于自己的物品(id={drop.Id})",
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 检查任务栏处是否有属于自己的悬赏物品，创建去任务栏拾取的决策。
        /// </summary>
        private Decision? TryMakePickUpFromBoardDecision(AWorker worker)
        {
            int ownerId = worker.GetInstanceID();

            var boardManager = Core.ServiceLocator.Get<Gameplay.TaskBoardManager>();
            if (boardManager == null || !boardManager.IsInitialized)
            {
                return null;
            }

            if (!boardManager.HasDeliveredItems(ownerId))
            {
                return null;
            }

            // 目标位置：任务栏四周的相邻位置
            Vector3Int neighborPos = boardManager.GetNeighborPosition();

            return new Decision
            {
                Type = WorkerDecisionType.PickUpFromBoard,
                TargetPosition = neighborPos,
                Resource = null,
                Description = $"去任务栏取回属于自己的物品",
            };
        }

        // ---- 概率计算 ----

        private float CalculateSelfGatherProbability(WorkerPersonality p)
        {
            float baseProb = 0.25f;
            baseProb += (p.Ambition - 50f) * this.AmbitionSelfGatherBonus;
            baseProb += (p.Mood - 50f) * this.MoodSelfGatherBonus;
            baseProb += (p.Diligence - 50f) * 0.003f;
            return Mathf.Clamp01(baseProb);
        }

        private float CalculatePostBountyProbability(WorkerPersonality p)
        {
            float baseProb = 0.25f;
            baseProb += (p.Sociality - 50f) * this.SocialityPostBountyBonus;
            baseProb -= (p.Diligence - 50f) * 0.002f;
            baseProb += (p.Ambition - 50f) * 0.002f;
            return Mathf.Clamp01(baseProb);
        }

        private float CalculateAcceptBountyProbability(WorkerPersonality p)
        {
            float baseProb = 0.20f;
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
