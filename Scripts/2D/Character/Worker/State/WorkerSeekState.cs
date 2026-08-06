namespace LAB2D.Character.Worker.State
{
    using LAB2D;
    using LAB2D.AI.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using LAB2D.Item;
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

            // 作为自己的任务加入（优先级 2，中优先级）
            AWorkerTask.TaskAddProvider(
                gatherTask,
                new GameGridPosition(
                    decision.TargetPosition.x,
                    decision.TargetPosition.y,
                    decision.TargetPosition.z),
                2);

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
        /// 创建自我建造任务 — 自己去建造。
        /// </summary>
        private void CreateSelfBuildTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default || decision.NeededResources == null)
            {
                AWorkerTask.LogProvider("SelfBuild: 无有效建造位置", LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            WorkerBuildTask buildTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuildPos(decision.TargetPosition)
                .SetNeedResource(decision.NeededResources)
                .Build();

            AWorkerTask.TaskAddProvider(
                buildTask,
                new GameGridPosition(
                    decision.TargetPosition.x,
                    decision.TargetPosition.y,
                    decision.TargetPosition.z),
                2);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我建造任务: pos=({decision.TargetPosition.x},{decision.TargetPosition.y}) tile={decision.BuildTileName}",
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

            AWorkerTask.TaskAddProvider(
                plantTask,
                new GameGridPosition(taskPos.x, taskPos.y, taskPos.z),
                2);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我种植任务: pos=({taskPos.x},{taskPos.y})",
                LogManager.LogLevelEnum.Info);
        }

        /// <summary>
        /// 创建自我吃饭任务 — 自己吃自己的，或向其他 Worker 购买。
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
            }
            else
            {
                AWorkerTask.LogProvider(
                    $"{this.Character.name} 无法解决饥饿，继续空闲",
                    LogManager.LogLevelEnum.Warning);
            }

            this.CreateIdleTask();
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
        /// 从 FurnitureManager 查找 Worker 的床位。
        /// </summary>
        private Vector3Int FindBedPosition()
        {
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
        /// 创建自我搬运任务 — 去捡地上属于自己的物品（悬赏得来）。
        /// </summary>
        private void CreateSelfCarryTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default)
            {
                this.CreateIdleTask();
                return;
            }

            WorkerCarryTask carryTask = new WorkerCarryTask.CarryTaskBuilder()
                .SetStartTarget(decision.TargetPosition)
                .SetResourceInfo(decision.Resource)
                .Build();

            AWorkerTask.TaskAddProvider(
                carryTask,
                new GameGridPosition(
                    decision.TargetPosition.x,
                    decision.TargetPosition.y,
                    decision.TargetPosition.z),
                2);

            AWorkerTask.LogProvider(
                $"{this.Character.name} 创建自我搬运任务: 捡 id={decision.Resource?.Id} pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
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
        /// 当 Worker 携带有资源且事业心足够时，自动套现。
        /// </summary>
        private void TryAutoSellResources(AWorker.WorkerData workerData)
        {
            // 事业心低的人不急着卖钱
            if (workerData.Personality.Ambition < 40f)
            {
                return;
            }

            int totalCarried = 0;
            List<ResourceInfo> allResources = this.Character.GetAllResources();
            foreach (var r in allResources)
            {
                totalCarried += r.Count;
            }

            // 携带量超过 60% 或携带超过 3 种不同资源时触发自动出售
            float carryRatio = (float)totalCarried / workerData.MaxResourceCount;
            bool shouldSell = carryRatio > 0.6f || allResources.Count >= 3;

            if (!shouldSell)
            {
                return;
            }

            // 随机出售部分资源（模拟决策：不全卖，留一些建造用）
            if (UnityEngine.Random.value < 0.5f)
            {
                Gameplay.MarketService market = Core.ServiceLocator.Get<Gameplay.MarketService>();
                if (market != null)
                {
                    int earned = market.WorkerAutoSellAll(this.Character);
                    if (earned > 0)
                    {
                        AWorkerTask.LogProvider(
                            $"{this.Character.name} 自动出售资源获得 {earned}G (携带 {totalCarried}/{workerData.MaxResourceCount})",
                            LogManager.LogLevelEnum.Info);
                    }
                }
            }
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
