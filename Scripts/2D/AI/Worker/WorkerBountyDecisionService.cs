namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Item;
    using LAB2D.Map;
    using LAB2D.Serializable;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 悬赏决策服务 — 判断 Worker 是否应该发布悬赏任务而非自己执行。
    /// 纯 C# 服务，不依赖 MonoBehaviour，可在测试中独立实例化。
    /// 整合 WorkerPersonality 影响决策权重和悬赏金额。
    /// </summary>
    public class WorkerBountyDecisionService
    {
        /// <summary>疲劳阈值：疲劳值高于 MaxTired-此值 倾向发布悬赏</summary>
        public float TiredThresholdForBounty = 50f;

        /// <summary>饥饿阈值：低于此值倾向发布悬赏</summary>
        public float HungryThresholdForBounty = 30f;

        /// <summary>最低钱包保留金额（发布悬赏后必须保留的余额）</summary>
        public CurrencyAmount MinimumWalletReserve = new CurrencyAmount(20);

        /// <summary>悬赏过期时间（游戏内秒数）</summary>
        public float BountyExpirationSeconds = 240f;

        /// <summary>悬赏交易手续费率（15%），作为货币销毁机制不进入托管不退还</summary>
        public const float BountyTransactionFeeRate = 0.15f;

        // 基础悬赏金额（按任务类型）— 高于资源市场价，体现"花钱买时间"
        public int BaseRewardBuild = 22;
        public int BaseRewardCarry = 15;
        public int BaseRewardGather = 10;
        public int BaseRewardPlant = 8;


        /// <summary>
        /// 判断 Worker 是否应该为此类任务发布悬赏（而非自己做）。
        /// </summary>
        /// <param name="worker">Worker 实例</param>
        /// <param name="taskType">候选任务类型</param>
        /// <returns>应该发布悬赏时返回 true</returns>
        public bool ShouldPostBounty(AWorker worker, WorkerTaskType taskType)
        {
            if (worker == null)
            {
                return false;
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return false;
            }

            CurrencyAmount reward = this.DetermineReward(taskType);
            int fee = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(reward.Gold * BountyTransactionFeeRate));
            CurrencyAmount totalNeeded = reward + this.MinimumWalletReserve + new CurrencyAmount(fee);

            // 条件 1: 余额充足（含手续费+保留金）
            if (!workerData.Wallet.HasEnough(totalNeeded))
            {
                return false;
            }

            // 条件 1.5: Bootstrap 阶段不发悬赏
            if (workerData.LifeStage < Domain.Worker.WorkerLifeStage.Settled)
            {
                return false;
            }

            // 条件 2: Worker 状态不佳（太累或太饿）= 不想自己做
            bool isTired = workerData.CurTired > workerData.MaxTired - AWorker.ThresholdTired;
            bool isHungry = workerData.CurHungry < AWorker.ThresholdHungry;

            if (isTired || isHungry)
            {
                return true;
            }

            // 条件 3: 已有任务在身 = 没空自己做
            if (workerData.Task != null)
            {
                return true;
            }

            // 条件 4: 人格加权的随机概率
            float probability = 0.5f; // 基准提高到50%
            if (workerData.CurTired > workerData.MaxTired - this.TiredThresholdForBounty) probability += 0.3f;

            WorkerPersonality p = workerData.Personality;
            probability += (p.Sociality - 50f) * 0.005f;
            probability += (p.Ambition - 50f) * 0.003f;
            probability -= (p.Diligence - 50f) * 0.002f;

            bool pass = Random.value < probability;
            if (!pass)
            {
                AWorkerTask.LogProvider(
                    $"{worker.name} 悬赏概率检查失败: prob={probability:F2} sociality={p.Sociality:F0} ambition={p.Ambition:F0}",
                    LogManager.LogLevelEnum.Debug);
            }
            return pass;
        }

        /// <summary>
        /// 根据任务类型计算悬赏金额（可被人格影响）。
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="personality">可选的人格数据，用于调整悬赏金额</param>
        public CurrencyAmount DetermineReward(WorkerTaskType taskType, WorkerPersonality personality = default)
        {
            int baseReward = taskType switch
            {
                WorkerTaskType.Build => this.BaseRewardBuild,
                WorkerTaskType.Carry => this.BaseRewardCarry,
                WorkerTaskType.Gather => this.BaseRewardGather,
                WorkerTaskType.Plant => this.BaseRewardPlant,
                _ => 10,
            };

            // 社交高的 Worker 出价更大方（+0~30%）
            if (personality.Sociality > 50f)
            {
                float bonus = (personality.Sociality - 50f) * 0.006f; // 最多 +30%
                baseReward = (int)(baseReward * (1f + bonus));
            }

            // 事业心高的也愿意多出钱（追求效率）
            if (personality.Ambition > 60f)
            {
                float bonus = (personality.Ambition - 60f) * 0.005f; // 最多 +20%
                baseReward = (int)(baseReward * (1f + bonus));
            }

            return new CurrencyAmount(Mathf.Max(baseReward, 1));
        }

        /// <summary>
        /// 使用预扫描的资源位置发布悬赏（由 WorkerBrain 传入，避免重复扫描失败）。
        /// 支持资源采集和地形挖掘两种类型。
        /// 发布时额外扣除 15% 手续费（不进入托管，不退还），作为货币销毁机制。
        /// </summary>
        public bool TryPostOneBounty(AWorker worker, Vector3Int targetPos, ResourceInfo resource,
            bool isTerrainDig = false, int terrainId = 0)
        {
            if (worker == null || targetPos == default) return false;

            WorkerTaskType taskType = WorkerTaskType.Gather; // 目前支持采集类

            if (!this.ShouldPostBounty(worker, taskType)) return false;

            AWorker.WorkerData issuerData = worker.CharacterDataLAB as AWorker.WorkerData;
            WorkerPersonality personality = issuerData?.Personality ?? WorkerPersonality.Neutral;
            CurrencyAmount reward = this.DetermineReward(taskType, personality);
            int issuerId = worker.GetInstanceID();

            // 计算手续费（至少 1G）
            int fee = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(reward.Gold * BountyTransactionFeeRate));
            CurrencyAmount totalCost = new CurrencyAmount(reward.Gold + fee);

            // 检查余额是否够 reward + fee
            if (!issuerData.Wallet.HasEnough(totalCost))
            {
                AWorkerTask.LogProvider(
                    $"{worker.name} 余额不足以支付悬赏+手续费: 需要{totalCost}, 余额{issuerData.Wallet}",
                    LogManager.LogLevelEnum.Debug);
                return false;
            }

            // 扣款：手续费直接销毁，悬赏金进托管
            issuerData.Wallet -= totalCost;
            var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
            if (!currencyManager.PostBounty(issuerId, reward))
            {
                issuerData.Wallet += totalCost;
                return false;
            }

            AWorkerTask innerTask;
            if (isTerrainDig)
            {
                innerTask = new WorkerGatherTask.GatherTaskBuilder()
                    .SetTerrainTarget(targetPos, terrainId)
                    .Build();
            }
            else
            {
                if (resource == null) { currencyManager.RefundBounty(issuerId, reward); issuerData.Wallet += reward; return false; }
                innerTask = new WorkerGatherTask.GatherTaskBuilder()
                    .SetTarget(targetPos)
                    .SetResourceInfo(resource)
                    .Build();
            }

            if (innerTask == null)
            {
                currencyManager.RefundBounty(issuerId, reward);
                issuerData.Wallet += reward; // 只退悬赏金，手续费不退
                return false;
            }

            float currentTime = Core.ServiceLocator.Get<IGameTime>().Time;
            WorkerBountyTask bountyTask = new WorkerBountyTask.BountyTaskBuilder()
                .SetInnerTask(innerTask)
                .SetReward(reward)
                .SetIssuer(issuerId)
                .SetExpiration(currentTime + this.BountyExpirationSeconds)
                .Build();

            AWorkerTask.TaskAddProvider(
                bountyTask,
                new GameGridPosition(targetPos.x, targetPos.y, targetPos.z),
                WorkerTaskPriority.WorkerBounty);

            string actionName = isTerrainDig ? "挖掘" : "Gather";
            AWorkerTask.LogProvider(
                $"{worker.name} 发布了悬赏: {actionName} pos=({targetPos.x},{targetPos.y}) 悬赏金 {reward} 手续费{fee}G",
                LogManager.LogLevelEnum.Debug);

            return true;
        }

    }
}
