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
        /// <summary>疲劳阈值：低于此值倾向发布悬赏</summary>
        public float TiredThresholdForBounty = 50f;

        /// <summary>饥饿阈值：低于此值倾向发布悬赏</summary>
        public float HungryThresholdForBounty = 30f;

        /// <summary>最低钱包保留金额（发布悬赏后必须保留的余额）</summary>
        public CurrencyAmount MinimumWalletReserve = new CurrencyAmount(20);

        /// <summary>悬赏过期时间（游戏内秒数）</summary>
        public float BountyExpirationSeconds = 120f;

        // 基础悬赏金额（按任务类型）— 高于资源市场价，体现"花钱买时间"
        public int BaseRewardBuild = 30;
        public int BaseRewardCarry = 20;
        public int BaseRewardGather = 15;
        public int BaseRewardPlant = 10;

        /// <summary>
        /// 扫描候选 — 轻量数据，避免扫描时创建完整任务实例导致副作用。
        /// </summary>
        private struct WorkCandidate
        {
            public Vector3Int Position;
            public WorkerTaskType TaskType;
            public ResourceInfo Resource;
        }

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
            CurrencyAmount totalNeeded = reward + this.MinimumWalletReserve;

            // 条件 1: 余额充足
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
            bool isTired = workerData.CurTired < AWorker.ThresholdTired;
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
            if (workerData.CurTired < this.TiredThresholdForBounty) probability += 0.3f;

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
        /// 扫描 Worker 周围环境，收集适合发布悬赏的工作候选。
        /// 返回轻量数据列表，不创建完整任务实例，避免触发 GatherMap.AddGather 等副作用。
        /// </summary>
        /// <param name="worker">Worker 实例</param>
        /// <param name="scanRadius">扫描半径（地图格子数）</param>
        private List<WorkCandidate> ScanCandidates(AWorker worker, int scanRadius = 15)
        {
            List<WorkCandidate> candidates = new List<WorkCandidate>();
            Vector3Int workerPos = AWorkerTask.TileMapWorldToMapProvider(worker.transform.position);

            this.ScanResources(candidates, workerPos, scanRadius);
            this.ScanBuildPositions(candidates, workerPos, scanRadius);
            this.ScanPlantPositions(candidates, workerPos, scanRadius);

            // 限制每轮扫描产生的候选数量
            if (candidates.Count > 3)
            {
                candidates = candidates.GetRange(0, 3);
            }

            return candidates;
        }

        /// <summary>
        /// 根据候选数据构建 innerTask（实际工作任务的实例）。
        /// 扫描时不创建任务（避免副作用），只在发布时才构建。
        /// </summary>
        private AWorkerTask BuildInnerTask(WorkCandidate candidate)
        {
            switch (candidate.TaskType)
            {
                case WorkerTaskType.Gather:
                    return new WorkerGatherTask.GatherTaskBuilder()
                        .SetTarget(candidate.Position)
                        .SetResourceInfo(candidate.Resource)
                        .Build();

                case WorkerTaskType.Build:
                {
                    // 获取建造材料需求
                    var itemDataManager = Core.ServiceLocator.Get<ItemDataManager>();
                    string tileName = this.ResolveBuildTileName(candidate.Position);
                    if (string.IsNullOrEmpty(tileName)) return null;

                    BuildItemData buildData = itemDataManager.GetBuildItemDataByName(tileName);
                    if (buildData == null) return null;

                    Dictionary<int, ResourceInfo> needs = this.BuildResourceDict(buildData);
                    if (needs == null || needs.Count == 0) return null;

                    return new WorkerBuildTask.BuildTaskBuilder()
                        .SetBuildPos(candidate.Position)
                        .SetNeedResource(needs)
                        .Build();
                }

                case WorkerTaskType.Plant:
                    return new WorkerPlantTask.PlantTaskBuilder().Build();

                case WorkerTaskType.Carry:
                {
                    if (candidate.Resource == null || candidate.Resource.Count <= 0) return null;
                    return new WorkerCarryTask.CarryTaskBuilder()
                        .SetStartTarget(candidate.Position)
                        .SetResourceInfo(candidate.Resource)
                        .Build();
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// 尝试为 Worker 发布一个悬赏任务。
        /// 综合状态判断 + 环境扫描 + 扣款 + 入队。
        /// 构建 innerTask 包装在 WorkerBountyTask 中——innerTask 处理全部工作逻辑，
        /// WorkerBountyTask 只负责金钱。
        /// </summary>
        /// <param name="worker">发布者 Worker</param>
        /// <returns>成功发布返回 true</returns>
        public bool TryPostOneBounty(AWorker worker)
        {
            // 扫描环境（轻量数据，不触发副作用）
            List<WorkCandidate> candidates = this.ScanCandidates(worker, 15);

            foreach (WorkCandidate candidate in candidates)
            {
                if (!this.ShouldPostBounty(worker, candidate.TaskType))
                {
                    continue;
                }

                // 获取 Worker 人格用于调整悬赏金额
                AWorker.WorkerData issuerData = worker.CharacterDataLAB as AWorker.WorkerData;
                WorkerPersonality personality = issuerData?.Personality ?? WorkerPersonality.Neutral;
                CurrencyAmount reward = this.DetermineReward(candidate.TaskType, personality);
                int issuerId = worker.GetInstanceID();

                // 扣款
                var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
                if (!currencyManager.PostBounty(issuerId, reward))
                {
                    continue;
                }

                // 构建 innerTask（此时才创建，触发 SetTarget 的 GatherMap.AddGather 等副作用）
                AWorkerTask innerTask = this.BuildInnerTask(candidate);
                if (innerTask == null)
                {
                    currencyManager.RefundBounty(issuerId, reward);
                    continue;
                }

                // 获取当前游戏时间
                float currentTime = Core.ServiceLocator.Get<IGameTime>().Time;
                float expiration = currentTime + this.BountyExpirationSeconds;

                // 包装为悬赏任务
                WorkerBountyTask bountyTask = new WorkerBountyTask.BountyTaskBuilder()
                    .SetInnerTask(innerTask)
                    .SetReward(reward)
                    .SetIssuer(issuerId)
                    .SetExpiration(expiration)
                    .Build();

                // 入队（优先级 2，中等优先级）
                AWorkerTask.TaskAddProvider(
                    bountyTask,
                    new GameGridPosition(
                        candidate.Position.x, candidate.Position.y, candidate.Position.z),
                    2);

                AWorkerTask.LogProvider(
                    $"{worker.name} 发布了悬赏: {candidate.TaskType} 悬赏金 {reward}",
                    LogManager.LogLevelEnum.Debug);

                // 每次扫描最多发布 1 个悬赏
                return true;
            }

            return false;
        }

        /// <summary>
        /// 使用预扫描的资源位置发布悬赏（由 WorkerBrain 传入，避免重复扫描失败）。
        /// </summary>
        public bool TryPostOneBounty(AWorker worker, Vector3Int targetPos, ResourceInfo resource)
        {
            if (worker == null || resource == null || targetPos == default) return false;

            WorkerTaskType taskType = WorkerTaskType.Gather; // 目前支持采集类

            if (!this.ShouldPostBounty(worker, taskType)) return false;

            AWorker.WorkerData issuerData = worker.CharacterDataLAB as AWorker.WorkerData;
            WorkerPersonality personality = issuerData?.Personality ?? WorkerPersonality.Neutral;
            CurrencyAmount reward = this.DetermineReward(taskType, personality);
            int issuerId = worker.GetInstanceID();

            var currencyManager = Core.ServiceLocator.Get<Gameplay.CurrencyManager>();
            if (!currencyManager.PostBounty(issuerId, reward)) return false;

            AWorkerTask innerTask = new WorkerGatherTask.GatherTaskBuilder()
                .SetTarget(targetPos)
                .SetResourceInfo(resource)
                .Build();

            if (innerTask == null)
            {
                currencyManager.RefundBounty(issuerId, reward);
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
                2);

            AWorkerTask.LogProvider(
                $"{worker.name} 发布了悬赏: Gather pos=({targetPos.x},{targetPos.y}) 悬赏金 {reward}",
                LogManager.LogLevelEnum.Debug);

            return true;
        }

        /// <summary>
        /// 扫描 ResourceMap 中 Worker 周围的资源位置。
        /// 只收集轻量候选数据，不创建任务实例。
        /// </summary>
        private void ScanResources(List<WorkCandidate> candidates, Vector3Int workerPos, int radius)
        {
            ResourceMap resourceMap = Core.ServiceLocator.Get<ResourceMap>();
            if (resourceMap == null || resourceMap.ResourceMapDataLAB == null)
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
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

                    // 使用 Try 版本静默跳过缺少 ItemData 的资源（如纯装饰性地形资源）
                    if (!Core.ServiceLocator.Get<ItemDataManager>().TryGetByName(resourceName, out ItemData itemData))
                    {
                        continue;
                    }

                    candidates.Add(new WorkCandidate
                    {
                        Position = pos,
                        TaskType = WorkerTaskType.Gather,
                        Resource = new ResourceInfo(itemData.Id),
                    });
                }
            }
        }

        /// <summary>
        /// 扫描 BuildMap 中 Worker 周围的待建造位置。
        /// </summary>
        private void ScanBuildPositions(List<WorkCandidate> candidates, Vector3Int workerPos, int radius)
        {
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null) return;

            foreach (var kv in buildMap.BuildMapDataLAB.PosMap)
            {
                if (kv.Value.IsComplete) continue;
                Vector3Int pos = Vector3IntLAB.ToVector3Int(kv.Key);
                float dist = (pos - workerPos).sqrMagnitude;
                if (dist > radius * radius) continue;

                candidates.Add(new WorkCandidate
                {
                    Position = pos,
                    TaskType = WorkerTaskType.Build,
                    Resource = null,
                });
            }
        }

        /// <summary>
        /// 扫描 FarmlandManager 中 Worker 周围的空闲农田。
        /// </summary>
        private void ScanPlantPositions(List<WorkCandidate> candidates, Vector3Int workerPos, int radius)
        {
            var farmlandManager = Core.ServiceLocator.Get<FarmlandManager>();
            if (farmlandManager == null) return;

            // 检查是否有空闲农田（id == -1 的 cells）
            Vector3Int farmPos = farmlandManager.IsEnoughAndPrePlant(null, null, false);
            if (farmPos == default) return;

            float dist = (farmPos - workerPos).sqrMagnitude;
            if (dist > radius * radius) return;

            candidates.Add(new WorkCandidate
            {
                Position = farmPos,
                TaskType = WorkerTaskType.Plant,
                Resource = null,
            });
        }

        /// <summary>
        /// 根据建造位置查询 BuildMap 获取 Tile 名称。
        /// </summary>
        private string ResolveBuildTileName(Vector3Int pos)
        {
            var buildMap = Core.ServiceLocator.Get<BuildMap>();
            if (buildMap?.BuildMapDataLAB?.PosMap == null) return null;

            Vector3IntLAB posLAB = Vector3IntLAB.ToVector3IntLAB(pos);
            if (buildMap.BuildMapDataLAB.PosMap.TryGetValue(posLAB, out BuildMap.BuildTileData tileData))
            {
                return tileData.Name;
            }

            return null;
        }

        /// <summary>
        /// 从 BuildItemData.BuildCosts 构建资源需求字典。
        /// </summary>
        private Dictionary<int, ResourceInfo> BuildResourceDict(BuildItemData buildData)
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

            return dict;
        }
    }
}
