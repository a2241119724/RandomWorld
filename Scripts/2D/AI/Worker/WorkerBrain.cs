namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
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
        PickUp,

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

        /// <summary>疲劳阈值：低于此值优先睡觉（提高以更早触发睡眠，避免进入低效区）。</summary>
        public float TiredThreshold = 35f;

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

            /// <summary>是否为地形挖掘（而非资源采集）。</summary>
            public bool IsTerrainDig;

            /// <summary>要挖掘的地形 ID（仅 IsTerrainDig=true 时有效）。</summary>
            public int TerrainId;

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

            /// <summary>是否为地形挖掘候选（而非 ResourceMap 资源）。</summary>
            public bool IsTerrainDig;

            /// <summary>要挖掘的地形 ID（仅 IsTerrainDig=true 时有效）。</summary>
            public int TerrainId;
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
                Decision? boardDecision = this.TryMakePickUpDecision(worker);
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
                    IsTerrainDig = nearbyResource.Value.IsTerrainDig,
                    TerrainId = nearbyResource.Value.TerrainId,
                };
            }

            // 事业型：自己干
            if (p.Ambition > this.AmbitionThreshold && p.Mood > this.MoodLowThreshold && hasNearbyResource && Random.value < selfProb)
            {
                var rc = nearbyResource.Value;
                return new Decision
                {
                    Type = WorkerDecisionType.SelfGather,
                    TargetPosition = rc.Position,
                    Resource = rc.Resource,
                    Description = rc.IsTerrainDig
                        ? $"事业心({p.Ambition:F0})驱使自主挖掘地形"
                        : $"事业心({p.Ambition:F0})驱使自主采集",
                    IsTerrainDig = rc.IsTerrainDig,
                    TerrainId = rc.TerrainId,
                };
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

            // 2. 疲劳 < 55 → 睡觉（Bootstrap阶段更早休息保证安全）
            if (wd.CurTired < 55f)
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
            {
                var rc = nearbyResource.Value;
                return new Decision
                {
                    Type = WorkerDecisionType.SelfGather,
                    TargetPosition = rc.Position,
                    Resource = rc.Resource,
                    Description = rc.IsTerrainDig ? "Bootstrap: 挖掘地形获取建材" : "Bootstrap: 采集建材",
                    IsTerrainDig = rc.IsTerrainDig,
                    TerrainId = rc.TerrainId,
                };
            }

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
        /// 扫描 5×5 房间区域内未被认领的资源。
        /// 在建造第一块墙之前调用，确保先清理区域内的所有资源。
        /// </summary>
        /// <param name="center">房间中心位置</param>
        /// <returns>最近的可采集资源，无则 null</returns>
        private ResourceCandidate? ScanRoomAreaForResource(Vector3Int center)
        {
            var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            var gatherMap = Core.ServiceLocator.Get<GatherMap>();
            ResourceCandidate? best = null;
            float bestDist = float.MaxValue;

            // 扫描整个 5×5 房间区域（含墙壁、门、床、地板）
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);

                    // 跳过已被其他 Worker 认领的资源
                    if (gatherMap?.GatherMapDataLAB?.ContainKey(pos) == true)
                        continue;

                    if (!resourceMap.TryGetGatherResourceInfo(pos, out ResourceInfo resourceInfo))
                        continue;

                    float dist = (pos - center).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = new ResourceCandidate
                        {
                            Position = pos,
                            Resource = resourceInfo,
                        };
                    }
                }
            }

            return best;
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
        private Decision? TryMakePickUpDecision(AWorker worker)
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
                Type = WorkerDecisionType.PickUp,
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
            var gatherMap = Core.ServiceLocator.Get<GatherMap>();

            for (int dx = -this.ScanRadius; dx <= this.ScanRadius; dx++)
            {
                for (int dy = -this.ScanRadius; dy <= this.ScanRadius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);

                    // 跳过已被其他 Worker 认领的资源（GatherMap 中的标记）
                    if (gatherMap?.GatherMapDataLAB?.ContainKey(pos) == true)
                        continue;

                    // 跳过近期寻路失败的位置，避免重复尝试不可达资源
                    if (ASeek.IsRecentFail(pos))
                        continue;

                    if (!resourceMap.TryGetGatherResourceInfo(pos, out ResourceInfo resourceInfo))
                        continue;

                    candidates.Add(new ResourceCandidate
                    {
                        Position = pos,
                        Resource = resourceInfo,
                    });
                }
            }

            // 同时扫描可挖掘地形（山脉等）
            List<ResourceCandidate> terrainCandidates = this.ScanForDiggableTerrain(worker);
            candidates.AddRange(terrainCandidates);

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
        /// 扫描周围可挖掘的地形瓦片（如山脉）。
        /// 地形瓦片存在于 TileMap.MapTiles，不在 ResourceMap 中。
        /// </summary>
        private List<ResourceCandidate> ScanForDiggableTerrain(AWorker worker)
        {
            List<ResourceCandidate> candidates = new List<ResourceCandidate>();
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);
            TileMap tileMap = Core.ServiceLocator.Get<TileMap>();
            TerrainConfigDatabase db = Core.ServiceLocator.Get<TerrainConfigDatabase>();
            var gatherMap = Core.ServiceLocator.Get<GatherMap>();

            if (tileMap?.TileMapDataLAB?.MapTiles == null || db == null)
            {
                return candidates;
            }

            int maxX = System.Math.Min(workerPos.x + this.ScanRadius, tileMap.TileMapDataLAB.Height - 1);
            int maxY = System.Math.Min(workerPos.y + this.ScanRadius, tileMap.TileMapDataLAB.Width - 1);

            for (int x = System.Math.Max(workerPos.x - this.ScanRadius, 0); x <= maxX; x++)
            {
                for (int y = System.Math.Max(workerPos.y - this.ScanRadius, 0); y <= maxY; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    // 跳过已被认领的位置
                    if (gatherMap?.GatherMapDataLAB?.ContainKey(pos) == true)
                    {
                        continue;
                    }

                    // 跳过近期寻路失败的位置
                    if (ASeek.IsRecentFail(pos))
                    {
                        continue;
                    }

                    int terrainId = tileMap.TileMapDataLAB.MapTiles[x, y];
                    if (!db.IsDiggable(terrainId))
                    {
                        continue;
                    }

                    // 检查是否有至少一个可行走的邻居（Worker 需要站在旁边工作）
                    if (!this.HasWalkableNeighborForDig(pos, tileMap, db))
                    {
                        continue;
                    }

                    candidates.Add(new ResourceCandidate
                    {
                        Position = pos,
                        IsTerrainDig = true,
                        TerrainId = terrainId,
                        Resource = new ResourceInfo(0), // 占位，实际掉落由 Finish() 确定
                    });
                }
            }

            return candidates;
        }

        /// <summary>
        /// 检查挖掘目标是否有至少一个可行走邻居（Worker 需要站在旁边工作）。
        /// </summary>
        private bool HasWalkableNeighborForDig(Vector3Int pos, TileMap tileMap, TerrainConfigDatabase db)
        {
            int[] dxArr = { 0, 1, 0, -1 };
            int[] dyArr = { 1, 0, -1, 0 };
            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dxArr[i];
                int ny = pos.y + dyArr[i];
                if (nx < 0 || nx >= tileMap.TileMapDataLAB.Height
                    || ny < 0 || ny >= tileMap.TileMapDataLAB.Width)
                {
                    continue;
                }

                int neighborId = tileMap.TileMapDataLAB.MapTiles[nx, ny];
                if (db.IsWalkable(neighborId))
                {
                    return true;
                }
            }

            return false;
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

                    // 跳过近期寻路失败的位置，避免重复尝试不可达资源
                    if (ASeek.IsRecentFail(pos))
                        continue;

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
        // 中间 3x3 空地放床，墙壁围在第二圈。下侧/南侧 (-2,0) 留门。
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
            new Vector3Int(1, -2, 0),  // 9: 下边右 (ARoom: 左)   → _3
            new Vector3Int(0, -2, 0),  // 10: 下边中 (ARoom: 左)   → _3 [原门位置,现为墙]
            new Vector3Int(-1, -2, 0), // 11: 下边左 (ARoom: 左)   → _3
            new Vector3Int(-2, -2, 0), // 12: 左下角 (ARoom: 左下) → _5
            new Vector3Int(-2, -1, 0), // 13: 左边下 (ARoom: 下)   → _6
            new Vector3Int(-2, 1, 0),  // 14: 左边上 (ARoom: 下)   → _6 [门在(-2,0)]
        };
        // 对应每块墙的方向编号（按 ARoom 坐标约定）
        private static readonly int[] WallDirections = new int[]
        {
            7,              // 0: 左上角 → _7 (右下)
            4, 4, 4,        // 1-3: 上边 → _4 (右)
            2,              // 4: 右上角 → _2 (右上)
            1, 1, 1,        // 5-7: 右边 → _1 (上)
            0,              // 8: 右下角 → _0 (左上)
            3,              // 9: 下边右 → _3 (左)
            3,              // 10: 下边中 → _3 (左) [原门位置,现为墙]
            3,              // 11: 下边左 → _3 (左)
            5,              // 12: 左下角 → _5 (左下)
            6,              // 13: 左边下 → _6 (下)
            6,              // 14: 左边上 → _6 (下)
        };
        public const int WallCount = 15;           // 15 面墙
        public const int DoorStage = WallCount;    // 15: 门
        public const int BedStage = WallCount + 1; // 16: 床
        public const int CompleteStage = WallCount + 2; // 17: 完成

        /// <summary>获取房间所有墙壁偏移（供外部注册房间使用）。</summary>
        public static IReadOnlyList<Vector3Int> GetWallOffsets() => WallOffsets;

        /// <summary>门相对于中心的偏移（左墙/下侧中间）。</summary>
        public static readonly Vector3Int DoorOffset = new Vector3Int(-2, 0, 0);

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
                // 必须有规划好的建家位置，不允许使用当前位置兜底（会导致多人重叠）
                if (wd.PlannedHomePosition == null)
                {
                    this.TryPickHomeSite(worker);
                    // 刚选了位置，下次决策再建造
                    return Decision.Make(WorkerDecisionType.Wander, "尚未选定建家位置, 漫游探索");
                }

                Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);

                // 建造前先扫描房间区域内的资源，有资源先采集
                if (wd.HomeBuildStage == 0)
                {
                    // 先预注册所有房间位置到 BuildMap，阻止 GenTree 在这些位置生成树
                    if (!this.PreReserveAllRoomPositions(center, worker))
                    {
                        // 预注册失败（位置冲突）→ 重新选址
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建房区域预注册失败, 重新选址",
                            LogManager.LogLevelEnum.Warning);
                        this.RelocateHomeSite(worker, wd);
                        return Decision.Make(WorkerDecisionType.Wander,
                            "建家位置预注册失败, 重新选址后漫游");
                    }

                    ResourceCandidate? roomResource = this.ScanRoomAreaForResource(center);
                    if (roomResource.HasValue)
                    {
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建房区域有资源 {roomResource.Value.Resource.Id}, 先采集 pos=({roomResource.Value.Position.x},{roomResource.Value.Position.y})",
                            LogManager.LogLevelEnum.Debug);
                        return Decision.MakeGather(roomResource.Value.Position,
                            roomResource.Value.Resource,
                            $"清理建房区域资源");
                    }
                }

                if (wd.HomeBuildStage < WallCount)
                {
                    // 阶段 0-14：建墙壁，使用对应方向变体
                    int dir = WallDirections[wd.HomeBuildStage];
                    buildTileName = $"CustomRoomWall_{dir}";
                    buildPos = center + WallOffsets[wd.HomeBuildStage];
                }
                else if (wd.HomeBuildStage == DoorStage)
                {
                    // 阶段 15：建门
                    buildTileName = "CustomDoor";
                    buildPos = center + DoorOffset;
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
                            LogManager.LogLevelEnum.Warning);
                        wd.HomeBuildStage = BedStage;
                        buildTileName = "SingleBed";
                        buildPos = center;
                        needs = this.GetBuildMaterialNeeds(buildTileName);
                    }

                    if (needs == null)
                    {
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建造失败: {buildTileName} 物品不存在于数据库",
                            LogManager.LogLevelEnum.Warning);
                        return null;
                    }
                }

                // 检查建造位置本身是否可用
                if (!buildPos.HasValue || !ASeek.IsCanReach(buildPos.Value))
                {
                    // 位置被阻挡：检查是否是资源，是则先采集再建造
                    if (buildPos.HasValue)
                    {
                        var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
                        var gatherMap = Core.ServiceLocator.Get<GatherMap>();

                        // 检查是否已被其他 Worker 认领
                        if (gatherMap?.GatherMapDataLAB?.ContainKey(buildPos.Value) == true)
                        {
                            AWorkerTask.LogProvider(
                                $"{worker.name} 建造位置资源已被认领, 等待释放 pos=({buildPos.Value.x},{buildPos.Value.y})",
                                LogManager.LogLevelEnum.Debug);
                            return Decision.Make(WorkerDecisionType.Wander, "等待资源释放");
                        }

                        if (resourceMap.TryGetGatherResourceInfo(buildPos.Value, out ResourceInfo resourceInfo))
                        {
                            AWorkerTask.LogProvider(
                                $"{worker.name} 建造位置有资源, 先采集再建 {buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})",
                                LogManager.LogLevelEnum.Debug);
                            return Decision.MakeGather(buildPos.Value, resourceInfo, "清理建造位置");
                        }

                        // 检查是否是地形可挖掘（如山/树等 terrain tile，不在 ResourceMap 中）
                        var tileMap = Core.ServiceLocator.Get<TileMap>();
                        var terrainDb = Core.ServiceLocator.Get<TerrainConfigDatabase>();
                        if (tileMap?.TileMapDataLAB?.MapTiles != null && terrainDb != null
                            && buildPos.Value.x >= 0 && buildPos.Value.x < tileMap.TileMapDataLAB.Height
                            && buildPos.Value.y >= 0 && buildPos.Value.y < tileMap.TileMapDataLAB.Width)
                        {
                            int terrainId = tileMap.TileMapDataLAB.MapTiles[buildPos.Value.x, buildPos.Value.y];
                            if (terrainDb.IsDiggable(terrainId))
                            {
                                AWorkerTask.LogProvider(
                                    $"{worker.name} 建造位置有可挖掘地形(terrainId={terrainId}), 先挖掘再建 {buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})",
                                    LogManager.LogLevelEnum.Debug);
                                return new Decision
                                {
                                    Type = WorkerDecisionType.SelfGather,
                                    TargetPosition = buildPos.Value,
                                    Resource = null,
                                    Description = "挖掘建造位置地形",
                                    IsTerrainDig = true,
                                    TerrainId = terrainId,
                                };
                            }
                        }
                    }

                    // 不是资源阻挡 → 位置本身有问题，清除规划重新选址，避免死循环
                    AWorkerTask.LogProvider(
                        $"{worker.name} 建造位置不可达(非资源阻挡): {buildTileName} pos=({buildPos?.x},{buildPos?.y}), 重新选址",
                        LogManager.LogLevelEnum.Warning);
                    this.RelocateHomeSite(worker, wd);
                    return Decision.Make(WorkerDecisionType.Wander,
                        "建家位置无效, 重新选址后漫游探索");
                }

                // 检查建造位置是否至少有一个可达的邻居（Worker 需要站在旁边建造）
                if (!this.HasReachableNeighbor(buildPos.Value))
                {
                    AWorkerTask.LogProvider(
                        $"{worker.name} 建造位置无可用邻居, 重新选址: {buildTileName} pos=({buildPos.Value.x},{buildPos.Value.y})",
                        LogManager.LogLevelEnum.Warning);
                    this.RelocateHomeSite(worker, wd);
                    return Decision.Make(WorkerDecisionType.Wander,
                        "建造位置无邻居可达, 重新选址后漫游");
                }

                // ---- 通用冲突检测（墙壁、门、床 所有阶段） ----
                {
                    var buildMap = Core.ServiceLocator.Get<BuildMap>();
                    if (buildMap?.BuildMapDataLAB?.PosMap != null)
                    {
                        Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(buildPos.Value);

                        // 已完成 → 墙壁阶段跳过，门/床阶段说明房间已完成 → 标记完成
                        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var existingTile)
                            && existingTile.IsComplete)
                        {
                            if (wd.HomeBuildStage < WallCount)
                            {
                                AWorkerTask.LogProvider(
                                    $"{worker.name} 位置已建造完成, 跳过墙壁{wd.HomeBuildStage + 1}: pos=({buildPos.Value.x},{buildPos.Value.y})",
                                    LogManager.LogLevelEnum.Debug);
                                wd.HomeBuildStage++;
                            }
                            else if (wd.HomeBuildStage == DoorStage || wd.HomeBuildStage == BedStage)
                            {
                                // 门或床已完成 → 房间已建好，直接标记完成
                                AWorkerTask.LogProvider(
                                    $"{worker.name} 门/床已完成, 标记建家完成: pos=({buildPos.Value.x},{buildPos.Value.y})",
                                    LogManager.LogLevelEnum.Debug);
                                wd.HomeBuildStage = CompleteStage;
                                wd.LifeStage = Domain.Worker.WorkerLifeStage.Settled;
                            }
                            return null;
                        }

                        // 被其他 Worker 注册但未完成 → 冲突，重新选址
                        if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var registeredTile)
                            && !registeredTile.IsComplete)
                        {
                            AWorkerTask.LogProvider(
                                $"{worker.name} 建家位置被其他Worker占用(stage={wd.HomeBuildStage}), 重新选址: pos=({buildPos.Value.x},{buildPos.Value.y})",
                                LogManager.LogLevelEnum.Warning);
                            this.ClearAbandonedBuildTiles(wd);
                            wd.PlannedHomePosition = null;
                            wd.HomeBuildStage = 0;
                            this.TryPickHomeSite(worker);
                            return Decision.Make(WorkerDecisionType.Wander,
                                "建家位置冲突, 重新选址后漫游");
                        }
                    }

                    // 位置未在 BuildMap 注册，但可能在其他 Worker 的 5×5 房间范围内
                    // 注意：此处比较的是单个建造位置 vs 房间中心，用 ≤ 2（房间半径），
                    // 而非 IsHomeSiteClaimedByOther 的 ≤ 4（中心距）。
                    if (this.IsPositionInsideOtherWorkerRoom(buildPos.Value, worker))
                    {
                        AWorkerTask.LogProvider(
                            $"{worker.name} 建家位置落入其他Worker规划范围(stage={wd.HomeBuildStage}), 重新选址: pos=({buildPos.Value.x},{buildPos.Value.y})",
                            LogManager.LogLevelEnum.Warning);
                        this.ClearAbandonedBuildTiles(wd);
                        wd.PlannedHomePosition = null;
                        wd.HomeBuildStage = 0;
                        this.TryPickHomeSite(worker);
                        return Decision.Make(WorkerDecisionType.Wander,
                            "建家位置与其他Worker规划冲突, 重新选址后漫游");
                    }
                }
            }
            // 有家+高事业 → 扩建（已有床则跳过，避免无限重复建造）
            else if (wd.Personality.Ambition > 65 && worker.BedItem == null)
            {
                buildTileName = "SingleBed"; // TODO: 扩展更多建造类型
                needs = this.GetBuildMaterialNeeds(buildTileName);
                if (needs == null || needs.Count == 0) return null;

                buildPos = this.FindFreeBuildPosition(worker);
                if (!buildPos.HasValue)
                {
                    AWorkerTask.LogProvider(
                        $"{worker.name} 想建{buildTileName}但附近无空闲位置",
                        LogManager.LogLevelEnum.Debug);
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
                LogManager.LogLevelEnum.Debug);

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
        /// 检查 BuildMap、地面可通行性、以及其他 Worker 的已规划建家位置。
        /// </summary>
        private Vector3Int? FindFreeBuildPosition(AWorker worker)
        {
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;

            // 优先：已有规划位置且仍然空闲（包括不被其他 Worker 占据）
            if (wd?.PlannedHomePosition != null)
            {
                Vector3Int planned = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
                if (ASeek.IsCanReach(planned)
                    && this.CanFitRoom(planned)
                    && !this.IsHomeSiteClaimedByOther(planned, worker)
                    && !this.IsRoomAreaBlockedInBuildMap(planned))
                {
                    return planned;
                }

                // 规划位置已被占用或不可通行，清除并重新搜索
                wd.PlannedHomePosition = null;
            }

            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

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

                        // 检查 5×5 房间区域是否与 BuildMap 冲突（包括墙壁、门、床位置）
                        if (this.IsRoomAreaBlockedInBuildMap(pos)) continue;

                        // 检查是否被其他 Worker 规划为建家位置（防止房间重叠）
                        if (this.IsHomeSiteClaimedByOther(pos, worker)) continue;

                        // 检查 5×5 房间能否完整放置（所有墙壁/门位置都可达）
                        if (!this.CanFitRoom(pos)) continue;

                        return pos;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 原子搬迁：先选新位置并设置 PlannedHomePosition，再清除旧位置瓦片。
        /// 消除 PlannedHomePosition=null 的空窗期，防止其他 Worker 抢占附近位置。
        /// </summary>
        private void RelocateHomeSite(AWorker worker, AWorker.WorkerData wd)
        {
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            Vector3IntLAB oldPosition = wd.PlannedHomePosition;

            // 1. 先选新位置（保留旧 PlannedHomePosition，IsHomeSiteClaimedByOther 会排除旧位置自身）
            Vector3Int? newPos = this.FindFreeBuildPosition(worker);
            if (newPos.HasValue)
            {
                wd.PlannedHomePosition = Vector3IntLAB.ToVector3IntLAB(newPos.Value);
                AWorkerTask.LogProvider(
                    $"{worker.name} 搬迁建家位置: ({newPos.Value.x},{newPos.Value.y})",
                    LogManager.LogLevelEnum.Debug);
            }
            // 如果找不到新位置，PlannedHomePosition 保持旧值，Worker 下次会重试

            // 2. 清除旧位置的残留瓦片（包括 tilemap 视觉和 BuildMap 数据）
            if (oldPosition != null && buildMap?.BuildMapDataLAB?.PosMap != null)
            {
                Vector3Int oldCenter = Vector3IntLAB.ToVector3Int(oldPosition);
                for (int i = 0; i < WallCount; i++)
                {
                    Vector3Int wallPos = oldCenter + WallOffsets[i];
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(wallPos);
                    if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var tile)
                        && !tile.IsComplete)
                    {
                        buildMap.BuildMapDataLAB.PosMap.Remove(posLAB);
                        buildMap.CancelBuilding(wallPos);
                    }
                }
                Vector3Int oldDoor = oldCenter + DoorOffset;
                Vector3IntLAB doorLAB = Vector3IntLAB.ToVector3IntLAB(oldDoor);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(doorLAB, out var d) && !d.IsComplete)
                {
                    buildMap.BuildMapDataLAB.PosMap.Remove(doorLAB);
                    buildMap.CancelBuilding(oldDoor);
                }
                Vector3IntLAB centerLAB = Vector3IntLAB.ToVector3IntLAB(oldCenter);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(centerLAB, out var c) && !c.IsComplete)
                {
                    buildMap.BuildMapDataLAB.PosMap.Remove(centerLAB);
                    buildMap.CancelBuilding(oldCenter);
                }
            }

            // 3. 重置建造阶段（新位置从墙壁 0 开始）
            wd.HomeBuildStage = 0;
        }

        /// <summary>
        /// 预注册房间所有建造位置到 BuildMap，阻止 GenTree 在墙/门/床位置生成树。
        /// 在 HomeBuildStage==0 时调用一次，后续 CreateSelfBuildTask 跳过已注册位置。
        /// </summary>
        /// <param name="center">房间中心</param>
        /// <param name="worker">当前 Worker（仅用于日志）</param>
        /// <returns>true = 全部注册成功；false = 存在冲突，需重新选址</returns>
        private bool PreReserveAllRoomPositions(Vector3Int center, AWorker worker)
        {
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap == null) return false;

            // 收集所有需要预注册的位置及其 tileName
            var positionsToReserve = new List<(Vector3Int pos, string tileName)>(WallCount + 2);

            for (int i = 0; i < WallCount; i++)
            {
                int dir = WallDirections[i];
                string tileName = $"CustomRoomWall_{dir}";
                positionsToReserve.Add((center + WallOffsets[i], tileName));
            }
            positionsToReserve.Add((center + DoorOffset, "CustomDoor"));
            positionsToReserve.Add((center, "SingleBed"));

            // 两阶段：先检查全部可用，再统一注册（避免部分注册后失败难以回滚）
            foreach (var (pos, tileName) in positionsToReserve)
            {
                Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var existing))
                {
                    if (existing.IsComplete)
                    {
                        // 已完成 → 可接受（可能是其他 Worker 建的公共墙）
                        AWorkerTask.LogProvider(
                            $"{worker.name} 预注册: 位置已完成, 跳过 {tileName} pos=({pos.x},{pos.y})",
                            LogManager.LogLevelEnum.Debug);
                        continue;
                    }

                    // 被其他 Worker 占用且未完成 → 冲突
                    AWorkerTask.LogProvider(
                        $"{worker.name} 预注册失败: 位置被占用 {tileName} pos=({pos.x},{pos.y}) 被 {existing.BuilderName}",
                        LogManager.LogLevelEnum.Warning);
                    return false;
                }
            }

            // 全部可用 → 执行注册
            foreach (var (pos, tileName) in positionsToReserve)
            {
                Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                if (buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                {
                    // 已在 PosMap 中（已完成，上面已跳过）→ 不重复注册
                    continue;
                }

                if (!buildMap.ReserveBuildPosition(pos, tileName))
                {
                    AWorkerTask.LogProvider(
                        $"{worker.name} 预注册执行失败: {tileName} pos=({pos.x},{pos.y})",
                        LogManager.LogLevelEnum.Error);
                    // 清理已注册的位置（回滚）
                    this.ClearAbandonedBuildTilesCore(wd: null, center);
                    return false;
                }
            }

            AWorkerTask.LogProvider(
                $"{worker.name} 预注册完成: 15面墙+门+床 共{positionsToReserve.Count}个位置",
                LogManager.LogLevelEnum.Debug);
            return true;
        }

        /// <summary>
        /// 清除指定中心位置的所有建造瓦片（内部实现，不依赖 WorkerData）。
        /// </summary>
        private void ClearAbandonedBuildTilesCore(AWorker.WorkerData wd, Vector3Int center)
        {
            // 如果 wd 为 null，使用给定 center；否则从 wd.PlannedHomePosition 推导
            if (wd != null)
            {
                if (wd.PlannedHomePosition == null) return;
                center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
            }

            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null) return;

            var positionsToClear = new List<Vector3Int>(WallCount + 2);
            for (int i = 0; i < WallCount; i++)
            {
                positionsToClear.Add(center + WallOffsets[i]);
            }
            positionsToClear.Add(center + DoorOffset);
            positionsToClear.Add(center);

            foreach (var pos in positionsToClear)
            {
                Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out var tile)
                    && !tile.IsComplete)
                {
                    buildMap.BuildMapDataLAB.PosMap.Remove(posLAB);
                    buildMap.CancelBuilding(pos);
                }
            }
        }

        /// <summary>
        /// 清除当前 Worker 已经废弃的建造瓦片（IsComplete=false 的 BuildMap 条目）。
        /// 当 Worker 因冲突重新选址时调用，防止残留墙壁影响后续选址。
        /// </summary>
        private void ClearAbandonedBuildTiles(AWorker.WorkerData wd)
        {
            if (wd?.PlannedHomePosition == null) return;
            this.ClearAbandonedBuildTilesCore(wd, default);
        }

        /// <summary>
        /// 检查以该位置为中心的 5×5 房间能否完整放置。
        /// 检查所有墙壁和门位置的可达性（不仅仅是四角），防止选址在部分不可达的位置。
        /// </summary>
        private bool CanFitRoom(Vector3Int center)
        {
            // 房间边界检查：5×5 房间不能超出地图范围
            var tileMap = Core.ServiceLocator.Get<TileMap>();
            int mapWidth = tileMap?.TileMapDataLAB?.Width ?? int.MaxValue;
            int mapHeight = tileMap?.TileMapDataLAB?.Height ?? int.MaxValue;

            // 房间四角：center ± 2
            // 下(左墙)、左(底墙)、右(上墙)三方向留一格行走空间；上(右墙)无需
            if (center.x - 2 < 1 || center.y - 2 < 1
                || center.x + 2 >= mapWidth || center.y + 2 >= mapHeight - 1)
            {
                return false;
            }

            // 检查所有墙壁位置是否可达（不仅仅是四角）
            for (int i = 0; i < WallCount; i++)
            {
                Vector3Int wallPos = center + WallOffsets[i];
                if (!ASeek.IsCanReach(wallPos))
                {
                    return false;
                }
            }

            // 检查门位置是否可达
            Vector3Int doorPos = center + DoorOffset;
            if (!ASeek.IsCanReach(doorPos))
            {
                return false;
            }

            // 检查中心位置（床的位置）是否可达
            if (!ASeek.IsCanReach(center))
            {
                return false;
            }

            // 检查下、左、右三面外侧的行走空间是否可通行
            // 门在左墙 (-2,0)，外侧为 (-3,0)；左墙用下(DOWN)瓦片，故为视觉"下"侧
            Vector3Int outsideDoor = center + new Vector3Int(-3, 0, 0);
            if (!ASeek.IsCanReach(outsideDoor))
            {
                return false;
            }

            // 视觉"左"侧 = 底墙 (y=-2)，外侧为 (0,-3)
            Vector3Int bottomOutside = center + new Vector3Int(0, -3, 0);
            if (!ASeek.IsCanReach(bottomOutside))
            {
                return false;
            }

            // 视觉"右"侧 = 上墙 (y=2)，外侧为 (0,3)
            Vector3Int topOutside = center + new Vector3Int(0, 3, 0);
            if (!ASeek.IsCanReach(topOutside))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查建造位置是否至少有一个相邻格子可通行（Worker 需要站在旁边才能建造）。
        /// </summary>
        private bool HasReachableNeighbor(Vector3Int buildPos)
        {
            // 检查上下左右四个邻居
            Vector3Int[] neighbors = {
                new Vector3Int(0, 1, 0),   // 上
                new Vector3Int(1, 0, 0),   // 右
                new Vector3Int(0, -1, 0),  // 下
                new Vector3Int(-1, 0, 0),  // 左
            };

            foreach (var offset in neighbors)
            {
                Vector3Int neighbor = buildPos + offset;
                if (ASeek.IsCanReach(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查 5×5 房间区域的任何瓦片是否已被 BuildMap 占用（已完成或建造中）。
        /// 这是对 IsHomeSiteClaimedByOther 的补充：
        /// IsHomeSiteClaimedByOther 检查其他 Worker 的 PlannedHomePosition/HomePosition，
        /// 本方法检查 BuildMap 中的实际建筑瓦片（包括非 Worker 来源的建筑、已完成墙壁等）。
        /// </summary>
        /// <param name="center">房间中心位置</param>
        /// <returns>true 表示区域内有 BuildMap 瓦片占用</returns>
        private bool IsRoomAreaBlockedInBuildMap(Vector3Int center)
        {
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null) return false;

            // 检查所有墙壁位置
            for (int i = 0; i < WallCount; i++)
            {
                Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(center + WallOffsets[i]);
                if (buildMap.BuildMapDataLAB.PosMap.ContainsKey(posLAB))
                {
                    return true;
                }
            }

            // 检查门位置
            Vector3IntLAB doorLAB = Vector3IntLAB.ToVector3IntLAB(center + DoorOffset);
            if (buildMap.BuildMapDataLAB.PosMap.ContainsKey(doorLAB))
            {
                return true;
            }

            // 检查床位置（中心）
            Vector3IntLAB centerLAB = Vector3IntLAB.ToVector3IntLAB(center);
            if (buildMap.BuildMapDataLAB.PosMap.ContainsKey(centerLAB))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查某个建造位置（墙壁/门/床的具体坐标）是否落入其他 Worker 的房间范围内。
        /// 房间范围 = 中心 ±2（5×5），因此阈值是 ≤ 2。
        /// 与 IsHomeSiteClaimedByOther 不同：IsHomeSiteClaimedByOther 比较的是
        /// 两个房间中心之间的距离（阈值 ≤ 4），本方法比较的是单个建造位置
        /// 与另一个房间中心之间的距离（阈值 ≤ 2）。
        /// </summary>
        /// <param name="buildPos">墙壁/门/床的具体坐标</param>
        /// <param name="self">当前 Worker</param>
        /// <returns>true 表示该建造位置在另一个 Worker 的房间范围内</returns>
        private bool IsPositionInsideOtherWorkerRoom(Vector3Int buildPos, AWorker self)
        {
            var workerManager = Core.ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null) return false;

            foreach (AWorker other in workerManager.Characters)
            {
                if (other == self) continue;
                AWorker.WorkerData otherWd = other.CharacterDataLAB as AWorker.WorkerData;
                if (otherWd == null) continue;

                Vector3IntLAB otherCenterLAB = otherWd.PlannedHomePosition ?? otherWd.HomePosition;
                if (otherCenterLAB == default) continue;

                Vector3Int otherCenter = Vector3IntLAB.ToVector3Int(otherCenterLAB);
                // 房间范围 ±2：单个位置与另一个房间中心距离 ≤ 2 才在房间内
                if (System.Math.Abs(buildPos.x - otherCenter.x) <= 2
                    && System.Math.Abs(buildPos.y - otherCenter.y) <= 2)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查以该位置为中心的 5×5 房间矩形，是否与其他 Worker 已规划或已完成的家重叠或太近。
        /// 中心距离 ≤ 5 时视为冲突，确保房间之间至少有一格行走间距。
        /// 依赖 RelocateHomeSite 保证 PlannedHomePosition 永远不为 null（消除空窗期）。
        /// </summary>
        private bool IsHomeSiteClaimedByOther(Vector3Int candidateCenter, AWorker self)
        {
            var workerManager = Core.ServiceLocator.Get<WorkerManager>();
            if (workerManager?.Characters == null) return false;

            foreach (AWorker other in workerManager.Characters)
            {
                if (other == self) continue;
                AWorker.WorkerData otherWd = other.CharacterDataLAB as AWorker.WorkerData;
                if (otherWd == null) continue;

                Vector3IntLAB otherCenterLAB = otherWd.PlannedHomePosition ?? otherWd.HomePosition;
                if (otherCenterLAB == default) continue;

                Vector3Int otherCenter = Vector3IntLAB.ToVector3Int(otherCenterLAB);
                if (System.Math.Abs(candidateCenter.x - otherCenter.x) <= 5
                    && System.Math.Abs(candidateCenter.y - otherCenter.y) <= 5)
                {
                    return true;
                }
            }

            return false;
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
                    LogManager.LogLevelEnum.Debug);
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
