namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Item;
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

        /// <summary>自己去建造</summary>
        SelfBuild,

        /// <summary>自己去种植</summary>
        SelfPlant,

        /// <summary>漫游休息 — 恢复精气神和心情</summary>
        Wander,

        /// <summary>地面睡眠 — 无床时的低效睡眠</summary>
        GroundSleep,
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

        /// <summary>精气神阈值：低于此值优先漫游恢复</summary>
        public float SpiritThreshold = 30f;

        /// <summary>漫游基础概率</summary>
        public float WanderBaseChance = 0.12f;

        /// <summary>Bootstrap 阶段食物囤积目标</summary>
        public int BootstrapFoodTarget = 3;

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

            /// <summary>建造任务需要的材料清单（仅 Build 类型使用）</summary>
            public Dictionary<int, ResourceInfo> NeededResources;

            /// <summary>建造物品的 Tile 名称（仅 Build 类型使用）</summary>
            public string BuildTileName;

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

            /// <summary>创建建造决策（自己建造或发悬赏建造）。</summary>
            public static Decision MakeBuild(WorkerDecisionType type, Vector3Int buildPos, string tileName,
                Dictionary<int, ResourceInfo> needs, string desc = "")
            {
                return new Decision
                {
                    Type = type,
                    TargetPosition = buildPos,
                    BuildTileName = tileName,
                    NeededResources = needs,
                    Description = desc,
                };
            }

            /// <summary>创建种植决策。</summary>
            public static Decision MakePlant(WorkerDecisionType type, Vector3Int farmlandPos, string desc = "")
            {
                return new Decision
                {
                    Type = type,
                    TargetPosition = farmlandPos,
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

        /// <summary>
        /// 建造候选 — 扫描时收集的待建造位置信息。
        /// </summary>
        private struct BuildCandidate
        {
            public Vector3Int Position;
            public string TileName;
            public Dictionary<int, ResourceInfo> NeededResources;
        }

        /// <summary>
        /// 种植候选 — 扫描时收集的可种植农田信息。
        /// </summary>
        private struct PlantCandidate
        {
            public Vector3Int Position;
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
            // 饥饿优先级：越低越紧急，饥饿<15 时跳过其他所有决策
            if (workerData.CurHungry < this.HungryThreshold)
            {
                // 优先检查自己身上/仓库是否有食物 → 直接吃，不需要扫描地图或交易
                if (this.WorkerHasFood(worker))
                    return Decision.Make(WorkerDecisionType.Eat,
                        $"饥饿({workerData.CurHungry:F0}), 吃自己携带/仓库的食物");

                // 扫描周围地上食物（无论有没有钱，食物在附近就先自己采）
                int foodScanRadius = workerData.CurHungry < 15 ? this.ScanRadius * 2 : this.ScanRadius;
                ResourceCandidate? foodCandidate = this.ScanForFood(worker, foodScanRadius);

                if (foodCandidate.HasValue)
                {
                    // 饥饿严重 → 必须自己采
                    if (workerData.CurHungry < 15)
                        return Decision.MakeGather(foodCandidate.Value.Position, foodCandidate.Value.Resource,
                            $"紧急饥饿({workerData.CurHungry:F0}), 自己采集食物");

                    // 有钱+不紧急 → 可以交易或自己采
                    if (workerData.Wallet.HasEnough(new CurrencyAmount(5)))
                        return Decision.Make(WorkerDecisionType.Eat,
                            $"饥饿({workerData.CurHungry:F0}), 有钱优先交易买食物");

                    return Decision.MakeGather(foodCandidate.Value.Position, foodCandidate.Value.Resource,
                        $"饥饿({workerData.CurHungry:F0}), 自己采集食物");
                }

                // 身上/仓库/附近地上都没食物 → 有钱就尝试交易购买
                if (workerData.Wallet.HasEnough(new CurrencyAmount(5)))
                    return Decision.Make(WorkerDecisionType.Eat,
                        $"饥饿({workerData.CurHungry:F0}), 附近无食物, 尝试交易");

                // 没钱+没食物 → 扩大扫描范围最后尝试
                foodCandidate = this.ScanForFood(worker, this.ScanRadius * 3);
                if (foodCandidate.HasValue)
                    return Decision.MakeGather(foodCandidate.Value.Position, foodCandidate.Value.Resource,
                        $"饥饿({workerData.CurHungry:F0}), 远距离采集食物");

                return Decision.Make(WorkerDecisionType.Idle, "饥饿但找不到食物");
            }

            if (workerData.CurTired < this.TiredThreshold)
            {
                // 疲劳时：有床→Sleep，无床→GroundSleep
                if (worker.BedItem != null)
                    return Decision.Make(WorkerDecisionType.Sleep,
                        $"疲劳({workerData.CurTired:F0}/{workerData.MaxTired:F0}), 有床睡眠");
                else
                    return Decision.Make(WorkerDecisionType.GroundSleep,
                        $"疲劳({workerData.CurTired:F0}/{workerData.MaxTired:F0}), 地面睡眠");
            }

            // === 精气神过低 → 优先漫游或休息 ===
            if (workerData.CurSpirit < this.SpiritThreshold)
            {
                return Decision.Make(WorkerDecisionType.Wander,
                    $"精气神低({workerData.CurSpirit:F0}), 漫游恢复");
            }

            // === TIER 1: Bootstrap 阶段专属决策 ===
            if (workerData.LifeStage == Domain.Worker.WorkerLifeStage.Bootstrap)
            {
                return this.DecideBootstrap(worker, workerData, p);
            }

            // === 定期刷新目标 ===
            this.RefreshGoal(workerData);

            // 阶段升级：Settled + 食物≥5 + 金钱≥200 → Established
            if (workerData.LifeStage == Domain.Worker.WorkerLifeStage.Settled
                && workerData.HomePosition != null)
            {
                int foodCount = this.CountFoodStockpile(worker);
                if (foodCount >= 5 && workerData.Wallet.Gold >= 200)
                {
                    workerData.LifeStage = Domain.Worker.WorkerLifeStage.Established;
                    workerData.FoodStockpileTarget = 5;
                    AWorkerTask.LogProvider(
                        $"{worker.name} 升级到 Established 阶段!",
                        LogManager.LogLevelEnum.Info);
                }
            }

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

            // === 建造决策：无家者始终尝试建家（不受 Ambition 门控），有家者 Ambition>65 才扩建 ===
            {
                Decision? buildDecision = this.TryMakeSelfBuildDecision(worker, workerData, canAfford);
                if (buildDecision.HasValue) return buildDecision.Value;
            }

            // === 目标驱动悬赏：Worker 缺什么就发什么悬赏 ===
            // 建造决策未触发（缺材料/缺钱）时，通过悬赏补充材料（需通过悬赏门槛）
            if (this.CanPostBounty(worker, workerData, canAfford))
            {
                Decision? goalBounty = this.TryMakeGoalDrivenBounty(worker, workerData, canAfford);
                if (goalBounty.HasValue) return goalBounty.Value;
            }

            // === 种植决策：有种子时扫描附近空闲农田 ===
            if (p.Diligence > this.DiligenceThreshold)
            {
                Decision? plantDecision = this.TryMakeSelfPlantDecision(worker);
                if (plantDecision.HasValue) return plantDecision.Value;
            }

            // === 第2层：一般性赚钱/干活 ===
            bool hasNearbyResource = nearbyResource.HasValue;
            float selfProb = this.CalculateSelfGatherProbability(p);
            float postProb = this.CalculatePostBountyProbability(p);
            bool canPostBounty = this.CanPostBounty(worker, workerData, canAfford);

            // 有钱+社交 → 发悬赏让别人干（需通过悬赏门槛）
            if (canPostBounty && p.Sociality > this.SocialityThreshold && hasNearbyResource && Random.value < postProb)
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

            // === 第3层：勤劳接单 ===
            float acceptProb = this.CalculateAcceptBountyProbability(p);
            if (p.Diligence > this.DiligenceThreshold && Random.value < acceptProb)
                return Decision.Make(WorkerDecisionType.AcceptBounty, $"勤奋({p.Diligence:F0})驱使接悬赏");

            // === 第4层：漫游或默认 ===
            // 精气神低或心情差 → 漫游恢复
            if (workerData.CurSpirit < this.SpiritThreshold
                || p.Mood < this.MoodLowThreshold)
            {
                return Decision.Make(WorkerDecisionType.Wander,
                    $"精气神({workerData.CurSpirit:F0})心情({p.Mood:F0})需要调整, 漫游休息");
            }

            // 小概率漫游（12% 基础 + 心情/精气神修正）
            float wanderChance = this.WanderBaseChance;
            wanderChance += (50f - p.Mood) * 0.003f;
            wanderChance += Mathf.Max(0f, (50f - workerData.CurSpirit)) * 0.002f;
            wanderChance = Mathf.Clamp01(wanderChance);
            if (Random.value < wanderChance)
                return Decision.Make(WorkerDecisionType.Wander, "小概率漫游, 转换心情");

            if (p.Mood < this.MoodLowThreshold && workerData.CurTired < 50f)
                return Decision.Make(WorkerDecisionType.Sleep, $"心情差({p.Mood:F0}), 休息调整");

            return Decision.Make(WorkerDecisionType.Idle, $"无特别需求, {p}");
        }

        // ---- Bootstrap 阶段决策 ----

        /// <summary>
        /// Bootstrap 阶段专属决策：采集食物→采集建材→建家→建床。
        /// 绝对不发悬赏，不受 Ambition 门控。
        /// </summary>
        private Decision DecideBootstrap(AWorker worker, AWorker.WorkerData wd, WorkerPersonality p)
        {
            // 1. 饥饿 < 50 → 先吃饱（比正常阈值30更早，保持充足）
            if (wd.CurHungry < 50f)
            {
                if (this.WorkerHasFood(worker))
                    return Decision.Make(WorkerDecisionType.Eat,
                        $"Bootstrap: 饥饿({wd.CurHungry:F0}), 吃自带食物");

                ResourceCandidate? food = this.ScanForFood(worker);
                if (food.HasValue)
                    return Decision.MakeGather(food.Value.Position, food.Value.Resource,
                        $"Bootstrap: 采集食物({wd.CurHungry:F0})");
            }

            // 2. 疲劳 < 50 → 睡觉
            if (wd.CurTired < 50f)
                return Decision.Make(WorkerDecisionType.Sleep,
                    $"Bootstrap: 疲劳({wd.CurTired:F0}), 休息");

            // 3. 精气神 < 30 → 漫游恢复
            if (wd.CurSpirit < this.SpiritThreshold)
                return Decision.Make(WorkerDecisionType.Wander,
                    $"Bootstrap: 精气神低({wd.CurSpirit:F0}), 漫游");

            // 4. 仓库食物不足 → 采集食物
            int foodCount = this.CountFoodStockpile(worker);
            if (foodCount < this.BootstrapFoodTarget)
            {
                ResourceCandidate? food = this.ScanForFood(worker);
                if (food.HasValue)
                    return Decision.MakeGather(food.Value.Position, food.Value.Resource,
                        $"Bootstrap: 囤食物({foodCount}/{this.BootstrapFoodTarget})");
            }

            // 5. 尝试建造（不受 Ambition 门控！）
            bool canAfford = wd.Wallet.HasEnough(
                this.DetermineMinReward() + this.MinimumWalletReserve);
            Decision? buildDecision = this.TryMakeSelfBuildDecision(worker, wd, canAfford);
            if (buildDecision.HasValue) return buildDecision.Value;

            // 6. 建造条件不满足 → 采集建材
            ResourceCandidate? nearbyResource = this.ScanForResources(worker);
            if (nearbyResource.HasValue)
                return Decision.MakeGather(nearbyResource.Value.Position, nearbyResource.Value.Resource,
                    "Bootstrap: 采集建材");

            // 7. 无资源可采 → 漫游探索
            return Decision.Make(WorkerDecisionType.Wander, "Bootstrap: 漫游探索资源");
        }

        /// <summary>
        /// 悬赏门槛检查：只有 Settled 及以上阶段才能发布悬赏。
        /// </summary>
        private bool CanPostBounty(AWorker worker, AWorker.WorkerData wd, bool canAfford)
        {
            if (!canAfford) return false;
            if (wd.LifeStage < Domain.Worker.WorkerLifeStage.Settled) return false;
            if (wd.HomePosition == null) return false;           // 无家不发悬赏
            if (wd.CurHungry < 40f) return false;                // 饿了不发
            if (wd.CurTired < 40f) return false;                 // 累了不发
            if (wd.CurSpirit < 35f) return false;                // 没精神不发
            int foodCount = this.CountFoodStockpile(worker);
            if (foodCount < wd.FoodStockpileTarget) return false; // 食物储备不足
            return true;
        }

        /// <summary>
        /// 统计 Worker 拥有的食物数量（身上 + 仓库）。
        /// </summary>
        private int CountFoodStockpile(AWorker worker)
        {
            int count = 0;
            foreach (var r in worker.GetAllResources())
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food)
                    count += r.Count;
            }

            foreach (var r in worker.GetStorageResources())
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food)
                    count += r.Count;
            }

            return count;
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

            // 无家者优先：设定建家目标
            if (workerData.HomePosition == null)
            {
                var materials = new System.Collections.Generic.Dictionary<int, int>();
                var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();

                // 尝试获取木材和石材的 ID（常见建材）
                ItemData wood = itemDataManager.GetByName("CustomWood");
                ItemData stone = itemDataManager.GetByName("CustomStone");
                if (wood != null && wood.Id > 0) materials[wood.Id] = 10;
                if (stone != null && stone.Id > 0) materials[stone.Id] = 8;

                // 若都没有则使用默认占位
                if (materials.Count == 0) materials[0] = 10;

                workerData.CurrentGoal = WorkerGoal.BuildStructure("建一个家", materials);
                return;
            }

            WorkerPersonality p = workerData.Personality;

            if (p.Ambition > 65 && workerData.Wallet.Gold >= 30)
            {
                // 大老板：想盖房 → 从 ItemDataManager 获取常见建材 ID
                var materials = new System.Collections.Generic.Dictionary<int, int>();
                var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();

                // 尝试获取木材和石材的 ID（常见建材）
                ItemData wood = itemDataManager.GetByName("CustomWood");
                ItemData stone = itemDataManager.GetByName("CustomStone");
                if (wood != null && wood.Id > 0) materials[wood.Id] = 10;
                if (stone != null && stone.Id > 0) materials[stone.Id] = 8;

                // 若都没有则使用默认占位
                if (materials.Count == 0) materials[0] = 10;

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
            if (goal.Type == WorkerGoalType.EarnMoney)
                return null;

            // 食物目标但没有指定具体材料 → 扫描食物类物品
            if (goal.IsFoodRelated && !goal.HasMaterialNeeds)
            {
                ResourceCandidate? foodCandidate = this.ScanForFood(worker);
                if (foodCandidate.HasValue)
                {
                    return new Decision
                    {
                        Type = WorkerDecisionType.PostBounty,
                        TargetPosition = foodCandidate.Value.Position,
                        Resource = foodCandidate.Value.Resource,
                        Description = $"目标「{goal.Description}」, 发布食物悬赏",
                    };
                }

                return null;
            }

            // 没有材料需求 → 走一般逻辑
            if (!goal.HasMaterialNeeds)
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
        /// <param name="worker">Worker 实例</param>
        /// <param name="radius">扫描半径，默认使用 ScanRadius</param>
        private ResourceCandidate? ScanForFood(AWorker worker, int radius = -1)
        {
            int scanR = radius > 0 ? radius : this.ScanRadius;
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();

            if (resourceMap == null || resourceMap.ResourceMapDataLAB == null)
            {
                return null;
            }

            ResourceCandidate? best = null;
            float bestDist = float.MaxValue;

            for (int dx = -scanR; dx <= scanR; dx++)
            {
                for (int dy = -scanR; dy <= scanR; dy++)
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

        // ---- 建造/种植决策 ----

        // 房间墙壁偏移（5x5 环，Unity 坐标 x=列, y=行，中心为原点）：
        // 中间 3x3 空地放床，墙壁围在第二圈。南侧 (0,-2) 留门。
        // ARoom 使用 x=行/y=列 坐标系（与 Unity 交换了 xy），因此方向需按 ARoom 约定：
        //   _0=左上 _1=上 _2=右上 _3=左 _4=右 _5=左下 _6=下 _7=右下
        private static readonly Vector3Int[] WallOffsets = new Vector3Int[]
        {
            new Vector3Int(-2, 2, 0),  // 0: 左上角 (ARoom: 右下) → _7
            new Vector3Int(-1, 2, 0),  // 1: 上边   (ARoom: 右)   → _4
            new Vector3Int(0, 2, 0),   // 2: 上中   (ARoom: 右)   → _4
            new Vector3Int(1, 2, 0),   // 3: 上边   (ARoom: 右)   → _4
            new Vector3Int(2, 2, 0),   // 4: 右上角 (ARoom: 右上) → _2
            new Vector3Int(2, 1, 0),   // 5: 右边   (ARoom: 上)   → _1
            new Vector3Int(2, 0, 0),   // 6: 右中   (ARoom: 上)   → _1
            new Vector3Int(2, -1, 0),  // 7: 右边   (ARoom: 上)   → _1
            new Vector3Int(2, -2, 0),  // 8: 右下角 (ARoom: 左上) → _0
            new Vector3Int(1, -2, 0),  // 9: 下边右 (ARoom: 左)   → _3 [之前缺失!]
            new Vector3Int(-1, -2, 0), // 10: 下边左 (ARoom: 左)   → _3 [门在(0,-2)]
            new Vector3Int(-2, -2, 0), // 11: 左下角 (ARoom: 左下) → _5
            new Vector3Int(-2, -1, 0), // 12: 左边   (ARoom: 下)   → _6
            new Vector3Int(-2, 0, 0),  // 13: 左中   (ARoom: 下)   → _6
            new Vector3Int(-2, 1, 0),  // 14: 左边   (ARoom: 下)   → _6
        };
        // 对应每块墙的方向编号（按 ARoom 坐标约定）
        private static readonly int[] WallDirections = new int[]
        {
            7,              // 0: 左上角 → _7 (右下)
            4, 4, 4,        // 1-3: 上边 → _4 (右)
            2,              // 4: 右上角 → _2 (右上)
            1, 1, 1,        // 5-7: 右边 → _1 (上)
            0,              // 8: 右下角 → _0 (左上)
            3,              // 9: 下边右 → _3 (左) [新增]
            3,              // 10: 下边左 → _3 (左)
            5,              // 11: 左下角 → _5 (左下)
            6, 6, 6,        // 12-14: 左边 → _6 (下)
        };
        public const int WallCount = 15;           // 15 面墙
        public const int DoorStage = WallCount;    // 15: 门
        public const int BedStage = WallCount + 1; // 16: 床
        public const int CompleteStage = WallCount + 2; // 17: 完成

        /// <summary>获取房间所有墙壁偏移（供外部注册房间使用）。</summary>
        public static IReadOnlyList<Vector3Int> GetWallOffsets() => WallOffsets;

        /// <summary>门相对于中心的偏移。</summary>
        public static readonly Vector3Int DoorOffset = new Vector3Int(0, -2, 0);

        /// <summary>
        /// 自主建造决策：Worker 决定为自己建造房屋。
        /// 无家者：先围墙壁（7块）形成房间（南侧留门），再在中间建床。
        /// 社交型+有钱 → 发建造悬赏；勤奋型+有材料 → 自己建。
        /// </summary>
        private Decision? TryMakeSelfBuildDecision(AWorker worker, AWorker.WorkerData wd, bool canAfford)
        {
            string buildTileName;
            Dictionary<int, ResourceInfo> needs;
            Vector3Int? buildPos;

            // 无家可归 → 围墙壁 + 建床
            if (wd.HomePosition == null)
            {
                Vector3Int center = wd.PlannedHomePosition != null
                    ? Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition)
                    : AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

                if (wd.HomeBuildStage < WallCount)
                {
                    // 阶段 0-14：建墙壁，使用对应方向变体
                    int dir = WallDirections[wd.HomeBuildStage];
                    buildTileName = $"CustomRoomWall_{dir}";
                    buildPos = center + WallOffsets[wd.HomeBuildStage];
                }
                else if (wd.HomeBuildStage == DoorStage)
                {
                    // 阶段 15：建门 (南侧正中间)
                    buildTileName = "CustomDoor";
                    buildPos = center + new Vector3Int(0, -2, 0);
                }
                else if (wd.HomeBuildStage == BedStage)
                {
                    // 阶段 16：建床
                    buildTileName = "SingleBed";
                    buildPos = center;
                }
                else
                {
                    // 阶段 17+：完成
                    return null;
                }

                needs = this.GetBuildMaterialNeeds(buildTileName);
                if (needs == null)
                {
                    // 物品不存在于数据库中 → 尝试降级
                    if (buildTileName.StartsWith("CustomRoomWall"))
                    {
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建造: CustomRoomWall 不存在, 跳过墙壁直接建床",
                            LogManager.LogLevelEnum.Info);
                        wd.HomeBuildStage = BedStage;
                        buildTileName = "SingleBed";
                        buildPos = center;
                        needs = this.GetBuildMaterialNeeds(buildTileName);
                    }

                    if (needs == null)
                    {
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建造失败: {buildTileName} 物品不存在于数据库",
                            LogManager.LogLevelEnum.Info);
                        return null;
                    }
                }

                // 检查建造位置是否可用
                if (!buildPos.HasValue || !ASeek.IsCanReach(buildPos.Value))
                {
                    // 位置被阻挡：检查是否是资源，是则先采集再建造
                    if (buildPos.HasValue)
                    {
                        var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
                        Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(buildPos.Value);
                        if (resourceMap?.ResourceMapDataLAB?.PosMap != null
                            && resourceMap.ResourceMapDataLAB.PosMap.TryGetValue(posLAB, out string resName)
                            && !string.IsNullOrEmpty(resName))
                        {
                            var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();
                            if (itemDataManager.TryGetByName(resName, out ItemData itemData) && itemData.Id > 0)
                            {
                                AWorkerTask.LogProvider(
                                    $"{worker.name} 建造位置有资源 {resName}, 先采集再建 {buildTileName}",
                                    LogManager.LogLevelEnum.Info);
                                return Decision.MakeGather(buildPos.Value,
                                    new ResourceInfo(itemData.Id),
                                    $"清理建造位置: {resName}");
                            }
                        }
                    }

                    // 不是资源阻挡 → 位置本身有问题，清除规划重新选址，避免死循环
                    AWorkerTask.LogProvider(
                        $"{worker.name} 建造位置不可达(非资源阻挡): {buildTileName} pos=({buildPos?.x},{buildPos?.y}), 重新选址",
                        LogManager.LogLevelEnum.Info);
                    wd.PlannedHomePosition = null;
                    wd.HomeBuildStage = 0;
                    this.TryPickHomeSite(worker);
                    // 返回 Wander 触发重新评估，不 fallback 到采集
                    return Decision.Make(WorkerDecisionType.Wander,
                        "建家位置无效, 重新选址后漫游探索");
                }

                // 墙壁阶段检查冲突：仅跳过已完成的墙壁，已注册但未完成的等待其完成
                if (wd.HomeBuildStage < WallCount)
                {
                    var buildMap = Core.ServiceLocator.Get<BuildMap>();
                    if (buildMap?.BuildMapDataLAB?.PosMap != null)
                    {
                        Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(buildPos.Value);
                        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var existingTile)
                            && existingTile.IsComplete)
                        {
                            AWorkerTask.LogProvider(
                                $"{worker.name} 位置已建造完成, 跳过墙壁{wd.HomeBuildStage + 1}: pos=({buildPos.Value.x},{buildPos.Value.y})",
                                LogManager.LogLevelEnum.Info);
                            wd.HomeBuildStage++;
                            return null;
                        }
                    }
                }
            }
            // 有家+高事业 → 扩建（简化：再建一个床/家具）
            else if (wd.Personality.Ambition > 65)
            {
                buildTileName = "SingleBed"; // TODO: 扩展更多建造类型
                needs = this.GetBuildMaterialNeeds(buildTileName);
                if (needs == null || needs.Count == 0) return null;

                buildPos = this.FindFreeBuildPosition(worker);
                if (!buildPos.HasValue)
                {
                    AWorkerTask.LogProvider(
                        $"{worker.name} 想建{buildTileName}但附近无空闲位置",
                        LogManager.LogLevelEnum.Info);
                    return null;
                }
            }
            else
            {
                return null;
            }

            // ---- 共同的决策路径（无家 + 扩建都走这里）----

            // 社交型+有钱 → 发建造悬赏（需通过悬赏门槛，Bootstrap阶段不发）
            if (this.CanPostBounty(worker, wd, canAfford)
                && wd.Personality.Sociality > this.SocialityThreshold
                && Random.value < 0.4f)
            {
                return Decision.MakeBuild(
                    WorkerDecisionType.PostBounty,
                    buildPos.Value,
                    buildTileName,
                    needs,
                    $"发布建造悬赏: 自己的{buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})");
            }

            // 勤奋型 → 自己建（需有足够材料）
            if (wd.Personality.Diligence > this.DiligenceThreshold
                && this.HasEnoughResourcesForBuild(worker, needs))
            {
                return Decision.MakeBuild(
                    WorkerDecisionType.SelfBuild,
                    buildPos.Value,
                    buildTileName,
                    needs,
                    $"自己建造: {buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})");
            }

            // 无家者兜底：有材料就自己建，没材料回退到采集
            if (wd.HomePosition == null)
            {
                if (this.HasEnoughResourcesForBuild(worker, needs))
                {
                    return Decision.MakeBuild(
                        WorkerDecisionType.SelfBuild,
                        buildPos.Value,
                        buildTileName,
                        needs,
                        $"建家: {buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})");
                }

                // 没材料 → 不强行建造，继续走到后面的采集/悬赏逻辑收集材料
            }

            AWorkerTask.LogProvider(
                $"{worker.name} 想建{buildTileName}但条件不满足 (可支付={canAfford} 社交={wd.Personality.Sociality:F0} 勤奋={wd.Personality.Diligence:F0} 材料够={this.HasEnoughResourcesForBuild(worker, needs)})",
                LogManager.LogLevelEnum.Info);

            return null;
        }

        /// <summary>
        /// 根据建造物品名称获取所需材料清单。
        /// </summary>
        private Dictionary<int, ResourceInfo> GetBuildMaterialNeeds(string tileName)
        {
            var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();

            // 尝试原始名字，失败则追加 _0（AutoGenerateDirections 的物品用变体名注册）
            ItemData itemData = itemDataManager.GetByName(tileName);
            if (itemData == null)
            {
                itemData = itemDataManager.GetByName(tileName + "_0");
            }
            if (itemData == null) return null;

            // BuildItemData 且配置了 BuildCosts → 使用配置的材料清单
            if (itemData is BuildItemData buildData && buildData.BuildCosts != null && buildData.BuildCosts.Count > 0)
            {
                return this.BuildResourceDictFromData(buildData);
            }

            // 物品存在但没有配置材料清单 → 使用默认材料（CustomWood x5）
            ItemData wood = itemDataManager.GetByName("CustomWood");
            if (wood != null && wood.Id > 0)
            {
                return new Dictionary<int, ResourceInfo>
                {
                    { wood.Id, new ResourceInfo(wood.Id, 5) },
                };
            }

            // 连默认材料都找不到 → 免费建造
            return new Dictionary<int, ResourceInfo>();
        }

        /// <summary>
        /// 在 Worker 周围找一个空闲的可建造位置。
        /// 优先返回 PlannedHomePosition（如果仍然空闲），否则螺旋搜索新位置。
        /// 检查 BuildMap 和地面是否可通行。
        /// </summary>
        private Vector3Int? FindFreeBuildPosition(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;

            // 优先：已有规划位置且仍然空闲
            if (wd?.PlannedHomePosition != null)
            {
                Vector3Int planned = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
                if (ASeek.IsCanReach(planned))
                {
                    var buildMap = Core.ServiceLocator.Get<BuildMap>();
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(planned);
                    if (buildMap?.BuildMapDataLAB?.PosMap == null
                        || !buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                    {
                        return planned;
                    }
                }

                // 规划位置已被占用或不可通行，清除并重新搜索
                wd.PlannedHomePosition = null;
            }

            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            var buildMap2 = Core.ServiceLocator.Get<BuildMap>();

            // 从 Worker 位置向外螺旋搜索空闲位置
            for (int r = 1; r <= 8; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (System.Math.Abs(dx) != r && System.Math.Abs(dy) != r) continue; // 只检查外围

                        Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);

                        // 检查是否可通行
                        if (!ASeek.IsCanReach(pos)) continue;

                        // 检查 BuildMap 是否已有建筑
                        if (buildMap2 != null)
                        {
                            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                            if (buildMap2.BuildMapDataLAB?.PosMap != null
                                && buildMap2.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                                continue;
                        }

                        return pos;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 为无家 Worker 选择建家位置。在附近扫描空地，选择最近的可建造位置。
        /// 结果写入 WorkerData.PlannedHomePosition，后续 FindFreeBuildPosition 会优先返回该位置。
        /// </summary>
        public void TryPickHomeSite(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null || wd.HomePosition != null) return; // 已有家
            if (wd.PlannedHomePosition != null) return;        // 已规划过

            Vector3Int? pos = this.FindFreeBuildPosition(worker);
            if (pos.HasValue)
            {
                wd.PlannedHomePosition = Vector3IntLAB.ToVector3IntLAB(pos.Value);
                AWorkerTask.LogProvider(
                    $"{worker.name} 选定建家位置: ({pos.Value.x},{pos.Value.y})",
                    LogManager.LogLevelEnum.Info);
            }
        }

        /// <summary>
        /// 检查 Worker（身上携带 + 个人仓库）是否有足够资源完成建造。
        /// </summary>
        private bool HasEnoughResourcesForBuild(AWorker worker, Dictionary<int, ResourceInfo> needs)
        {
            if (needs == null || needs.Count == 0) return true;

            foreach (var kv in needs)
            {
                int itemId = kv.Key;
                int required = kv.Value.Count;
                int have = worker.GetResourceCountById(itemId); // 身上携带

                // 检查仓库
                if (worker.HasInStorage(itemId, 1))
                {
                    var stored = worker.GetStorageResources();
                    foreach (var s in stored)
                        if (s.Id == itemId) { have += s.Count; break; }
                }

                if (have < required) return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试创建自主种植决策。
        /// Worker 有种子且附近有空闲农田时触发。
        /// </summary>
        private Decision? TryMakeSelfPlantDecision(AWorker worker)
        {
            PlantCandidate? candidate = this.ScanForPlantPositions(worker);
            if (!candidate.HasValue) return null;

            return Decision.MakePlant(
                WorkerDecisionType.SelfPlant,
                candidate.Value.Position,
                $"自己种植: pos=({candidate.Value.Position.x},{candidate.Value.Position.y})");
        }

        /// <summary>
        /// 扫描附近待建造位置（BuildMap 中 IsComplete == false 的位置）。
        /// </summary>
        private BuildCandidate? ScanForBuildPositions(AWorker worker)
        {
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null) return null;

            BuildCandidate? best = null;
            float bestDist = float.MaxValue;

            foreach (var kv in buildMap.BuildMapDataLAB.PosMap)
            {
                // 已完成的建筑不需要再建造
                if (kv.Value.IsComplete) continue;

                Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);
                float dist = (pos - workerPos).sqrMagnitude;
                if (dist > this.ScanRadius * this.ScanRadius) continue;

                // 获取建造材料需求
                var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();
                BuildItemData buildData = itemDataManager.GetBuildItemDataByName(kv.Value.Name);
                if (buildData == null) continue;

                Dictionary<int, ResourceInfo> needs = this.BuildResourceDictFromData(buildData);
                if (needs == null || needs.Count == 0) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new BuildCandidate
                    {
                        Position = pos,
                        TileName = kv.Value.Name,
                        NeededResources = needs,
                    };
                }
            }

            return best;
        }

        /// <summary>
        /// 从 BuildItemData.BuildCosts 构建资源需求字典（与 BuildMap.BuildResourceDict 逻辑一致）。
        /// </summary>
        private Dictionary<int, ResourceInfo> BuildResourceDictFromData(BuildItemData buildData)
        {
            Dictionary<int, ResourceInfo> dict = new Dictionary<int, ResourceInfo>();
            if (buildData.BuildCosts != null)
            {
                foreach (ResourceCost cost in buildData.BuildCosts)
                {
                    if (string.IsNullOrEmpty(cost.ItemName)) continue;

                    ItemData item = Core.ServiceLocator.Get<ItemDataManager>().GetByName(cost.ItemName);
                    if (item != null && item.Id > 0)
                    {
                        dict[item.Id] = new ResourceInfo(item.Id, cost.Count);
                    }
                }
            }

            // Fallback: 若未配置 BuildCosts，默认返回空字典（建造位置会存在但无材料需求）
            return dict;
        }

        /// <summary>
        /// 扫描附近空闲农田（FarmlandManager 中未种植的位置）。
        /// Worker 必须携带种子或在仓库中有种子。
        /// </summary>
        private PlantCandidate? ScanForPlantPositions(AWorker worker)
        {
            // 检查 Worker 是否有种子（身上或仓库）
            bool hasSeeds = this.WorkerHasSeeds(worker);
            if (!hasSeeds) return null;

            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            var farmlandManager = Core.ServiceLocator.Get<FarmlandManager>();
            if (farmlandManager == null) return null;

            // FarmlandManager 的 cells 字典中，id == -1 表示空闲农田
            // 通过反射或直接访问内部字典有困难，使用 FarmlandManager 公开方法
            // IsEnoughAndPrePlant 返回空闲农田位置（不传 isPre 以避免副作用）
            Vector3Int farmPos = farmlandManager.IsEnoughAndPrePlant(worker, null, false);
            if (farmPos == default) return null;

            float dist = (farmPos - workerPos).sqrMagnitude;
            if (dist > this.ScanRadius * this.ScanRadius) return null;

            return new PlantCandidate { Position = farmPos };
        }

        /// <summary>
        /// 检查 Worker 是否有种子（身上携带或仓库存储）。
        /// </summary>
        private bool WorkerHasSeeds(AWorker worker)
        {
            // 检查身上携带的资源中是否有种子类型
            List<ResourceInfo> carried = worker.GetAllResources();
            foreach (var r in carried)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Seed) return true;
            }

            // 检查仓库中是否有种子
            List<ResourceInfo> stored = worker.GetStorageResources();
            foreach (var r in stored)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Seed) return true;
            }

            return false;
        }

        /// <summary>
        /// 检查 Worker 是否有食物（身上携带或仓库存储）。
        /// </summary>
        private bool WorkerHasFood(AWorker worker)
        {
            // 检查身上携带的资源中是否有食物类型
            List<ResourceInfo> carried = worker.GetAllResources();
            foreach (var r in carried)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food) return true;
            }

            // 检查仓库中是否有食物
            List<ResourceInfo> stored = worker.GetStorageResources();
            foreach (var r in stored)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food) return true;
            }

            return false;
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
