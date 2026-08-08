namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core.Seek;
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
        private long lastDecisionAtSeekTimes; // 上次决策时的 seekTimes，用于防止快速重复决策

        public WorkerSeekState(AWorker character)
            : base(character)
        {
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();

            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            bool targetExplicitlySet = false;
            bool skipFinalSeek = false;
            if (workerData.Task != null)
            {
                // 有任务时隐藏内心独白
                this.Character.HideDialogText();
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
                targetExplicitlySet = true;
            }
            else
            {
                // 没有任务 → 自主决策
                AWorkerTask.LogProvider(this.Character.name + " 没有任务!", LogManager.LogLevelEnum.Trace);
                ++this.seekTimes;

                // 漫游到达处理：到达路点后恢复精气神+心情，继续或结束漫游
                if (workerData.WanderWaypointsRemaining > 0)
                {
                    workerData.WanderWaypointsRemaining--;
                    float restoreAmount = Constant.WorkerConditionConstant.SpiritWanderRestorePerSecond
                        * WorkerTaskTimeConfig.WanderSeconds;
                    workerData.CurSpirit = System.Math.Min(workerData.MaxSpirit,
                        workerData.CurSpirit + restoreAmount);
                    workerData.Personality = workerData.Personality.AfterWander();

                    // 小概率(5%)发现随机物品
                    if (UnityEngine.Random.value < 0.05f)
                    {
                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 漫游中发现了一些东西!",
                            LogManager.LogLevelEnum.Info);
                        // TODO: 可通过 DropManager 在附近生成随机基础资源
                    }

                    if (workerData.WanderWaypointsRemaining > 0)
                    {
                        // 继续漫游：选下一个路点，不触发正常决策
                        Vector3Int currentPos = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
                        this.targetMap = AWorkerTask.GenCanReachPosProvider(currentPos);
                        this.Character.Seek.Seek(this.targetMap);
                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 继续漫游 剩余{workerData.WanderWaypointsRemaining}路点 精气神={workerData.CurSpirit:F0}",
                            LogManager.LogLevelEnum.Trace);
                        return;
                    }
                    else
                    {
                        // 漫游结束，精气神已恢复，正常决策
                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 漫游结束 精气神={workerData.CurSpirit:F0}",
                            LogManager.LogLevelEnum.Info);
                    }
                }

                // 首次 Seek 时选定建家位置
                if (this.seekTimes == 1)
                {
                    this.brain.TryPickHomeSite(this.Character);
                }

                // 判断是否应该立即决策：
                // 决策间隔根据 LifeStage 调整：Bootstrap=2, Settled=5, Established=8
                int decisionInterval = workerData.LifeStage switch
                {
                    Domain.Worker.WorkerLifeStage.Bootstrap => 2,
                    Domain.Worker.WorkerLifeStage.Settled => 5,
                    Domain.Worker.WorkerLifeStage.Established => 8,
                    _ => 5,
                };
                bool isPeriodic = this.seekTimes % decisionInterval == 0;
                bool isQuickReeval = false;
                if (!isPeriodic && workerData.Task == null)
                {
                    // 所有阶段统一：距上次决策 >= 2 次 seek 即可快速重评
                    // 防止 Settled/Established Worker 长时间卡在 Exercise 状态
                    bool cooldownPassed = (this.seekTimes - this.lastDecisionAtSeekTimes) >= 2;
                    isQuickReeval = cooldownPassed;
                }

                // 任务完成后强制立即决策，跳过无意义的漫游间隔
                if (workerData.ForceDecisionOnNextSeek)
                {
                    workerData.ForceDecisionOnNextSeek = false;
                    isQuickReeval = true;
                }

                if (isPeriodic || isQuickReeval)
                {
                    this.lastDecisionAtSeekTimes = this.seekTimes;

                    // 周期性决策时尝试自动出售资源（经济闭环：资源→金币）
                    if (isPeriodic)
                    {
                        this.TryAutoSellResources(workerData);
                    }

                    // 做自主决策
                    this.ExecuteAutonomousDecision(workerData);

                    // 决策创建了任务 → Start() 内部 ChangeState(Seek) 已触发重入 OnEnter
                    // 重入的 OnEnter 已完成寻路到邻居位置，这里不覆盖 targetMap，不重复 Seek
                    if (workerData.Task != null)
                    {
                        targetExplicitlySet = true;
                        skipFinalSeek = true;
                    }
                }

                // 没有任务且决策未创建新任务 → 生成随机路点进行漫游
                if (!targetExplicitlySet)
                {
                    this.targetMap = AWorkerTask.GenCanReachPosProvider(posMap);
                }

                // 连续 30 次以上无任务寻路 → 游手好闲惩罚：勤奋↓ 心情↓
                if (this.seekTimes > 0 && this.seekTimes % 30 == 0 && workerData != null)
                {
                    workerData.Personality = workerData.Personality.AfterIdle();
                }
            }

            if (!skipFinalSeek)
            {
                AWorkerTask.LogProvider(this.Character.name + " 寻路->" + this.targetMap, LogManager.LogLevelEnum.Trace);
                this.Character.Seek.Seek(this.targetMap);
            }
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
                case WorkerDecisionType.GroundSleep:
                    this.CreateSelfSleepTask();
                    break;

                case WorkerDecisionType.Wander:
                    this.CreateWanderTask();
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

            // SetTarget 中 AddGather 认领失败 → 资源被其他 Worker 抢先认领
            if (gatherTask == null)
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 采集目标已被其他Worker认领, 放弃: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                    LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

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
            // 先在 BuildMap 中注册建造位置，成功后再发悬赏（避免位置冲突后无法退款）
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                bool reserved = Core.ServiceLocator.Get<BuildMap>().ReserveBuildPosition(
                    decision.TargetPosition, decision.BuildTileName);
                if (!reserved) return false;
            }

            if (!currencyManager.PostBounty(issuerId, reward))
            {
                // 发悬赏失败 → 释放刚预约的建造位置（仅释放 IsComplete=false 的预约）
                // 注：暂不实现复杂的回滚逻辑，BuildMap 中的未完成预约会随 Worker 重新选址自然被覆盖
                return false;
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

            // 建造悬赏不推进建家阶段（悬赏可能过期），等下次决策时重新评估
            // this.AdvanceHomeBuildStage(wd, decision.BuildTileName);

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

            // 在 BuildMap 中注册建造位置（仅注册不创建任务，避免 AddBuild 内部自动创建重复任务）
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                bool reserved = Core.ServiceLocator.Get<BuildMap>().ReserveBuildPosition(
                    decision.TargetPosition, decision.BuildTileName);
                if (!reserved)
                {
                    // 位置被其他 Worker 占用 → 重新选址
                    AWorkerTask.LogProvider(
                        $"{this.Character.name} 建造位置已被占用, 重新选址: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                        LogManager.LogLevelEnum.Warning);
                    AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;
                    if (wd != null)
                    {
                        wd.PlannedHomePosition = null;
                        wd.HomeBuildStage = 0;
                    }
                    this.CreateIdleTask();
                    return;
                }
            }

            WorkerBuildTask buildTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuildPos(decision.TargetPosition)
                .SetNeedResource(decision.NeededResources)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池，确保自己建造自己的房子
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;
            workerData.Task = buildTask;
            buildTask.Start(this.Character);

            // 无家者建完房间后推进到下一阶段（建床）
            this.AdvanceHomeBuildStage(workerData, decision.BuildTileName);

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
        /// 创建自我睡觉任务 — 有床优先床，无床原地地面睡眠。
        /// </summary>
        private void CreateSelfSleepTask()
        {
            AWorker.WorkerData workerData = this.Character.CharacterDataLAB as AWorker.WorkerData;

            // 从 FurnitureManager 查找分配给该 Worker 的床位置
            Vector3Int bedPos = this.FindBedPosition();

            if (bedPos != default)
            {
                // 有床：直接分配给自己执行
                WorkerSleepTask sleepTask = new WorkerSleepTask.SleepTaskBuilder()
                    .SetTarget(bedPos)
                    .SetWorker(this.Character)
                    .Build();

                workerData.Task = sleepTask;
                sleepTask.Start(this.Character);

                AWorkerTask.LogProvider(
                    $"{this.Character.name} 创建睡觉任务(有床) pos=({bedPos.x},{bedPos.y})",
                    LogManager.LogLevelEnum.Info);
            }
            else
            {
                // 无床：原地地面睡眠
                Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
                WorkerSleepTask sleepTask = new WorkerSleepTask.SleepTaskBuilder()
                    .SetTarget(posMap)
                    .SetWorker(this.Character)
                    .Build();

                workerData.Task = sleepTask;
                sleepTask.Start(this.Character);

                AWorkerTask.LogProvider(
                    $"{this.Character.name} 创建地面睡觉任务(无床) pos=({posMap.x},{posMap.y})",
                    LogManager.LogLevelEnum.Info);
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

        /// <summary>
        /// 创建漫游任务 — 多路点持续漫游。
        /// 随机走向远处位置，每到达一个路点恢复精气神和心情。
        /// 与 Idle 的区别：Idle 是原地锻炼（不动），漫游是主动探索走动。
        /// </summary>
        private void CreateWanderTask()
        {
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return;

            // 初始化漫游路点计数（3-5个路点，约18-30秒的漫游）
            if (wd.WanderWaypointsRemaining <= 0)
            {
                wd.WanderWaypointsRemaining = UnityEngine.Random.Range(3, 6);
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 开始漫游 ({wd.WanderWaypointsRemaining} 个路点), 精气神={wd.CurSpirit:F0}",
                    LogManager.LogLevelEnum.Info);
            }

            // 选一个较远的随机可到达位置（漫游半径比普通寻路更大）
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.Character.transform.position);
            Vector3Int wanderTarget = default;
            float bestDist = 0f;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                int dx = UnityEngine.Random.Range(-20, 21);
                int dy = UnityEngine.Random.Range(-20, 21);
                Vector3Int candidate = new Vector3Int(posMap.x + dx, posMap.y + dy, 0);
                if (ASeek.IsCanReach(candidate))
                {
                    float dist = (candidate - posMap).sqrMagnitude;
                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        wanderTarget = candidate;
                    }
                }
            }

            if (wanderTarget == default)
                wanderTarget = AWorkerTask.GenCanReachPosProvider(posMap);

            this.targetMap = wanderTarget;

            // 每到达一个路点恢复少量精气神（到达时在下次 OnEnter 中处理）
            // 这里先不做恢复，等到达后再处理
            AWorkerTask.LogProvider(
                $"{this.Character.name} 漫游 → ({wanderTarget.x},{wanderTarget.y}) 剩余{wd.WanderWaypointsRemaining}路点",
                LogManager.LogLevelEnum.Trace);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            base.OnUpdate();

            // 紧急检测（每10帧执行一次，约167ms延迟，不可感知）
            AWorker.WorkerData wd = this.Character.CharacterDataLAB as AWorker.WorkerData;
            if (Time.frameCount % 10 == 0 && wd != null && !this.Character.IsDialoguePaused)
            {
                bool emergency = false;

                // 饥饿 < 15 → 强制触发生存决策
                if (wd.CurHungry > 0 && wd.CurHungry < 15f)
                {
                    if (wd.Task != null && wd.Task.TaskType != WorkerTaskType.Eat)
                    {
                        this.Character.GiveUpTask();
                        emergency = true;
                    }
                }

                // 疲劳 < 15 → 强制触发睡觉决策
                if (wd.CurTired > 0 && wd.CurTired < 15f)
                {
                    if (wd.Task != null
                        && wd.Task.TaskType != WorkerTaskType.Sleep
                        && wd.Task.TaskType != WorkerTaskType.GroundSleep)
                    {
                        this.Character.GiveUpTask();
                        emergency = true;
                    }
                }

                // 精气神 < 10 → 强制触发漫游/休息决策
                if (wd.CurSpirit > 0 && wd.CurSpirit < 10f)
                {
                    if (wd.Task != null
                        && wd.Task.TaskType != WorkerTaskType.Wander
                        && wd.Task.TaskType != WorkerTaskType.Sleep)
                    {
                        this.Character.GiveUpTask();
                        emergency = true;
                    }
                }

                if (emergency)
                {
                    this.ExecuteAutonomousDecision(wd);
                    this.Character.Seek.Seek(this.targetMap);
                    return;
                }
            }

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

            if (!this.Character.Seek.IsSeeking())
            {
                // 没有找到路
                if (!this.Character.Seek.IsHavePath())
                {
                    // 记录寻路失败位置，防止短时间内重复尝试同一不可达目标
                    ASeek.RecordFail(this.targetMap);

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

        /// <summary>
        /// 无家者建造任务创建后推进建家阶段。
        /// 阶段 0-6：墙壁，阶段 7：床，阶段 8：完成。
        /// </summary>
        private void AdvanceHomeBuildStage(AWorker.WorkerData wd, string buildTileName)
        {
            if (wd == null || wd.HomePosition != null) return;

            const int wallCount = 15;      // 与 WorkerBrain.WallCount 一致
            const int completeStage = 17;  // 完成
            int prevStage = wd.HomeBuildStage;
            wd.HomeBuildStage++;

            if (buildTileName.StartsWith("CustomRoomWall") && wd.HomeBuildStage < wallCount)
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 建家: 墙壁{prevStage + 1}/{wallCount} → 下一块",
                    LogManager.LogLevelEnum.Info);
            }
            else if (buildTileName.StartsWith("CustomRoomWall") && wd.HomeBuildStage >= wallCount)
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 建家: 墙壁完成 → 接下来建门",
                    LogManager.LogLevelEnum.Info);
            }
            else if (buildTileName == "CustomDoor")
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 建家: 门完成 → 接下来建床",
                    LogManager.LogLevelEnum.Info);
            }
            else if (buildTileName == "SingleBed")
            {
                wd.HomeBuildStage = completeStage;
                wd.LifeStage = Domain.Worker.WorkerLifeStage.Settled;
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 建家: 床完成 → 有家了! → Settled 阶段",
                    LogManager.LogLevelEnum.Info);

                // 将房间注册到 RoomManager（所有墙壁和门已建完）
                this.RegisterWorkerRoom(wd);

                // 自动绑定床到当前 Worker（床位置 = 房间中心 = PlannedHomePosition）
                if (wd.PlannedHomePosition != null)
                {
                    Vector3Int bedPos = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
                    var fm = Core.ServiceLocator.Get<Item.FurnitureManager>();
                    fm.AddBed(bedPos);
                    fm.AddWorkerToBed(bedPos, this.Character);
                    AWorkerTask.LogProvider(
                        $"{this.Character.name} 床已自动绑定: pos=({bedPos.x},{bedPos.y})",
                        LogManager.LogLevelEnum.Info);
                }
            }
        }

        /// <summary>
        /// 建家完成后，将所有墙壁和门注册到 RoomManager。
        /// </summary>
        private void RegisterWorkerRoom(AWorker.WorkerData wd)
        {
            if (wd?.PlannedHomePosition == null) return;

            Vector3Int center = Vector3IntLAB.ToVector3Int(wd.PlannedHomePosition);
            var roomInfo = new LAB2D.Item.RoomInfo();
            var wallOffsets = LAB2D.AI.Worker.WorkerBrain.GetWallOffsets();

            // 收集所有墙壁位置
            for (int i = 0; i < LAB2D.AI.Worker.WorkerBrain.WallCount; i++)
            {
                roomInfo.Points.Add(center + wallOffsets[i]);
            }

            // 门位置
            roomInfo.Points.Add(center + LAB2D.AI.Worker.WorkerBrain.DoorOffset);

            // 所有点都已建完，进度为 0
            roomInfo.Progress = 0;
            roomInfo.Temperature = 25.0f;
            roomInfo.Humidity = 25.0f;

            Core.ServiceLocator.Get<LAB2D.Item.RoomManager>().AddRoom(
                System.Guid.NewGuid().ToString(), roomInfo);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 房间已注册: {roomInfo.Points.Count} 个墙壁/门位置",
                LogManager.LogLevelEnum.Info);
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
