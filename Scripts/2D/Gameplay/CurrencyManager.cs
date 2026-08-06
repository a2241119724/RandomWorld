namespace LAB2D.Gameplay
{
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using System.Collections.Generic;

    /// <summary>
    /// 货币管理器 — 处理所有货币操作（初始化、托管转账、退款）。
    /// 继承 Singleton&lt;CurrencyManager&gt;，在 GlobalInit.RegisterSafeServices() 中注册。
    ///
    /// 托管（escrow）机制：发布悬赏时资金从发布者转入托管池，
    /// 完成时从托管池交付给执行者，过期时退回发布者。
    /// 交易通过 EventBus 发布 CurrencyTransactionEvent 供 UI 和日志订阅。
    /// </summary>
    public class CurrencyManager : Singleton<CurrencyManager>
    {
        /// <summary>Player 钱包 ownerId</summary>
        public const int PlayerOwnerId = 0;

        /// <summary>
        /// 新 Worker 初始资金。
        /// </summary>
        public CurrencyAmount InitialWorkerFunds { get; set; } = new CurrencyAmount(100);

        /// <summary>
        /// Player 初始资金。
        /// </summary>
        public CurrencyAmount InitialPlayerFunds { get; set; } = new CurrencyAmount(200);

        /// <summary>
        /// 托管资金 — Worker instance ID → 托管总额。
        /// </summary>
        private readonly Dictionary<int, CurrencyAmount> escrow;

        /// <summary>
        /// 钱包余额 — ownerId → 当前余额。ownerId=0 为 Player，>0 为 Worker instance ID。
        /// </summary>
        private readonly Dictionary<int, CurrencyAmount> wallets;

        public CurrencyManager()
        {
            this.escrow = new Dictionary<int, CurrencyAmount>();
            this.wallets = new Dictionary<int, CurrencyAmount>();
        }

        /// <summary>
        /// 初始化 Worker 钱包。
        /// 在 WorkerManager.Add() 中对新生成的 Worker 调用。
        /// </summary>
        /// <param name="worker">Worker 实例</param>
        public void InitializeWorkerWallet(AWorker worker)
        {
            if (worker == null)
            {
                return;
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return;
            }

            workerData.Wallet = this.InitialWorkerFunds;
            this.PublishTransaction(0, worker.GetInstanceID(), this.InitialWorkerFunds, "InitialFunds");
        }

        /// <summary>
        /// 发布悬赏：从发布者钱包扣除悬赏金并进入托管。
        /// 支持 Worker (issuerWorkerId>0) 和 Player (issuerWorkerId=0)。
        /// </summary>
        public bool PostBounty(int issuerWorkerId, CurrencyAmount reward)
        {
            // Player (id=0): 直接操作托管，扣款由 PlayerBountyService 处理
            if (issuerWorkerId == 0)
            {
                this.escrow[0] = this.escrow.TryGetValue(0, out CurrencyAmount escrowAmount)
                    ? escrowAmount + reward
                    : reward;
                this.PublishTransaction(0, 0, reward, "BountyPost(Player)");
                return true;
            }

            AWorker worker = this.FindWorker(issuerWorkerId);
            if (worker == null) return false;

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null) return false;

            if (!workerData.Wallet.HasEnough(reward)) return false;

            workerData.Wallet -= reward;
            this.escrow[issuerWorkerId] = this.escrow.TryGetValue(issuerWorkerId, out CurrencyAmount existing)
                ? existing + reward
                : reward;

            this.PublishTransaction(issuerWorkerId, 0, reward, "BountyPost");
            return true;
        }

        /// <summary>
        /// 完成悬赏：从托管交付给执行者。issuerWorkerId=0 表示 Player 发布的悬赏。
        /// </summary>
        public void CompleteBounty(int issuerWorkerId, AWorker executor, CurrencyAmount reward)
        {
            if (executor == null) return;

            AWorker.WorkerData executorData = executor.CharacterDataLAB as AWorker.WorkerData;
            if (executorData == null) return;

            // 从托管中扣除
            if (this.escrow.TryGetValue(issuerWorkerId, out CurrencyAmount held))
            {
                CurrencyAmount newHeld = held - reward;
                if (newHeld.Gold <= 0) this.escrow.Remove(issuerWorkerId);
                else this.escrow[issuerWorkerId] = newHeld;
            }

            executorData.Wallet += reward;
            this.PublishTransaction(issuerWorkerId, executor.GetInstanceID(), reward, "BountyReward");
        }

        /// <summary>
        /// 退款：过期或取消时退回托管余额给发布者。
        /// </summary>
        /// <param name="issuerWorkerId">发布者 Worker instance ID</param>
        /// <param name="reward">退款金额</param>
        public void RefundBounty(int issuerWorkerId, CurrencyAmount reward)
        {
            AWorker worker = this.FindWorker(issuerWorkerId);
            if (worker == null)
            {
                return;
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return;
            }

            workerData.Wallet += reward;
            this.escrow.Remove(issuerWorkerId);
            this.PublishTransaction(0, issuerWorkerId, reward, "Refund");
        }

        /// <summary>
        /// 获取 Worker 余额。
        /// </summary>
        /// <param name="worker">Worker 实例</param>
        /// <returns>钱包余额，Worker 无效时返回 Zero</returns>
        public CurrencyAmount GetBalance(AWorker worker)
        {
            return (worker?.CharacterDataLAB as AWorker.WorkerData)?.Wallet
                ?? CurrencyAmount.Zero;
        }

        /// <summary>
        /// 获取指定 Worker 的托管总额。
        /// </summary>
        /// <param name="workerId">Worker instance ID</param>
        /// <returns>托管总额</returns>
        public CurrencyAmount GetEscrow(int workerId)
        {
            return this.escrow.TryGetValue(workerId, out CurrencyAmount amount)
                ? amount
                : CurrencyAmount.Zero;
        }

        private void PublishTransaction(int from, int to, CurrencyAmount amount, string reason)
        {
            Core.ServiceLocator.Get<EventBus>().Publish(new CurrencyTransactionEvent
            {
                FromWorkerId = from,
                ToWorkerId = to,
                Amount = amount,
                Reason = reason,
            });
        }

        /// <summary>
        /// 获取 Player 当前余额。ownerId=0。
        /// </summary>
        public CurrencyAmount GetPlayerBalance()
        {
            return this.wallets.TryGetValue(PlayerOwnerId, out CurrencyAmount amount)
                ? amount
                : CurrencyAmount.Zero;
        }

        /// <summary>
        /// 初始化 Player 钱包（首次使用时调用）。
        /// </summary>
        public void EnsurePlayerWallet()
        {
            if (!this.wallets.ContainsKey(PlayerOwnerId))
            {
                this.wallets[PlayerOwnerId] = this.InitialPlayerFunds;
                this.PublishTransaction(0, PlayerOwnerId, this.InitialPlayerFunds, "PlayerInitialFunds");
            }
        }

        /// <summary>
        /// Player 消费金币。余额不足时返回 false。
        /// </summary>
        /// <param name="amount">消费金额</param>
        /// <returns>扣款成功返回 true</returns>
        public bool TrySpendPlayerGold(int amount)
        {
            if (amount <= 0) return true;
            this.EnsurePlayerWallet();
            CurrencyAmount current = this.wallets[PlayerOwnerId];
            if (current.Gold < amount) return false;
            this.wallets[PlayerOwnerId] = new CurrencyAmount(current.Gold - amount);
            this.PublishTransaction(PlayerOwnerId, 0, new CurrencyAmount(amount), "PlayerSpend");
            return true;
        }

        /// <summary>
        /// Player 获得金币。
        /// </summary>
        /// <param name="amount">金额</param>
        public void AddPlayerGold(int amount)
        {
            if (amount <= 0) return;
            this.EnsurePlayerWallet();
            CurrencyAmount current = this.wallets[PlayerOwnerId];
            this.wallets[PlayerOwnerId] = new CurrencyAmount(current.Gold + amount);
            this.PublishTransaction(0, PlayerOwnerId, new CurrencyAmount(amount), "PlayerEarn");
        }

        /// <summary>
        /// 获取指定 ownerId 的钱包余额（0=Player，>0=Worker）。
        /// Worker 余额优先从 WorkerData.Wallet 读取，Player 从 wallets 字典读取。
        /// </summary>
        public CurrencyAmount GetWalletBalance(int ownerId)
        {
            if (ownerId == PlayerOwnerId)
            {
                return this.GetPlayerBalance();
            }

            AWorker worker = this.FindWorker(ownerId);
            return this.GetBalance(worker);
        }

        /// <summary>
        /// 通过 instance ID 查找 Worker。
        /// </summary>
        /// <param name="instanceId">Worker instance ID</param>
        /// <returns>找到的 Worker，未找到返回 null</returns>
        public AWorker FindWorker(int instanceId)
        {
            System.Collections.Generic.List<AWorker> workers =
                Core.ServiceLocator.Get<WorkerManager>().Characters;
            foreach (AWorker w in workers)
            {
                if (w.GetInstanceID() == instanceId)
                {
                    return w;
                }
            }

            return null;
        }
    }
}
