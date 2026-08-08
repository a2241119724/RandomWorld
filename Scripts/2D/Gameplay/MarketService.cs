namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// 市场交易服务 — Worker 可出售资源换取金币，Player 可购买资源。
    /// 纯 C# 服务，通过 Singleton 注册到 ServiceLocator。
    ///
    /// 经济闭环的关键节点：
    /// Worker 采集资源 → 卖给市场 → 获得金币 → 发布悬赏 → 其他 Worker 接悬赏赚钱
    /// Player 买资源 → 金币流入 Worker 经济 → Player 获得建造材料
    /// </summary>
    public class MarketService : Singleton<MarketService>
    {
        /// <summary>
        /// 物品市场价格配置。
        /// </summary>
        [System.Serializable]
        public class PriceConfig
        {
            /// <summary>物品 ID</summary>
            public int ItemId;

            /// <summary>市场买入价（Player 买价 = Worker 卖价 × 利润率）</summary>
            public int BaseSellPrice;

            /// <summary>市场卖出价（Player 买价）</summary>
            public int BaseBuyPrice;

            /// <summary>物品中文名（调试用）</summary>
            public string CnName;

            public PriceConfig(int itemId, int sellPrice, int buyPrice, string cnName = "")
            {
                this.ItemId = itemId;
                this.BaseSellPrice = sellPrice;
                this.BaseBuyPrice = buyPrice;
                this.CnName = cnName;
            }
        }

        /// <summary>价格表（ItemId → PriceConfig）</summary>
        private readonly Dictionary<int, PriceConfig> priceTable;

        /// <summary>市场利润率：BuyPrice = SellPrice × (1 + profitMargin)</summary>
        public float ProfitMargin = 0.3f;

        /// <summary>Worker 出售资源时是否自动清除所有权</summary>
        public bool AutoClearOwnership = true;

        /// <summary>市场总交易次数（统计用）</summary>
        public int TotalTransactions { get; private set; }

        /// <summary>市场总流通金币（统计用）</summary>
        public int TotalGoldCirculated { get; private set; }

        public MarketService()
        {
            this.priceTable = new Dictionary<int, PriceConfig>();
            this.InitDefaultPrices();
        }

        /// <summary>
        /// 初始化默认价格表。基于常见资源类型设置默认价格。
        /// </summary>
        private void InitDefaultPrices()
        {
            // 木材类
            this.RegisterPrice(/* wood */ 0, 3, 5, "木材");
            this.RegisterPrice(/* hardwood */ 0, 5, 8, "硬木");

            // 石材类
            this.RegisterPrice(/* stone */ 0, 3, 5, "石材");

            // 食物类
            this.RegisterPrice(/* apple */ 0, 4, 6, "苹果");
            this.RegisterPrice(/* meat */ 0, 8, 12, "肉");
        }

        /// <summary>
        /// 注册一个物品的市场价格。
        /// </summary>
        public void RegisterPrice(int itemId, int sellPrice, int buyPrice, string cnName = "")
        {
            this.priceTable[itemId] = new PriceConfig(itemId, sellPrice, buyPrice, cnName);
        }

        /// <summary>
        /// 获取物品的卖出价（Worker 卖价）。
        /// </summary>
        public int GetSellPrice(int itemId, int count = 1)
        {
            if (this.priceTable.TryGetValue(itemId, out PriceConfig config))
            {
                return config.BaseSellPrice * count;
            }

            // 未配置价格，返回默认低保价
            return count;
        }

        /// <summary>
        /// 获取物品的买入价（Player 买价）。
        /// </summary>
        public int GetBuyPrice(int itemId, int count = 1)
        {
            if (this.priceTable.TryGetValue(itemId, out PriceConfig config))
            {
                return config.BaseBuyPrice * count;
            }

            // 未配置价格，返回默认价（比卖价高）
            return count * 2;
        }

        /// <summary>
        /// Worker 出售携带的资源给市场。
        /// 资源从 Worker 背包中扣除，金币添加到 Worker 钱包。
        /// </summary>
        /// <param name="worker">出售者 Worker</param>
        /// <param name="resourceInfo">要出售的资源（会被修改：Count 清零表示全部卖出）</param>
        /// <returns>获得的金币数量，出售失败返回 0</returns>
        public int WorkerSellResource(AWorker worker, ResourceInfo resourceInfo)
        {
            if (worker == null || resourceInfo == null || resourceInfo.Count <= 0)
            {
                return 0;
            }

            AWorker.WorkerData workerData = worker.CharacterDataLAB as AWorker.WorkerData;
            if (workerData == null)
            {
                return 0;
            }

            // 检查 Worker 是否真的携带这些资源
            int carried = worker.GetResourceCountById(resourceInfo.Id);
            if (carried < resourceInfo.Count)
            {
                AWorkerTask.LogProvider(
                    $"[Market] {worker.name} 试图出售 {resourceInfo.Count} 个资源(id={resourceInfo.Id})，但只携带 {carried} 个",
                    LogManager.LogLevelEnum.Warning);
                return 0;
            }

            int sellPrice = this.GetSellPrice(resourceInfo.Id, resourceInfo.Count);
            if (sellPrice <= 0)
            {
                return 0;
            }

            // 扣除资源
            worker.SubResource(resourceInfo);

            // 添加金币
            CurrencyAmount earned = new CurrencyAmount(sellPrice);
            workerData.Wallet += earned;

            // 清除所有权（卖给了市场）
            if (this.AutoClearOwnership)
            {
                resourceInfo.OwnerId = ItemOwnershipService.UnownedId;
            }

            // 更新人格：赚钱提升心情和事业心
            workerData.Personality = workerData.Personality.AfterEarnGold(sellPrice);

            // 统计
            this.TotalTransactions++;
            this.TotalGoldCirculated += sellPrice;

            // 发布交易事件
            this.PublishTransaction(worker.GetInstanceID(), 0, earned, "WorkerSell");

            AWorkerTask.LogProvider(
                $"[Market] {worker.name} 出售 {resourceInfo.Count}×id={resourceInfo.Id} 获得 {earned}",
                LogManager.LogLevelEnum.Debug);

            return sellPrice;
        }

        /// <summary>
        /// Worker 自动出售所有携带的资源给市场（当背包满或想要套现时调用）。
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <returns>总获得金币</returns>
        public int WorkerAutoSellAll(AWorker worker)
        {
            if (worker == null)
            {
                return 0;
            }

            int totalEarned = 0;

            List<ResourceInfo> resources = worker.GetAllResources();
            if (resources == null || resources.Count == 0)
            {
                return 0;
            }

            foreach (var resource in resources)
            {
                if (resource.Count > 0)
                {
                    int earned = this.WorkerSellResource(worker,
                        new ResourceInfo(resource.Id, resource.Count, resource.OwnerId));
                    totalEarned += earned;
                }
            }

            return totalEarned;
        }

        /// <summary>
        /// Worker 智能出售指定的资源列表（已过滤掉食物/建材/种子等）。
        /// </summary>
        /// <param name="worker">Worker</param>
        /// <param name="toSell">要出售的资源列表</param>
        /// <returns>总获得金币</returns>
        public int WorkerAutoSellFiltered(AWorker worker, List<ResourceInfo> toSell)
        {
            if (worker == null || toSell == null || toSell.Count == 0)
            {
                return 0;
            }

            int totalEarned = 0;

            foreach (var resource in toSell)
            {
                if (resource.Count > 0)
                {
                    int earned = this.WorkerSellResource(worker,
                        new ResourceInfo(resource.Id, resource.Count, resource.OwnerId));
                    totalEarned += earned;
                }
            }

            return totalEarned;
        }


        /// <summary>
        /// 获取市场价格信息（供 UI 展示）。
        /// </summary>
        public string GetMarketInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 市场价格 ===");
            foreach (var kv in this.priceTable)
            {
                sb.AppendLine($"{kv.Value.CnName}(id={kv.Key}): 卖{kv.Value.BaseSellPrice}G 买{kv.Value.BaseBuyPrice}G");
            }
            sb.AppendLine($"总交易: {this.TotalTransactions} 次");
            sb.AppendLine($"流通金币: {this.TotalGoldCirculated}G");
            return sb.ToString();
        }

        private void PublishTransaction(int fromWorkerId, int toWorkerId, CurrencyAmount amount, string reason)
        {
            try
            {
                Core.ServiceLocator.Get<EventBus>().Publish(new CurrencyTransactionEvent
                {
                    FromWorkerId = fromWorkerId,
                    ToWorkerId = toWorkerId,
                    Amount = amount,
                    Reason = reason,
                });
            }
            catch
            {
                // EventBus 可能未初始化，静默忽略
            }
        }
    }
}
