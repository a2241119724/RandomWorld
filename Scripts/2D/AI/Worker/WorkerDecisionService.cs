namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
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
    using UnityEngine;

    /// <summary>
    /// Worker 决策服务 — 三段式任务接取（玩家悬赏 → 全局任务 → WorkerBrain 自主决策）
    /// 与 Create*Task 家族。自 WorkerSeekState 迁出（活动层/决策层解耦）。
    /// 每 Worker 一个实例（持有决策簿记：brain、seekTimes、上次决策点、当前目标格），
    /// 由 WorkerSeekState 持有并驱动。
    /// </summary>
    public class WorkerDecisionService
    {
        private readonly WorkerBrain brain = new WorkerBrain(); // 自主决策引擎
        private readonly AWorker worker;
        private Vector3Int currentTargetMap; // 当前目标格（原 WorkerSeekState.targetMap 在无任务决策路径中的角色：CreateIdle/CreateWander 读写，经 SyncTargetMap 与状态层保持一致）
        private long seekTimes; // 没有任务寻路的次数
        private long lastDecisionAtSeekTimes; // 上次决策时的 seekTimes，用于防止快速重复决策

        public WorkerDecisionService(AWorker worker)
        {
            this.worker = worker;
        }

        /// <summary>
        /// 同步状态层的 targetMap（跨进入持久语义：CreateIdleTask 的锻炼目标取自上次的寻路目标）。
        /// </summary>
        public void SyncTargetMap(Vector3Int targetMap)
        {
            this.currentTargetMap = targetMap;
        }

        /// <summary>
        /// 三段式任务接取 + 漫游簿记（原 WorkerSeekState.OnEnter 无任务分支整体迁入）。
        /// 返回 true：已产生任务（任务 Start 内部 ChangeState(Seek) 重入完成寻路）
        /// 或漫游继续已自行寻路 — 调用方不要再 Seek。
        /// 返回 false：无任务，moveTarget 为随机漫游路点 — 调用方 Seek(moveTarget)。
        /// </summary>
        public bool TryAcquireTask(out Vector3Int moveTarget)
        {
            AWorker.WorkerData workerData = this.worker.CharacterDataLAB as AWorker.WorkerData;
            moveTarget = default;

            // 没有任务 → 自主决策
            ++this.seekTimes;

            // 漫游到达处理：到达路点后恢复精气神+心情，继续或结束漫游
            if (workerData.WanderWaypointsRemaining > 0)
            {
                workerData.WanderWaypointsRemaining--;
                float restoreAmount = Constant.WorkerConditionConstant.SpiritWanderRestorePerSecond
                    * WorkerTaskTimeConfig.WanderSeconds;
                workerData.CurSpirit = System.Math.Min(workerData.MaxSpirit,
                    workerData.CurSpirit + restoreAmount);
                workerData.CurStress = System.Math.Max(
                    0.0f,
                    workerData.CurStress - Constant.WorkerConditionConstant.StressWanderRestorePerWaypoint);
                workerData.Personality = workerData.Personality.AfterWander();

                // 小概率(5%)发现随机物品
                if (UnityEngine.Random.value < 0.05f)
                {
                    AWorkerTask.LogProvider(
                        $"{this.worker.name} 漫游中发现了一些东西!",
                        LogManager.LogLevelEnum.Debug);
                    // TODO: 可通过 DropManager 在附近生成随机基础资源
                }

                if (workerData.WanderWaypointsRemaining > 0)
                {
                    // 继续漫游：选下一个路点，不触发正常决策
                    Vector3Int currentPos = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
                    this.currentTargetMap = AWorkerTask.GenCanReachPosProvider(currentPos);
                    this.worker.Seek.Seek(this.currentTargetMap);
                    AWorkerTask.LogProvider(
                        $"{this.worker.name} 继续漫游 剩余{workerData.WanderWaypointsRemaining}路点 精气神={workerData.CurSpirit:F0}",
                        LogManager.LogLevelEnum.Trace);
                    return true;
                }
                else
                {
                    // 漫游结束，精气神已恢复，正常决策
                    AWorkerTask.LogProvider(
                        $"{this.worker.name} 漫游结束 精气神={workerData.CurSpirit:F0}",
                        LogManager.LogLevelEnum.Debug);
                }
            }

            // 首次 Seek 时选定建家位置
            if (this.seekTimes == 1)
            {
                this.brain.TryPickHomeSite(this.worker);
            }

            // 步骤1: 始终优先检查玩家悬赏任务（优先级 0）。
            // 心智门（体验门）：先看是否存在可派发的玩家悬赏，再评估该 Worker 的接受意愿。
            // 接受（或生存硬阻断）→ 尝试接取；自主拒绝/拖延 → 记录怨恨累积 + 弹理由气泡，保持自主。
            // 注：即使 Worker 饥饿，TryAssignPlayerTask 内部也会因 BlocksWhenHungry 对非 Eat 任务
            // 返回 false，饥饿 Worker 只会接到玩家发布的 Eat 相关任务。
            WorkerTaskManager taskManager = Core.ServiceLocator.Get<WorkerTaskManager>();
            if (taskManager.HasAssignablePlayerTask(this.worker))
            {
                WorkerMindService mindService = Core.ServiceLocator.Get<WorkerMindService>();
                string reasonKey;
                CommandAcceptance acceptance =
                    mindService.EvaluateCommand(this.worker, UnityEngine.Random.value, out reasonKey);

                if (acceptance == CommandAcceptance.Accept
                    || reasonKey == Constant.WorkerMindConstant.ReasonSurvival)
                {
                    if (taskManager.TryAssignPlayerTask(this.worker))
                    {
                        // 成功接取玩家任务 → 跳过后续决策
                        // 真实接受（非生存硬放行）时记录：感恩+、怨恨缓和（生存放行不计数）
                        if (acceptance == CommandAcceptance.Accept)
                        {
                            mindService.RecordCommandOutcome(this.worker, true, acceptance, reasonKey);
                        }

                        return true;
                    }
                }
                else if (reasonKey != Constant.WorkerMindConstant.ReasonCooldown)
                {
                    // 自主拒绝/拖延：累积怨恨 + 弹理由气泡（冷却内静默，不重复反馈）
                    mindService.RecordCommandOutcome(this.worker, false, acceptance, reasonKey);
                    this.worker.ShowMindBubble(Constant.WorkerInnerMonologue.GetRefusalReason(acceptance, reasonKey));
                    AWorkerTask.LogProvider(
                        $"[MindDiag] {this.worker.name} 拒绝玩家悬赏 理由={reasonKey}",
                        LogManager.LogLevelEnum.Debug);
                }
            }
            else if (taskManager.TryAssignPlayerTask(this.worker))
            {
                // 成功接取玩家任务 → 跳过后续决策
                return true;
            }

            // 步骤2: 检查自保需求。饥饿或疲劳的 Worker 优先处理自身生存，
            // 不参与全局 WorkerBounty/SystemDefault 任务分配。
            bool needsSelfPreservation = workerData.CurHungry < AWorker.ThresholdHungry
                || workerData.CurTired > workerData.MaxTired - AWorker.ThresholdTired;

            if (!needsSelfPreservation)
            {
                // Worker 状态良好 → 尝试接取全局任务（优先级 1→2：WorkerBounty → SystemDefault）
                // 避免 Worker 因自主决策持续创建锻炼/漫游任务而从不接取全局队列任务
                if (Core.ServiceLocator.Get<WorkerTaskManager>().TryAssignGlobalTask(this.worker))
                {
                    return true;
                }
            }

            // 判断是否应该立即决策：
            // 决策间隔根据 LifeStage 调整：Bootstrap=3, Settled=10, Established=20
            int decisionInterval = workerData.LifeStage switch
            {
                WorkerLifeStage.Bootstrap => 3,
                WorkerLifeStage.Settled => 10,
                WorkerLifeStage.Established => 20,
                _ => 10,
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
                    return true;
                }
            }

            // 没有任务且决策未创建新任务 → 生成随机路点进行漫游
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
            moveTarget = AWorkerTask.GenCanReachPosProvider(posMap);

            // 连续 30 次以上无任务寻路 → 游手好闲惩罚：勤奋↓ 心情↓
            if (this.seekTimes > 0 && this.seekTimes % 30 == 0 && workerData != null)
            {
                workerData.Personality = workerData.Personality.AfterIdle();
            }

            return false;
        }

        /// <summary>
        /// 执行自主决策：根据人格和状态决定下一步行动。
        /// </summary>
        private void ExecuteAutonomousDecision(AWorker.WorkerData workerData)
        {
            WorkerBrain.Decision decision = this.brain.Decide(this.worker);

            AWorkerTask.LogProvider(
                $"{this.worker.name} 自主决策: {WorkerBrain.GetDecisionLabel(decision)}",
                LogManager.LogLevelEnum.Debug);

            // 决策诊断（事件点，单次字典查询构造廉价）：决策选中的目标是否仍在近期失败缓存
            // (IsRecentFail) 中。若频繁命中，说明"失败→重建任务→又失败"的冷却/失败缓存未生效，
            // 会形成死循环刷屏（历史观测：30ms 内重试 5+ 次）。
            if (decision.TargetPosition != default && ASeek.IsRecentFail(decision.TargetPosition))
            {
                AWorkerTask.LogProvider(
                    $"[StateDiag] {this.worker.name} 决策重新选中近期失败目标: {decision.Type} pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                    LogManager.LogLevelEnum.Warning);
            }

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
                        $"{this.worker.name} 准备接受悬赏任务",
                        LogManager.LogLevelEnum.Debug);
                    this.CreateIdleTask();
                    break;

                case WorkerDecisionType.SelfCarry:
                    this.CreateSelfCarryTask(decision);
                    break;

                case WorkerDecisionType.PickUp:
                    this.CreatePickUpTask(decision);
                    break;

                case WorkerDecisionType.SelfBuild:
                    this.CreateSelfBuildTask(decision);
                    break;

                case WorkerDecisionType.SelfPlant:
                    this.CreateSelfPlantTask(decision);
                    break;

                case WorkerDecisionType.Store:
                    this.CreateStorageStoreTask(decision);
                    break;

                case WorkerDecisionType.Withdraw:
                    this.CreateStorageWithdrawTask(decision);
                    break;

                case WorkerDecisionType.Eat:
                    this.CreateSelfEatTask();
                    break;

                case WorkerDecisionType.Sleep:
                case WorkerDecisionType.GroundSleep:
                    this.CreateSelfSleepTask();
                    break;

                case WorkerDecisionType.Wander:
                    // 送礼分支（自发关系）：低概率给朋友/爱慕对象送份小礼物，双方亲密度上升 + 收礼方好感微升
                    if (UnityEngine.Random.value < WorkerMindConstant.RelationGiftChance)
                    {
                        this.TryGiveGift(workerData);
                    }

                    this.CreateWanderTask();
                    break;

                case WorkerDecisionType.Idle:
                default:
                    this.CreateIdleTask();
                    break;
            }
        }

        /// <summary>
        /// 送礼（自发关系，漫游决策前低概率触发）：给一位朋友/爱慕对象送份小礼物。
        /// 双方亲密度上升 + 收礼方好感度微升（讨好）。节流：同日不重复送（LastInteractionDay）。
        /// 无送礼目标/目标不存在/已同日互动 → 静默跳过。
        /// </summary>
        private void TryGiveGift(AWorker.WorkerData workerData)
        {
            AWorker giver = this.worker;
            WorkerMindData.Ensure(workerData);
            WorkerRelationEntry target = WorkerRelationshipRuleService.FindGiftTarget(workerData.Mind);
            if (target == null)
            {
                return;
            }

            // 关系键为 name（跨存档稳定引用），解析回 AWorker 实例；解析失败跳过
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            AWorker receiver = null;
            if (wm?.Characters != null)
            {
                foreach (AWorker w in wm.Characters)
                {
                    if (w != null && w.name == target.TargetName)
                    {
                        receiver = w;
                        break;
                    }
                }
            }

            if (receiver == null)
            {
                return;
            }

            int day = this.GetGameDayIndex();
            if (target.LastInteractionDay == day)
            {
                return; // 同日已互动，不重复送
            }

            WorkerRelationshipRuleService.ModifyAffinity(
                workerData.Mind, receiver.name, WorkerMindConstant.RelationGiftAffinityGain, day);

            AWorker.WorkerData receiverData = receiver.CharacterDataLAB as AWorker.WorkerData;
            if (receiverData != null)
            {
                WorkerMindData.Ensure(receiverData);
                WorkerRelationshipRuleService.ModifyAffinity(
                    receiverData.Mind, giver.name, WorkerMindConstant.RelationGiftAffinityGain, day);
            }

            // 收礼方对送礼方好感度微升（讨好，low-frequency 防经济干扰）
            if (Core.ServiceLocator.TryGet<FavorabilityManager>(out FavorabilityManager fm))
            {
                fm.ModifyFavorability(receiver, giver.GetInstanceID(), WorkerMindConstant.RelationGiftFavorabilityDelta, "收到礼物");
            }

            giver.ShowMindBubble($"给 {receiver.name} 送了份小礼物");
            AWorkerTask.LogProvider(
                $"[MindDiag] {giver.name} 送礼给 {receiver.name}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>当前游戏日索引（沿用 FavorabilityManager 的日口径）。</summary>
        private int GetGameDayIndex()
        {
            IGameTime gt = Core.ServiceLocator.Get<IGameTime>();
            return gt == null ? 0 : (int)(gt.Time / FavorabilityConstant.GameDaySeconds);
        }

        /// <summary>
        /// 创建"回家存仓库"任务：收集全部可存物品 → 走到仓库瓦片邻居格 → 存入个人仓库。
        /// 无物可存/无可达仓库 → 记冷却并回退 Idle（决策层以此防死循环）。
        /// </summary>
        private void CreateStorageStoreTask(WorkerBrain.Decision decision)
        {
            AWorker worker = this.worker;
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;

            // 决策时已检查可达性；这里再兜底确认一次（防目标移动/建家未完成）
            Vector3Int tile = decision.TargetPosition;
            if (tile == default) tile = WorkerStorageTask.PickStorageTile(worker);
            if (tile == default)
            {
                this.CreateIdleTask();
                return;
            }

            // 收集全部可存物品（与决策一致：只存"现在不需要"的，一趟尽量腾够空间）。
            // 用一次性收集 GetDepositableResources：单件挑选器无副作用，
            // 若在 while 循环里反复调用会无限返回同一物品导致主线程挂死。
            List<ResourceInfo> deposits = worker.GetDepositableResources();

            if (deposits.Count == 0)
            {
                // 无物可存 → 记冷却防反复重试（与决策层冷却联动）
                if (wd != null) wd.LastStorageAccessFailTime = UnityEngine.Time.time;
                this.CreateIdleTask();
                return;
            }

            WorkerStorageTask store = new WorkerStorageTask.StorageTaskBuilder()
                .SetMode(WorkerStorageTask.StorageMode.Store)
                .SetTarget(tile)
                .SetDepositResources(deposits)
                .Build();

            if (wd != null)
            {
                worker.SetTask(store, WorkerTaskSource.SelfDecision);
            }
        }

        /// <summary>
        /// 创建"回家取料"任务：走到仓库瓦片邻居格 → 按 WithdrawNeeds 从个人仓库取到身上。
        /// </summary>
        private void CreateStorageWithdrawTask(WorkerBrain.Decision decision)
        {
            AWorker worker = this.worker;
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;

            Vector3Int tile = decision.TargetPosition;
            if (tile == default) tile = WorkerStorageTask.PickStorageTile(worker);
            if (tile == default || decision.WithdrawNeeds == null || decision.WithdrawNeeds.Count == 0)
            {
                this.CreateIdleTask();
                return;
            }

            WorkerStorageTask withdraw = new WorkerStorageTask.StorageTaskBuilder()
                .SetMode(WorkerStorageTask.StorageMode.Withdraw)
                .SetTarget(tile)
                .SetWithdrawNeeds(decision.WithdrawNeeds)
                .Build();

            if (wd != null)
            {
                worker.SetTask(withdraw, WorkerTaskSource.SelfDecision);
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

            WorkerGatherTask gatherTask;

            if (decision.IsTerrainDig)
            {
                gatherTask = new WorkerGatherTask.GatherTaskBuilder()
                    .SetTerrainTarget(decision.TargetPosition, decision.TerrainId)
                    .Build();
            }
            else
            {
                gatherTask = new WorkerGatherTask.GatherTaskBuilder()
                    .SetTarget(decision.TargetPosition)
                    .SetResourceInfo(decision.Resource)
                    .Build();
            }

            // SetTarget/SetTerrainTarget 中 AddGather 认领失败 → 目标被其他 Worker 抢先认领
            if (gatherTask == null)
            {
                AWorkerTask.LogProvider(
                    $"{this.worker.name} 采集/挖掘目标已被其他Worker认领, 放弃: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                    LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            // 直接分配给当前 Worker，不入全局任务池，确保自己执行
            this.worker.SetTask(gatherTask, WorkerTaskSource.SelfDecision);

            string actionName = decision.IsTerrainDig ? "挖掘" : "采集";
            AWorkerTask.LogProvider(
                $"{this.worker.name} 创建自我{actionName}任务: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 发布悬赏 — 使用 WorkerBrain 预扫描的资源位置，避免重复扫描失败。
        /// 支持 Gather 和 Build 两种悬赏类型。
        /// </summary>
        private void TryPostBounty(WorkerBrain.Decision decision)
        {
            AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;
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
                // 采集悬赏/挖掘悬赏：使用预扫描位置
                posted = bountyDecision.TryPostOneBounty(
                    this.worker, decision.TargetPosition, decision.Resource,
                    decision.IsTerrainDig, decision.TerrainId);
            }

            if (!posted)
            {
                string reason = "条件不满足";
                if (wd != null)
                {
                    int needGold = bountyDecision.BaseRewardGather + bountyDecision.MinimumWalletReserve.Gold;
                    if (!wd.Wallet.HasEnough(new CurrencyAmount(needGold)))
                        reason = $"余额不足(需要{needGold}G, 有{wd.Wallet.Gold}G)";
                    else if (wd.CurTired > wd.MaxTired - bountyDecision.TiredThresholdForBounty)
                        reason = $"太累({wd.CurTired:F0}>{wd.MaxTired - bountyDecision.TiredThresholdForBounty:F0})";
                    else if (wd.CurHungry < bountyDecision.HungryThresholdForBounty)
                        reason = $"太饿({wd.CurHungry:F0}<{bountyDecision.HungryThresholdForBounty})";
                    else
                        reason = $"概率未通过或周围无资源";
                }

                AWorkerTask.LogProvider(
                    $"{this.worker.name} 发布悬赏失败: {reason}",
                    LogManager.LogLevelEnum.Debug);

                // 失败时回退：建造→自己建，采集→自己采
                if (decision.NeededResources != null && decision.NeededResources.Count > 0)
                    this.CreateSelfBuildTask(decision);
                else if (decision.TargetPosition != default)
                    this.CreateSelfGatherTask(decision);
                else
                    this.CreateIdleTask();
            }
            else if (wd != null)
            {
                // 发布成功后记录冷却时间
                wd.LastBountyPostTime = Time.time;
            }
        }

        /// <summary>
        /// 发布建造悬赏。
        /// </summary>
        private bool TryPostBuildBounty(WorkerBrain.Decision decision, WorkerBountyDecisionService bountyDecision)
        {
            AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;

            if (!bountyDecision.ShouldPostBounty(this.worker, WorkerTaskType.Build))
                return false;

            WorkerPersonality personality = wd?.Personality ?? WorkerPersonality.Neutral;
            CurrencyAmount reward = bountyDecision.DetermineReward(WorkerTaskType.Build, personality);
            int issuerId = this.worker.GetInstanceID();

            var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
            // 先在 BuildMap 中注册建造位置，成功后再发悬赏（避免位置冲突后无法退款）
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                bool reserved = Core.ServiceLocator.Get<BuildMap>().ReserveBuildPosition(
                    decision.TargetPosition, decision.BuildTileName, this.worker.name);
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
                WorkerTaskPriority.WorkerBounty);

            // 建造悬赏不推进建家阶段（悬赏可能过期），等下次决策时重新评估
            // this.AdvanceHomeBuildStage(wd, decision.BuildTileName);

            AWorkerTask.LogProvider(
                $"{this.worker.name} 发布了建造悬赏: pos=({decision.TargetPosition.x},{decision.TargetPosition.y}) 悬赏金 {reward}",
                LogManager.LogLevelEnum.Debug);

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
            // 如果位置已在 HomeBuildStage==0 时预注册，跳过 ReserveBuildPosition
            if (!string.IsNullOrEmpty(decision.BuildTileName))
            {
                var buildMap = Core.ServiceLocator.Get<BuildMap>();
                var existingTile = buildMap?.GetBuildTileData(decision.TargetPosition);

                if (existingTile != null && !existingTile.IsComplete)
                {
                    // 位置已预注册（HomeBuildStage>0 时的正常情况），跳过注册
                }
                else
                {
                    bool reserved = Core.ServiceLocator.Get<BuildMap>().ReserveBuildPosition(
                        decision.TargetPosition, decision.BuildTileName, this.worker.name);
                    if (!reserved)
                    {
                        // 位置被其他 Worker 占用 → 重新选址
                        AWorkerTask.LogProvider(
                            $"{this.worker.name} 建造位置已被占用, 重新选址: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                            LogManager.LogLevelEnum.Warning);
                        AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;
                        if (wd != null)
                        {
                            wd.PlannedHomePosition = null;
                            wd.HomeBuildStage = 0;
                        }
                        this.CreateIdleTask();
                        return;
                    }
                }
            }

            WorkerBuildTask buildTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuildPos(decision.TargetPosition)
                .SetNeedResource(decision.NeededResources)
                .SetBuildTileName(decision.BuildTileName)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池，确保自己建造自己的房子
            this.worker.SetTask(buildTask, WorkerTaskSource.SelfDecision);

            // 注意: HomeBuildStage 的推进已移至 WorkerBuildTask.Finish()，
            // 确保只有在建造真正完成时才推进阶段，防止任务中断导致墙壁被跳过。

            AWorkerTask.LogProvider(
                $"{this.worker.name} 为自己建造 {decision.BuildTileName}: pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Debug);
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
            this.worker.SetTask(plantTask, WorkerTaskSource.SelfDecision);

            AWorkerTask.LogProvider(
                $"{this.worker.name} 创建自我种植任务: pos=({taskPos.x},{taskPos.y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 创建自我吃饭任务 — 优先交易买食物，失败则自己去采集食物。
        /// </summary>
        private void CreateSelfEatTask()
        {
            AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;

            // 先尝试 Worker 间交易（吃自己的或买别人的）
            WorkerTradeService trade = new WorkerTradeService();
            bool success = trade.TryBuyFood(this.worker);

            if (success)
            {
                AWorkerTask.LogProvider(
                    $"{this.worker.name} 成功解决饥饿 (当前饥饿:{wd?.CurHungry:F0})",
                    LogManager.LogLevelEnum.Debug);
                this.CreateIdleTask();
                return;
            }

            // 交易失败 → 自己去采集食物（扩大扫描范围）
            AWorkerTask.LogProvider(
                $"{this.worker.name} 交易失败, 尝试自己采集食物（扩大扫描范围）",
                LogManager.LogLevelEnum.Debug);

            // 扫描食物：从小范围到大范围逐级扩大
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
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
                            $"{this.worker.name} 找到食物 id={foodResource.Id} pos=({foodPos.Value.x},{foodPos.Value.y})",
                            LogManager.LogLevelEnum.Debug);

                        this.CreateSelfGatherTask(decision);
                        return;
                    }
                }
            }

            AWorkerTask.LogProvider(
                $"{this.worker.name} 扩大扫描范围内无食物可采集，继续空闲",
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
            // 从 FurnitureManager 查找分配给该 Worker 的床位置
            Vector3Int bedPos = this.FindBedPosition();

            if (bedPos != default)
            {
                // 有床：直接分配给自己执行
                WorkerSleepTask sleepTask = new WorkerSleepTask.SleepTaskBuilder()
                    .SetTarget(bedPos)
                    .SetWorker(this.worker)
                    .Build();

                this.worker.SetTask(sleepTask, WorkerTaskSource.SelfDecision);

                AWorkerTask.LogProvider(
                    $"{this.worker.name} 创建睡觉任务(有床) pos=({bedPos.x},{bedPos.y})",
                    LogManager.LogLevelEnum.Debug);
            }
            else
            {
                // 无床：原地地面睡眠
                Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
                WorkerSleepTask sleepTask = new WorkerSleepTask.SleepTaskBuilder()
                    .SetTarget(posMap)
                    .SetWorker(this.worker)
                    .Build();

                this.worker.SetTask(sleepTask, WorkerTaskSource.SelfDecision);

                AWorkerTask.LogProvider(
                    $"{this.worker.name} 创建地面睡觉任务(无床) pos=({posMap.x},{posMap.y})",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <summary>
        /// 查找 Worker 的床位。
        /// 优先从 WorkerData.HomePosition 读取（O(1)），无记录时 fallback 到 FurnitureManager 遍历。
        /// </summary>
        private Vector3Int FindBedPosition()
        {
            // 优先：从持久化的 HomePosition 读取（null = 无家）
            AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;
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
                if (kv.Value == this.worker)
                {
                    return kv.Key;
                }
            }

            return default;
        }

        /// <summary>
        /// 创建自我拾取任务 — 从地面捡起属于自己的物品直接放入背包。
        /// 使用 WorkerPickUpTask 的 FromGround 模式，不走复杂的 Carry 两阶段流程。
        /// </summary>
        private void CreateSelfCarryTask(WorkerBrain.Decision decision)
        {
            if (decision.TargetPosition == default)
            {
                this.CreateIdleTask();
                return;
            }

            int ownerId = this.worker.GetInstanceID();

            WorkerPickUpTask pickUpTask = new WorkerPickUpTask.PickUpTaskBuilder()
                .SetMode(WorkerPickUpTask.PickUpMode.FromGround)
                .SetTargetPosition(decision.TargetPosition)
                .SetGroundResource(decision.Resource)
                .SetOwnerId(ownerId)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池
            this.worker.SetTask(pickUpTask, WorkerTaskSource.SelfDecision);

            AWorkerTask.LogProvider(
                $"{this.worker.name} 创建自我拾取任务: 捡 id={decision.Resource?.Id} pos=({decision.TargetPosition.x},{decision.TargetPosition.y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 创建去任务栏拾取任务 — 去任务栏取回属于自己的悬赏物品。
        /// 直接分配给当前 Worker（不入全局池），确保物品不会被其他人拿走。
        /// </summary>
        private void CreatePickUpTask(WorkerBrain.Decision decision)
        {
            var boardManager = Core.ServiceLocator.Get<Gameplay.TaskBoardManager>();
            if (boardManager == null || !boardManager.IsInitialized)
            {
                AWorkerTask.LogProvider("任务栏未初始化，无法创建拾取任务", LogManager.LogLevelEnum.Warning);
                this.CreateIdleTask();
                return;
            }

            int ownerId = this.worker.GetInstanceID();

            WorkerPickUpTask pickUpTask = new WorkerPickUpTask.PickUpTaskBuilder()
                .SetMode(WorkerPickUpTask.PickUpMode.FromBoard)
                .SetBoardNeighbor(boardManager.GetNeighborPosition())
                .SetOwnerId(ownerId)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池
            this.worker.SetTask(pickUpTask, WorkerTaskSource.SelfDecision);

            AWorkerTask.LogProvider(
                $"{this.worker.name} 创建任务栏拾取任务: pos=({boardManager.BoardPosition.x},{boardManager.BoardPosition.y})",
                LogManager.LogLevelEnum.Debug);
        }

        /// <summary>
        /// 自动出售资源给市场（经济闭环关键步骤）。
        /// 阶梯式按比例出售：持有越多卖越多，保留合理储备。
        /// </summary>
        private void TryAutoSellResources(AWorker.WorkerData workerData)
        {
            // 事业心低的人不急着卖钱
            if (workerData.Personality.Ambition < 40f) return;

            List<ResourceInfo> allResources = this.worker.GetAllResources();
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

            // 触发条件：携带量超过85%或有5种以上可售资源
            // 门槛高于存储阈值(0.8)：让"先入仓、存不下再卖"成为优先策略，存储决策先于出售
            int totalCarried = 0;
            foreach (var r in allResources) totalCarried += r.Count;
            float carryRatio = (float)totalCarried / workerData.MaxResourceCount;
            if (carryRatio <= 0.85f && sellList.Count < 5) return;

            // 执行出售（概率从50%降到35%）
            if (UnityEngine.Random.value < 0.35f)
            {
                Gameplay.MarketService market = Core.ServiceLocator.Get<Gameplay.MarketService>();
                if (market != null)
                {
                    int earned = market.WorkerAutoSellFiltered(this.worker, sellList);
                    if (earned > 0)
                    {
                        AWorkerTask.LogProvider(
                            $"{this.worker.name} 出售{totalSellCount}个资源({sellList.Count}种)获得{earned}G (总携带{totalCarried}/{workerData.MaxResourceCount})",
                            LogManager.LogLevelEnum.Debug);
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
                    return isHungry ? 15 : 10;
                case AItem.ItemTypeEnum.Seed:
                    return 5;
                case AItem.ItemTypeEnum.Material:
                    return 15;
                case AItem.ItemTypeEnum.Equipment:
                case AItem.ItemTypeEnum.Weapon:
                    return 1; // 装备/武器只保留1件
                default:
                    return 5;
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
                    // 食物：5-10卖5%, 10-20卖15%, 20-40卖25%, 40+卖40%
                    if (totalCount <= 5) ratio = 0f;
                    else if (totalCount <= 10) ratio = 0.05f;
                    else if (totalCount <= 20) ratio = 0.15f;
                    else if (totalCount <= 40) ratio = 0.25f;
                    else ratio = 0.40f;
                    // 饥饿时折扣出售意愿
                    if (isHungry) ratio *= 0.3f;
                    break;

                case AItem.ItemTypeEnum.Material:
                    // 材料：0-10不卖, 10-20卖10%, 20-40卖25%, 40+卖40%
                    if (totalCount <= 10) ratio = 0f;
                    else if (totalCount <= 20) ratio = 0.10f;
                    else if (totalCount <= 40) ratio = 0.25f;
                    else ratio = 0.40f;
                    break;

                case AItem.ItemTypeEnum.Seed:
                    // 种子：0-5不卖, 5-10卖15%, 10+卖30%
                    if (totalCount <= 5) ratio = 0f;
                    else if (totalCount <= 10) ratio = 0.15f;
                    else ratio = 0.30f;
                    break;

                case AItem.ItemTypeEnum.Equipment:
                case AItem.ItemTypeEnum.Weapon:
                    // 装备武器：多于1件才卖
                    ratio = totalCount > 1 ? 0.5f : 0f;
                    break;

                default:
                    // 其他：0-5不卖, 5-15卖20%, 15-30卖35%, 30+卖50%
                    if (totalCount <= 5) ratio = 0f;
                    else if (totalCount <= 15) ratio = 0.20f;
                    else if (totalCount <= 30) ratio = 0.35f;
                    else ratio = 0.50f;
                    break;
            }

            return Mathf.Clamp01(ratio);
        }

        /// <summary>
        /// 创建默认空闲任务（锻炼）。
        /// 直接分配给当前 Worker 执行，不走全局任务队列。
        /// 与其他 Self 任务（SelfGather/SelfBuild 等）保持一致：自己创建、自己执行。
        /// 走全局队列会导致锻炼任务堆积：Worker 创建任务后继续漫游，
        /// 分配循环（每5帧）来不及分配，同一 Worker 可能连续创建多个锻炼任务。
        /// </summary>
        private void CreateIdleTask()
        {
            AWorker.WorkerData workerData = this.worker.CharacterDataLAB as AWorker.WorkerData;

            // 余额不足 2G → 免费漫游恢复，而不是付费锻炼
            if (workerData != null && !workerData.Wallet.HasEnough(new CurrencyAmount(2)))
            {
                this.CreateWanderTask();
                return;
            }

            WorkerExerciseTask exerciseTask = new WorkerExerciseTask.ExerciseTaskBuilder()
                .SetTarget(this.currentTargetMap)
                .SetWorker(this.worker)
                .Build();

            // 直接分配给当前 Worker，不入全局任务池，确保自己执行
            this.worker.SetTask(exerciseTask, WorkerTaskSource.SelfDecision);
        }

        /// <summary>
        /// 创建漫游任务 — 多路点持续漫游。
        /// 随机走向远处位置，每到达一个路点恢复精气神和心情。
        /// 与 Idle 的区别：Idle 是原地锻炼（不动），漫游是主动探索走动。
        /// </summary>
        private void CreateWanderTask()
        {
            AWorker.WorkerData wd = this.worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return;

            // 初始化漫游路点计数（3-5个路点，约18-30秒的漫游）
            if (wd.WanderWaypointsRemaining <= 0)
            {
                wd.WanderWaypointsRemaining = UnityEngine.Random.Range(3, 6);
                AWorkerTask.LogProvider(
                    $"{this.worker.name} 开始漫游 ({wd.WanderWaypointsRemaining} 个路点), 精气神={wd.CurSpirit:F0}",
                    LogManager.LogLevelEnum.Debug);
            }

            // 选一个较远的随机可到达位置（漫游半径比普通寻路更大）
            Vector3Int posMap = AWorkerTask.TileMapWorldToMapProvider(this.worker.transform.position);
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

            this.currentTargetMap = wanderTarget;

            // 每到达一个路点恢复少量精气神（到达时在下次 OnEnter 中处理）
            // 这里先不做恢复，等到达后再处理
            AWorkerTask.LogProvider(
                $"{this.worker.name} 漫游 → ({wanderTarget.x},{wanderTarget.y}) 剩余{wd.WanderWaypointsRemaining}路点",
                LogManager.LogLevelEnum.Trace);
        }
    }
}
