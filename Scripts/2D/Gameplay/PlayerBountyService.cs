namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Item.Build;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Map;
    using UnityEngine;

    /// <summary>
    /// Player 悬赏服务 — Player 可以发布悬赏任务，Worker 领取并完成后获得金币。
    /// 与 WorkerBountyDecisionService 类似，但由 Player 手动触发。
    /// Player 的 OwnerId = 0。
    /// </summary>
    public class PlayerBountyService
    {
        /// <summary>Player 发布悬赏时使用的 OwnerId</summary>
        public const int PlayerOwnerId = 0;

        /// <summary>悬赏过期时间（游戏内秒数）</summary>
        public float BountyExpirationSeconds = 300f;

        // 基础悬赏金额 — Player 出价更高（Player 更有钱）
        public int BaseRewardBuild = 40;
        public int BaseRewardCarry = 25;
        public int BaseRewardGather = 20;
        public int BaseRewardPlant = 12;

        /// <summary>
        /// Player 发布一个采集悬赏（在指定位置采集资源）。
        /// </summary>
        /// <param name="player">Player 实例</param>
        /// <param name="targetPos">目标地图位置</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="reward">悬赏金额（0=自动计算）</param>
        /// <returns>成功发布返回 true</returns>
        public bool PostGatherBounty(Player player, Vector3Int targetPos, int resourceId, int reward = 0)
        {
            if (player == null) return false;

            Player.PlayerData pd = player.CharacterDataLAB as Player.PlayerData;
            if (pd == null) return false;

            if (reward <= 0) reward = this.BaseRewardGather;
            CurrencyAmount cost = new CurrencyAmount(reward);

            if (!pd.Wallet.HasEnough(cost))
            {
                AWorkerTask.LogProvider(
                    $"[PlayerBounty] 余额不足: 需要{cost}, 余额{pd.Wallet}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            // 扣款
            pd.Wallet -= cost;

            // 托管追踪
            var cm = Core.ServiceLocator.Get<CurrencyManager>();
            cm.PostBounty(PlayerOwnerId, cost);

            // 构建 innerTask
            ResourceInfo resource = new ResourceInfo(resourceId);
            WorkerGatherTask innerTask = new WorkerGatherTask.GatherTaskBuilder()
                .SetTarget(targetPos)
                .SetResourceInfo(resource)
                .Build();

            // 包装为悬赏（issuer=0 表示 Player）
            float currentTime = Core.ServiceLocator.Get<IGameTime>().Time;
            WorkerBountyTask bounty = new WorkerBountyTask.BountyTaskBuilder()
                .SetInnerTask(innerTask)
                .SetReward(cost)
                .SetIssuer(PlayerOwnerId)
                .SetExpiration(currentTime + this.BountyExpirationSeconds)
                .Build();

            AWorkerTask.TaskAddProvider(
                bounty,
                new GameGridPosition(targetPos.x, targetPos.y, targetPos.z),
                2);

            AWorkerTask.LogProvider(
                $"[PlayerBounty] 发布采集悬赏 pos=({targetPos.x},{targetPos.y}) 悬赏金{cost}",
                LogManager.LogLevelEnum.Info);

            return true;
        }

        /// <summary>
        /// Player 发布建造悬赏。
        /// </summary>
        public bool PostBuildBounty(Player player, Vector3Int targetPos, ABuildItem buildItem, int reward = 0)
        {
            if (player == null || buildItem == null) return false;

            Player.PlayerData pd = player.CharacterDataLAB as Player.PlayerData;
            if (pd == null) return false;

            if (reward <= 0) reward = this.BaseRewardBuild;
            CurrencyAmount cost = new CurrencyAmount(reward);

            if (!pd.Wallet.HasEnough(cost))
            {
                AWorkerTask.LogProvider(
                    $"[PlayerBounty] 余额不足: 需要{cost}, 余额{pd.Wallet}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            pd.Wallet -= cost;

            // 托管追踪
            var cm = Core.ServiceLocator.Get<CurrencyManager>();
            cm.PostBounty(PlayerOwnerId, cost);

            WorkerBuildTask innerTask = new WorkerBuildTask.BuildTaskBuilder()
                .SetBuild(buildItem)
                .SetBuildPos(targetPos)
                .Build();

            float currentTime = Core.ServiceLocator.Get<IGameTime>().Time;
            WorkerBountyTask bounty = new WorkerBountyTask.BountyTaskBuilder()
                .SetInnerTask(innerTask)
                .SetReward(cost)
                .SetIssuer(PlayerOwnerId)
                .SetExpiration(currentTime + this.BountyExpirationSeconds)
                .Build();

            AWorkerTask.TaskAddProvider(
                bounty,
                new GameGridPosition(targetPos.x, targetPos.y, targetPos.z),
                2);

            AWorkerTask.LogProvider(
                $"[PlayerBounty] 发布建造悬赏 pos=({targetPos.x},{targetPos.y}) 悬赏金{cost}",
                LogManager.LogLevelEnum.Info);

            return true;
        }

        /// <summary>
        /// 根据任务类型获取推荐悬赏金额。
        /// </summary>
        public int GetRecommendedReward(WorkerTaskType taskType)
        {
            return taskType switch
            {
                WorkerTaskType.Build => this.BaseRewardBuild,
                WorkerTaskType.Carry => this.BaseRewardCarry,
                WorkerTaskType.Gather => this.BaseRewardGather,
                WorkerTaskType.Plant => this.BaseRewardPlant,
                _ => 10,
            };
        }

        /// <summary>
        /// 获取 Player 钱包余额。
        /// </summary>
        public CurrencyAmount GetPlayerBalance(Player player)
        {
            return (player?.CharacterDataLAB as Player.PlayerData)?.Wallet ?? CurrencyAmount.Zero;
        }

        /// <summary>
        /// Player 完成悬赏时调用 — 从托管支付给 Worker。
        /// WorkerBountyTask.Execute 内部会调用 CurrencyManager.CompleteBounty，
        /// 这里提供 Player 侧的便捷入口。
        /// </summary>
        public void CompleteBountyForPlayer(Player player, AWorker executor, CurrencyAmount reward)
        {
            var cm = Core.ServiceLocator.Get<CurrencyManager>();
            cm.CompleteBounty(PlayerOwnerId, executor, reward);

            AWorkerTask.LogProvider(
                $"[PlayerBounty] {executor.name} 完成 Player 悬赏，获得 {reward}",
                LogManager.LogLevelEnum.Debug);
        }
    }
}
