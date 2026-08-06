namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// Worker寻找状态 — 空闲时通过 WorkerBrain 自主决策下一步行动。
    /// </summary>
    public class WorkerSeekState : AWorkerState
    {
        private readonly StringBuilder builder = new (128); // 减少GC
        private readonly WorkerBrain brain = new WorkerBrain(); // 自主决策引擎
        private Vector3Int targetMap;
        private long seekTimes; // 没有任务寻路的次数

        public WorkerSeekState(AWorker character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            // 没有任务
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            this.targetMap = AWorkerTask.GenCanReachPosProvider(posMap);
            if (workerData.Task != null)
            {
                // 有任务 → 寻路到任务位置
                this.targetMap = Vector3IntLAB.ToVector3Int(workerData.Task.TargetMap);
                float minDistance = 99999.0f;
                Vector3Int closedPos = default;
                foreach (Vector3IntLAB pos in workerData.Task.AvailableNeighborPos)
                {
                    // 由于是斜对称
                    Vector3Int temp = new (this.targetMap.x + pos.Y, this.targetMap.y + pos.X, 0);
                    if (ASeek.IsCanReach(temp))
                    {
                        Vector3 worldPos = AWorkerTask.TileMapPositionProvider(temp);
                        float dx = worldPos.x - this.Character.transform.position.x;
                        float dy = worldPos.y - this.Character.transform.position.y;
                        float distance = (dx * dx) + (dy * dy);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closedPos = temp;
                        }
                    }
                }

                if (closedPos == default)
                {
                    AWorkerTask.LogProvider($"{workerData.Task.TaskType}, 没有邻居位置!", LogManager.LogLevelEnum.Warning);
                    this.Character.GiveUpTask();
                    return;
                }

                this.targetMap = closedPos;
            }
            else
            {
                // 没有任务 → 自主决策
                AWorkerTask.LogProvider(this.Character.name + " 没有任务!", LogManager.LogLevelEnum.Trace);
                ++this.seekTimes;

                // 每隔一定次数进行自主决策
                if (this.seekTimes % WorkerTaskTimeConfig.ExerciseSeekThreshold == 0)
                {
                    // 先尝试自动出售资源（经济闭环：资源→金币）
                    this.TryAutoSellResources(workerData);

                    // 然后做自主决策
                    this.ExecuteAutonomousDecision(workerData);
                }

                // 每 10 次无任务寻路 → 空闲惩罚：勤奋↓ 心情↓
                if (this.seekTimes % 10 == 0 && workerData != null)
                {
                    workerData.Personality = workerData.Personality.AfterIdle();
                }
            }

            AWorkerTask.LogProvider(this.Character.name + " 寻路->" + this.targetMap, LogManager.LogLevelEnum.Trace);
            this.Character.Seek.Seek(this.targetMap);
        }

        /// <summary>
        /// 执行自主决策：根据人格和状态决定下一步行动。
        /// </summary>
        private void ExecuteAutonomousDecision(AWorker.WorkerData workerData)
        {
            WorkerBrain.Decision decision = this.brain.Decide(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 自主决策: {WorkerBrain.GetDecisionLabel(decision)}",
                LogManager.LogLevelEnum.Info);

            switch (decision.Type)
            {
                case WorkerDecisionType.SelfGather:
                    this.CreateSelfGatherTask(decision);
                    break;

                case WorkerDecisionType.PostBounty:
                    this.TryPostBounty(decision);
                    break;

                case WorkerDecisionType.AcceptBounty:
                    // 接受悬赏由 WorkerTaskManager 的任务分配系统处理
                    // 这里确保 Worker 处于可接任务状态
                    AWorkerTask.LogProvider(
                        $"{this.Character.name} 准备接受悬赏任务",
                        LogManager.LogLevelEnum.Info);
                    this.CreateIdleTask();
                    break;

                case WorkerDecisionType.SelfCarry:
                    this.CreateSelfCarryTask(decision);
                    break;

                case WorkerDecisionType.PickUpFromBoard:
                    this.CreatePickUpFromBoardTask(decision);
                    break;

                case WorkerDecisionType.SelfBuild:
                    this.CreateSelfBuildTask(decision);
                    break;

                case WorkerDecisionType.SelfPlant:
                    this.CreateSelfPlantTask(decision);
                    break;

                case WorkerDecisionType.Eat:
                    this.CreateSelfEatTask();
                    break;

                case WorkerDecisionType.Sleep:
                    this.CreateSelfSleepTask();
                    break;

                case WorkerDecisionType.Idle:
                default:
                    this.CreateIdleTask();
                    break;
            }
        }

        /// <summary>
        /// 创建自我采集任务。
        /// </summary>
        private void CreateSelfGatherTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default)
            {
                AWorkerTask.LogProvider("SelfGather: 无有效目标位置", LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            WorkerGatherTask gatherTask = new WorkerGatherTask.GatherTaskBuilder()
                .SetTarget(decision.TargetPosition)
                .SetResourceInfo(decision.Resource)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池，确保自己执行
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = gatherTask;
            gatherTask.Start(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我采集任务: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 发布悬赏 — 使用 WorkerBrain 预扫描的资源位置，避免重复扫描失败。
        /// 支持 Gather 和 Build 两种悬赏类型。
        /// </summary>
        private void TryPostBounty(WorkerBrain.Decision decision)
        {
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;
            WorkerBountyDecisionService bountyDecision = new WorkerBountyDecisionService();
            bool posted;

            // 建造悬赏：决策携带建造数据
            if (decision.NeededResources != null && decision.NeededResources.Count > 0
                && !string.IsNullOrEmpty(decision.BuildTileName))
            {
                posted = this.TryPostBuildBounty(decision, bountyDecision);
            }
            else
            {
                // 采集悬赏：使用预扫描位置
                posted = bountyDecision.TryPostOneBounty(this.Character, decision.TargetPosition, decision.Resource);
            }

            if (!posted)
            {
                string reason = "条件不满足";
                if (wd != null)
                {
                    int needGold = bountyDecision.BaseRewardGather + bountyDecision.MinimumWalletReserve.Gold;
                    if (!wd.Wallet.HasEnough(new CurrencyAmount(needGold)))
                        reason = $"余额不足(需要{needGold}G, 有{wd.Wallet.Gold}G)";
                    else if (wd.CurTired < bountyDecision.TiredThresholdForBounty)
                        reason = $"太累({wd.CurTired:F0}<{bountyDecision.TiredThresholdForBounty})";
                    else if (wd.CurHungry < bountyDecision.HungryThresholdForBounty)
                        reason = $"太饿({wd.CurHungry:F0}<{bountyDecision.HungryThresholdForBounty})";
                    else
                        reason = $"概率未通过或周围无资源";
                }

                AWorkerTask.LogProvider(
                    $"{this.Character.name} 发布悬赏失败: {reason}",
                    LogManager.LogLevelEnum.Info);

                // 失败时回退：建造→自己建，采集→自己采
                if (decision.NeededResources != null && decision.NeededResources.Count > 0)
                    this.CreateSelfBuildTask(decision);
                else if (decision.TargetPosition != default)
                    this.CreateSelfGatherTask(decision);
                else
                    this.CreateIdleTask();
            }
        }

        /// <summary>
        /// 发布建造悬赏。
        /// </summary>
        private bool TryPostBuildBounty(WorkerBrain.Decision decision, WorkerBountyDecisionService bountyDecision)
        {
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;

            if (!bountyDecision.ShouldPostBounty(this.Character, WorkerTaskType.Build))
                return false;

            WorkerPersonality personality = wd?.Personality ?? WorkerPersonality.Neutral;
            CurrencyAmount reward = bountyDecision.DetermineReward(WorkerTaskType.Build, personality);
            int issuerId = this.Character.GetInstanceID();

            var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
            if (!currencyManager.PostBounty(issuerId, reward)) return false;

            // 在 BuildMap 中注册建造位置（放置建造中的标记），标记为发布者所有
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                Core.ServiceLocator.Get<BuildMap>().AddBuild(
                    decision.TargetPosition, decision.BuildTileName);
            }

            // 构建建造 innerTask
            WorkerBuildTask buildTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuildPos(decision.TargetPosition)
                .SetNeedResource(decision.NeededResources)
                .Build();

            float currentTime = Core.ServiceLocator.Get<IGameTime>().Time;
            WorkerBountyTask bountyTask = new WorkerBountyTask.BountyTaskBuilder()
                .SetInnerTask(buildTask)
                .SetReward(reward)
                .SetIssuer(issuerId)
                .SetExpiration(currentTime + bountyDecision.BountyExpirationSeconds)
                .Build();

            AWorkerTask.TaskAddProvider(
                bountyTask,
                new GameGridPosition(
                    decision.TargetPosition.x,
                    decision.TargetPosition.y,
                    decision.TargetPosition.z),
                2);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 发布了建造悬赏: pos=({decision.TargetPosition.x},{decision.TargetPosition.y}) 悬赏金 {reward}",
                LogManager.LogLevelEnum.Info);

            return true;
        }

        /// <summary>
        /// 创建自我建造任务 — Worker 为自己建造房屋。
        /// 先通过 BuildMap.AddBuild 注册建造位置，再直接分配给自己执行。
        /// </summary>
        private void CreateSelfBuildTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default || decision.NeededResources == null)
            {
                AWorkerTask.LogProvider("SelfBuild: 无有效建造位置", LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            // 在 BuildMap 中注册建造位置（放置建造中的标记），标记为自己所有
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                Core.ServiceLocator.Get<BuildMap>().AddBuild(
                    decision.TargetPosition, decision.BuildTileName);
            }

            WorkerBuildTask buildTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuildPos(decision.TargetPosition)
                .SetNeedResource(decision.NeededResources)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池，确保自己建造自己的房子
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = buildTask;
            buildTask.Start(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 为自己建造 {decision.BuildTileName}: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 创建自我种植任务 — 自己去种植。
        /// </summary>
        private void CreateSelfPlantTask(WorkerBrain.Decision decision)
        {
            WorkerPlantTask plantTask = new WorkerPlantTask.PlantTaskBuilder().Build();

            // 种植任务位置由 PlantTask 内部动态确定，这里使用候选位置做空间索引
            Vector3Int taskPos = decision.TargetPosition != default
                ? decision.TargetPosition
                : Vector3Int.zero;

            // 直接分配给当前 Worker，不入全局任务池，确保自己执行
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = plantTask;
            plantTask.Start(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我种植任务: pos=({taskPos.x},{taskPos.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 创建自我吃饭任务 — 优先交易买食物，失败则自己去采集食物。
        /// </summary>
        private void CreateSelfEatTask()
        {
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;

            // 先尝试 Worker 间交易（吃自己的或买别人的）
            WorkerTradeService trade = new WorkerTradeService();
            bool success = trade.TryBuyFood(this.Character);

            if (success)
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 成功解决饥饿 (当前饥饿:{wd?.CurHungry:F0})",
                    LogManager.LogLevelEnum.Info);
                this.CreateIdleTask();
                return;
            }

            // 交易失败 → 自己去采集食物（扩大扫描范围）
            AWorkerTask.LogProvider(
                $"{this.Character.name} 交易失败, 尝试自己采集食物（扩大扫描范围）",
                LogManager.LogLevelEnum.Info);

            // 扫描食物：从小范围到大范围逐级扩大
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            for (int scanR = 20; scanR <= 80; scanR += 20)
            {
                ResourceInfo foodResource = this.ScanForFoodAtRange(workerPos, scanR);
                if (foodResource != null && foodResource.Id > 0)
                {
                    // 找到了！找到食物所在的具体位置
                    Vector3Int? foodPos = this.FindResourcePosition(workerPos, foodResource.Id, scanR);
                    if (foodPos.HasValue)
                    {
                        var decision = WorkerBrain.Decision.MakeGather(
                            foodPos.Value, foodResource, "交易失败后自己采集食物");

                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 找到食物 id={foodResource.Id} pos=({foodPos.Value.x},{foodPos.Value.y})",
                            LogManager.LogLevelEnum.Info);

                        this.CreateSelfGatherTask(decision);
                        return;
                    }
                }
            }

            AWorkerTask.LogProvider(
                $"{this.Character.name} 扩大扫描范围内无食物可采集，继续空闲",
                LogManager.LogLevelEnum.Warning);
            this.CreateIdleTask();
        }

        /// <summary>
        /// 在指定范围内扫描食物资源，返回找到的第一个食物 ResourceInfo。
        /// </summary>
        private ResourceInfo ScanForFoodAtRange(Vector3Int workerPos, int radius)
        {
            var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            if (resourceMap?.ResourceMapDataLAB?.PosMap == null) return null;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                    if (!resourceMap.ResourceMapDataLAB.PosMap.ContainsKey(posLAB)) continue;

                    string resName = resourceMap.ResourceMapDataLAB.PosMap[posLAB];
                    if (string.IsNullOrEmpty(resName)) continue;
                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resName, out ItemData itemData)) continue;
                    if (AWorkerTask.ItemTypeProvider(itemData.Id) != AItem.ItemTypeEnum.Food) continue;

                    return new ResourceInfo(itemData.Id);
                }
            }

            return null;
        }

        /// <summary>
        /// 在指定范围内查找特定资源 ID 的位置。
        /// </summary>
        private Vector3Int? FindResourcePosition(Vector3Int workerPos, int itemId, int radius)
        {
            var resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            if (resourceMap?.ResourceMapDataLAB?.PosMap == null) return null;

            Vector3Int? best = null;
            float bestDist = float.MaxValue;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector3Int pos = new Vector3Int(workerPos.x + dx, workerPos.y + dy, 0);
                    Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
                    if (!resourceMap.ResourceMapDataLAB.PosMap.ContainsKey(posLAB)) continue;

                    string resName = resourceMap.ResourceMapDataLAB.PosMap[posLAB];
                    if (string.IsNullOrEmpty(resName)) continue;
                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resName, out ItemData itemData)) continue;
                    if (itemData.Id != itemId) continue;

                    float dist = (pos - workerPos).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = pos;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// 创建自我睡觉任务（需要找到床）。
        /// </summary>
        private void CreateSelfSleepTask()
        {
            // 从 FurnitureManager 查找分配给该 Worker 的床位置
            Vector3Int bedPos = this.FindBedPosition();

            if (bedPos != default)
            {
                WorkerSleepTask sleepTask = new WorkerSleepTask.SleepTaskBuilder()
                    .SetTarget(bedPos)
                    .SetWorker(this.Character)
                    .Build();

                AWorkerTask.TaskAddProvider(
                    sleepTask,
                    new GameGridPosition(bedPos.x, bedPos.y, bedPos.z),
                    1); // 高优先级

                AWorkerTask.LogProvider(
                    $"{this.Character.name} 创建睡觉任务 pos=({bedPos.x},{bedPos.y})",
                    LogManager.LogLevelEnum.Info);
            }
            else
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 有床但找不到床位",
                    LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
            }
        }

        /// <summary>
        /// 查找 Worker 的床位。
        /// 优先从 WorkerData.HomePosition 读取（O(1)），无记录时 fallback 到 FurnitureManager 遍历。
        /// </summary>
        private Vector3Int FindBedPosition()
        {
            // 优先：从持久化的 HomePosition 读取（null = 无家）
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (wd?.HomePosition != null)
            {
                return Vector3IntLAB.ToVector3Int(wd.HomePosition);
            }

            // Fallback：遍历 FurnitureManager
            var furnitureManager = Core.ServiceLocator.Get<Item.FurnitureManager>();
            if (furnitureManager?.BedToWorker == null)
            {
                return default;
            }

            foreach (KeyValuePair<Vector3Int, AWorker> kv in furnitureManager.BedToWorker)
            {
                if (kv.Value == this.Character)
                {
                    return kv.Key;
                }
            }

            return default;
        }

        /// <summary>
        /// 创建自我拾取任务 — 从地面捡起属于自己的物品直接放入背包。
        /// 使用 WorkerPickUpFromBoardTask 的 FromGround 模式，不走复杂的 Carry 两阶段流程。
        /// </summary>
        private void CreateSelfCarryTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default)
            {
                this.CreateIdleTask();
                return;
            }

            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            int ownerId = this.Character.GetInstanceID();

            WorkerPickUpFromBoardTask pickUpTask = new WorkerPickUpFromBoardTask.PickUpFromBoardTaskBuilder()
                .SetMode(WorkerPickUpFromBoardTask.PickUpMode.FromGround)
                .SetTargetPosition(decision.TargetPosition)
                .SetGroundResource(decision.Resource)
                .SetOwnerId(ownerId)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池
            workerData.Task = pickUpTask;
            pickUpTask.Start(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我拾取任务: 捡 id={decision.Resource?.Id} pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 创建去任务栏拾取任务 — 去任务栏取回属于自己的悬赏物品。
        /// 直接分配给当前 Worker（不入全局池），确保物品不会被其他人拿走。
        /// </summary>
        private void CreatePickUpFromBoardTask(WorkerBrain.Decision decision)
        {
            var boardManager = Core.ServiceLocator.Get<Gameplay.TaskBoardManager>();
            if (boardManager == null || !boardManager.IsInitialized)
            {
                AWorkerTask.LogProvider("任务栏未初始化，无法创建拾取任务", LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            int ownerId = this.Character.GetInstanceID();

            WorkerPickUpFromBoardTask pickUpTask = new WorkerPickUpFromBoardTask.PickUpFromBoardTaskBuilder()
                .SetMode(WorkerPickUpFromBoardTask.PickUpMode.FromBoard)
                .SetBoardNeighbor(boardManager.GetNeighborPosition())
                .SetOwnerId(ownerId)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池
            workerData.Task = pickUpTask;
            pickUpTask.Start(this.Character);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建任务栏拾取任务: pos=({boardManager.BoardPosition.x},{boardManager.BoardPosition.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 自动出售资源给市场（经济闭环关键步骤）。
        /// 阶梯式按比例出售：持有越多卖越多，保留合理储备。
        /// </summary>
        private void TryAutoSellResources(AWorker.WorkerData workerData)
        {
            // 事业心低的人不急着卖钱
            if (workerData.Personality.Ambition < 40f) return;

            List<ResourceInfo> allResources = this.Character.GetAllResources();
            List<ResourceInfo> sellList = new List<ResourceInfo>();
            int totalSellCount = 0;

            bool isHungry = workerData.CurHungry < AWorker.ThresholdHungry;
            bool hasBuildGoal = workerData.CurrentGoal.Type == WorkerGoalType.BuildStructure
                && workerData.CurrentGoal.HasMaterialNeeds;

            foreach (var r in allResources)
            {
                if (r.Count <= 0) continue;

                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                int keepReserve = this.GetReserveCount(r.Id, itemType, isHungry, hasBuildGoal, workerData);
                int excess = r.Count - keepReserve;

                if (excess <= 0) continue;

                // 根据持有量计算出售比例：持有越多卖越多
                float sellRatio = this.GetSellRatio(r.Count, itemType, isHungry);
                int sellCount = UnityEngine.Mathf.RoundToInt(excess * sellRatio);

                if (sellCount > 0)
                {
                    sellList.Add(new ResourceInfo(r.Id, sellCount, r.OwnerId));
                    totalSellCount += sellCount;
                }
            }

            if (sellList.Count == 0) return;

            // 触发条件：携带量超过60%或有3种以上可售资源
            int totalCarried = 0;
            foreach (var r in allResources) totalCarried += r.Count;
            float carryRatio = (float)totalCarried / workerData.MaxResourceCount;
            if (carryRatio <= 0.6f && sellList.Count < 3) return;

            // 执行出售
            if (UnityEngine.Random.value < 0.5f)
            {
                Gameplay.MarketService market = Core.ServiceLocator.Get<Gameplay.MarketService>();
                if (market != null)
                {
                    int earned = market.WorkerAutoSellFiltered(this.Character, sellList);
                    if (earned > 0)
                    {
                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 出售{totalSellCount}个资源({sellList.Count}种)获得{earned}G (总携带{totalCarried}/{workerData.MaxResourceCount})",
                            LogManager.LogLevelEnum.Info);
                    }
                }
            }
        }

        /// <summary>
        /// 获取某种资源的保留数量（低于此数不卖）。
        /// </summary>
        private int GetReserveCount(int itemId, AItem.ItemTypeEnum itemType, bool isHungry, bool hasBuildGoal, AWorker.WorkerData wd)
        {
            // 建造目标所需的材料完全保留
            if (hasBuildGoal && wd.CurrentGoal.RequiredMaterials.ContainsKey(itemId))
                return int.MaxValue;

            switch (itemType)
            {
                case AItem.ItemTypeEnum.Food:
                    // 饥饿时保留更多食物
                    return isHungry ? 10 : 5;
                case AItem.ItemTypeEnum.Seed:
                    return 3;
                case AItem.ItemTypeEnum.Material:
                    return 8;
                case AItem.ItemTypeEnum.Equipment:
                case AItem.ItemTypeEnum.Weapon:
                    return 1; // 装备/武器只保留1件
                default:
                    return 3;
            }
        }

        /// <summary>
        /// 根据持有量计算出售比例：持有越多卖越多。
        /// 饥饿时食物出售比例减半。
        /// </summary>
        private float GetSellRatio(int totalCount, AItem.ItemTypeEnum itemType, bool isHungry)
        {
            float ratio;

            switch (itemType)
            {
                case AItem.ItemTypeEnum.Food:
                    // 食物：5-10卖10%, 10-20卖25%, 20-40卖40%, 40+卖60%
                    if (totalCount <= 5) ratio = 0f;
                    else if (totalCount <= 10) ratio = 0.10f;
                    else if (totalCount <= 20) ratio = 0.25f;
                    else if (totalCount <= 40) ratio = 0.40f;
                    else ratio = 0.60f;
                    // 饥饿时折扣出售意愿
                    if (isHungry) ratio *= 0.3f;
                    break;

                case AItem.ItemTypeEnum.Material:
                    // 材料：0-10不卖, 10-20卖15%, 20-40卖35%, 40+卖55%
                    if (totalCount <= 10) ratio = 0f;
                    else if (totalCount <= 20) ratio = 0.15f;
                    else if (totalCount <= 40) ratio = 0.35f;
                    else ratio = 0.55f;
                    break;

                case AItem.ItemTypeEnum.Seed:
                    // 种子：0-3不卖, 3-8卖20%, 8+卖40%
                    if (totalCount <= 3) ratio = 0f;
                    else if (totalCount <= 8) ratio = 0.20f;
                    else ratio = 0.40f;
                    break;

                case AItem.ItemTypeEnum.Equipment:
                case AItem.ItemTypeEnum.Weapon:
                    // 装备武器：多于1件才卖
                    ratio = totalCount > 1 ? 0.5f : 0f;
                    break;

                default:
                    // 其他：0-5不卖, 5-15卖25%, 15-30卖45%, 30+卖65%
                    if (totalCount <= 5) ratio = 0f;
                    else if (totalCount <= 15) ratio = 0.25f;
                    else if (totalCount <= 30) ratio = 0.45f;
                    else ratio = 0.65f;
                    break;
            }

            return Mathf.Clamp01(ratio);
        }

        /// <summary>
        /// 创建默认空闲任务（锻炼）。
        /// </summary>
        private void CreateIdleTask()
        {
            AWorkerTask.TaskAddProvider(
                new WorkerExerciseTask.ExerciseTaskBuilder()
                    .SetTarget(this.targetMap)
                    .SetWorker(this.Character)
                    .Build(),
                new GameGridPosition(0, 0, 0),
                3);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 每60帧刷新一次
            if (Time.frameCount % 60 == 0)
            {
                this.builder.Clear();
                this.Character.WorkerStateText.text = this.builder.Append(this.preString)
                    .Append("<color=" + PixelUITheme.RichGold + ">Seeking: ")
                    .Append(MathHelper.RoundToInt(this.Character.Seek.SeekProgress * 100))
                    .Append("%</color>\nTarget: ")
                    .Append(this.targetMap.x)
                    .Append(",")
                    .Append(this.targetMap.y)
                    .ToString();
            }

            // if (Worker.SeekLock.GetLock(this.Character))
            // {
            //     // 使用协程时,只能有一个在寻路(加锁),如果被锁了且锁的拥有者不是自己则阻塞,可重入
            //     if (this.isOne)
            //     {
            //         this.isOne = false;
            //         this.Character.ToTarget();
            //     }
            // }

            // // 只能有一个在寻路
            // if (this.isOne)
            // {
            //     this.isOne = false;
            //     this.Character.ToTarget();
            // }
            if (!this.Character.Seek.IsSeeking())
            {
                // 没有找到路
                if (!this.Character.Seek.IsHavePath())
                {
                    // 如果有任务
                    AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
                    if (workerData.Task != null)
                    {
                        this.Character.GiveUpTask();
                    }
                    else
                    {
                        this.Character.Manager.ChangeState(AWorkerState.TypeEnum.Seek);
                    }

                    AWorkerTask.LogProvider(this.Character.name + " 没有找到路!", LogManager.LogLevelEnum.Trace);
                    return;
                }

                // Worker.SeekLock.ReleaseLock(this.Character);
                // 寻路结束
                this.Character.Manager.ChangeState(TypeEnum.Move);
            }
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
