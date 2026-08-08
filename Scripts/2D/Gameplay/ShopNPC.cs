namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 商店 NPC — 可放置在场景中，提供买卖服务。
    /// Worker 和 Player 都可以在此买卖物品。
    /// 挂载到任意 GameObject 上即可工作。
    /// </summary>
    public class ShopNPC : MonoBehaviour
    {
        [Header("商店配置")]
        [Tooltip("商店名称")]
        public string ShopName = "杂货铺";

        [Tooltip("商品列表（在 Inspector 中配置）")]
        public List<ShopItem> ItemsForSale = new List<ShopItem>();

        [Tooltip("收购折扣率 — 商店买入价 = 卖出价 × buybackRate")]
        [Range(0.1f, 1f)]
        public float BuybackRate = 0.6f;

        [Tooltip("交互半径")]
        public float InteractionRadius = 3f;

        [Tooltip("出售给市场的物品是否清除所有权")]
        public bool ClearOwnershipOnSell = true;

        /// <summary>
        /// 商店单品
        /// </summary>
        [System.Serializable]
        public class ShopItem
        {
            [Tooltip("物品ID")]
            public int ItemId;

            [Tooltip("售价（金币）")]
            public int Price;

            [Tooltip("库存数量（-1=无限）")]
            public int Stock = -1;

            [Tooltip("物品中文名（调试用）")]
            public string CnName;
        }

        /// <summary>
        /// Player 交互打开商店的回调。由 ShopPanel 在初始化时注册。
        /// </summary>
        public static System.Action<ShopNPC> OnShopInteract;

        [Tooltip("交互按键")]
        public KeyCode InteractKey = KeyCode.Alpha7;

        private MarketService market;
        private bool isShopOpen;

        private void Awake()
        {
            this.market = Core.ServiceLocator.Get<MarketService>();
        }

        /// <summary>
        /// Worker 向商店出售资源。
        /// </summary>
        /// <param name="worker">出售者</param>
        /// <param name="resource">要出售的资源</param>
        /// <returns>获得的金币</returns>
        public int WorkerSellToShop(AWorker worker, ResourceInfo resource)
        {
            if (worker == null || resource == null || resource.Count <= 0) return 0;

            // 查找商店是否收这个物品
            ShopItem shopItem = this.FindShopItem(resource.Id);
            int price;
            if (shopItem != null)
            {
                price = Mathf.FloorToInt(shopItem.Price * this.BuybackRate);
            }
            else
            {
                // 未配置的物品用市场底价
                price = this.market?.GetSellPrice(resource.Id, resource.Count) ?? resource.Count;
            }

            if (price <= 0) return 0;

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return 0;

            // 检查 Worker 是否真的有这些资源
            if (worker.GetResourceCountById(resource.Id) < resource.Count) return 0;

            // 扣资源
            worker.SubResource(resource);

            // 加钱
            CurrencyAmount earned = new CurrencyAmount(price);
            wd.Wallet += earned;
            wd.Personality = wd.Personality.AfterEarnGold(price);

            // 清除所有权
            if (this.ClearOwnershipOnSell)
                resource.OwnerId = ItemOwnershipService.UnownedId;

            // 商店库存增加（如果有限库存的话）
            if (shopItem != null && shopItem.Stock >= 0)
                shopItem.Stock += resource.Count;

            AWorkerTask.LogProvider(
                $"[{this.ShopName}] {worker.name} 出售 {resource.Count}×id={resource.Id} 获得 {earned}",
                LogManager.LogLevelEnum.Debug);

            return price;
        }

        /// <summary>
        /// Worker 从商店购买物品。
        /// </summary>
        /// <param name="worker">购买者</param>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">数量</param>
        /// <returns>购买成功返回 true</returns>
        public bool WorkerBuyFromShop(AWorker worker, int itemId, int count)
        {
            if (worker == null || count <= 0) return false;

            ShopItem shopItem = this.FindShopItem(itemId);
            if (shopItem == null)
            {
                AWorkerTask.LogProvider($"[{this.ShopName}] 不卖 id={itemId}", LogManager.LogLevelEnum.Warning);
                return false;
            }

            if (shopItem.Stock >= 0 && shopItem.Stock < count)
            {
                AWorkerTask.LogProvider($"[{this.ShopName}] 库存不足 id={itemId}", LogManager.LogLevelEnum.Warning);
                return false;
            }

            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return false;

            int totalPrice = shopItem.Price * count;
            CurrencyAmount cost = new CurrencyAmount(totalPrice);

            if (!wd.Wallet.HasEnough(cost))
            {
                AWorkerTask.LogProvider(
                    $"[{this.ShopName}] {worker.name} 余额不足: 需要{cost}, 余额{wd.Wallet}",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            // 扣钱
            wd.Wallet -= cost;
            wd.Personality = wd.Personality.AfterSpendGold(totalPrice);

            // 给物品
            worker.AddResource(new ResourceInfo(itemId, count));

            // 库存减少
            if (shopItem.Stock >= 0)
                shopItem.Stock -= count;

            AWorkerTask.LogProvider(
                $"[{this.ShopName}] {worker.name} 购买 {count}×{shopItem.CnName}(id={itemId}) 花费 {cost}",
                LogManager.LogLevelEnum.Debug);

            return true;
        }

        /// <summary>
        /// Worker 自动到商店买卖（由 WorkerBrain 调用）。
        /// 饥饿时尝试买食物，背包满时卖资源。
        /// </summary>
        public bool WorkerAutoTrade(AWorker worker)
        {
            if (worker == null) return false;
            if (!this.IsInRange(worker)) return false;

            bool didSomething = false;
            AWorker.WorkerData wd = worker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null) return false;

            // 饥饿 + 有钱 → 买食物
            if (wd.CurHungry < AWorker.ThresholdHungry && wd.Wallet.Gold >= 5)
            {
                foreach (ShopItem item in this.ItemsForSale)
                {
                    AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(item.ItemId);
                    if (itemType == AItem.ItemTypeEnum.Food && item.Price <= wd.Wallet.Gold)
                    {
                        if (this.WorkerBuyFromShop(worker, item.ItemId, 1))
                        {
                            wd.CurHungry = Mathf.Min(wd.MaxHungry, wd.CurHungry + 30f);
                            didSomething = true;
                            break;
                        }
                    }
                }
            }

            // 背包满 → 卖资源
            List<ResourceInfo> resources = worker.GetAllResources();
            int total = 0;
            foreach (var r in resources) total += r.Count;
            if (total > wd.MaxResourceCount * 0.7f && wd.Personality.Ambition > 40f)
            {
                foreach (var r in resources)
                {
                    if (r.Count > 0 && this.FindShopItem(r.Id) != null)
                    {
                        int earned = this.WorkerSellToShop(worker,
                            new ResourceInfo(r.Id, r.Count, r.OwnerId));
                        if (earned > 0) didSomething = true;
                    }
                }
            }

            return didSomething;
        }

        /// <summary>
        /// 根据 itemId 查找商店中的商品（公开方法，供 ShopPanel 等 UI 使用）。
        /// </summary>
        public ShopItem FindShopItemPublic(int itemId)
        {
            return this.FindShopItem(itemId);
        }

        private ShopItem FindShopItem(int itemId)
        {
            foreach (ShopItem item in this.ItemsForSale)
            {
                if (item.ItemId == itemId) return item;
            }
            return null;
        }

        /// <summary>
        /// Player 从商店购买物品。货币通过 CurrencyManager（ownerId=0）管理。
        /// 物品由调用方（ShopPanel）负责添加到 Player 背包。
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">购买数量</param>
        /// <returns>购买成功返回 true</returns>
        public bool PlayerBuyFromShop(int itemId, int count)
        {
            if (count <= 0) return false;

            ShopItem shopItem = this.FindShopItem(itemId);
            if (shopItem == null)
            {
                AWorkerTask.LogProvider($"[{this.ShopName}] 不卖 id={itemId}", LogManager.LogLevelEnum.Warning);
                return false;
            }

            if (shopItem.Stock >= 0 && shopItem.Stock < count)
            {
                AWorkerTask.LogProvider($"[{this.ShopName}] 库存不足 id={itemId}", LogManager.LogLevelEnum.Warning);
                return false;
            }

            int totalPrice = shopItem.Price * count;
            CurrencyManager currency = Core.ServiceLocator.Get<CurrencyManager>();
            if (!currency.TrySpendPlayerGold(totalPrice))
            {
                AWorkerTask.LogProvider(
                    $"[{this.ShopName}] Player 余额不足: 需要{totalPrice}G, 余额{currency.GetPlayerBalance().Gold}G",
                    LogManager.LogLevelEnum.Warning);
                return false;
            }

            // 库存减少
            if (shopItem.Stock >= 0)
                shopItem.Stock -= count;

            AWorkerTask.LogProvider(
                $"[{this.ShopName}] Player 购买 {count}×{shopItem.CnName}(id={itemId}) 花费 {totalPrice}G",
                LogManager.LogLevelEnum.Info);

            return true;
        }

        /// <summary>
        /// Player 向商店出售资源。货币通过 CurrencyManager（ownerId=0）管理。
        /// 物品由调用方（ShopPanel）负责从 Player 背包扣除。
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="count">出售数量</param>
        /// <returns>获得的金币，出售失败返回 0</returns>
        public int PlayerSellToShop(int itemId, int count)
        {
            if (count <= 0) return 0;

            ShopItem shopItem = this.FindShopItem(itemId);
            int price;
            if (shopItem != null)
            {
                price = Mathf.FloorToInt(shopItem.Price * this.BuybackRate);
            }
            else
            {
                // 未配置的物品用市场底价
                price = this.market?.GetSellPrice(itemId, count) ?? count;
            }

            if (price <= 0) return 0;

            Core.ServiceLocator.Get<CurrencyManager>().AddPlayerGold(price);

            // 商店库存增加（如果有限库存的话）
            if (shopItem != null && shopItem.Stock >= 0)
                shopItem.Stock += count;

            string itemName = shopItem?.CnName ?? $"id={itemId}";
            AWorkerTask.LogProvider(
                $"[{this.ShopName}] Player 出售 {count}×{itemName} 获得 {price}G",
                LogManager.LogLevelEnum.Info);

            return price;
        }

        /// <summary>
        /// Player 是否在交互范围内。
        /// </summary>
        public bool IsPlayerInRange()
        {
            var player = Core.ServiceLocator.Get<Character.Player.PlayerManager>().Mine;
            if (player == null) return false;
            return Vector3.Distance(player.transform.position, this.transform.position) <= this.InteractionRadius;
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Alpha7) && !Input.GetKeyDown(KeyCode.Keypad7)) return;
            if (this.isShopOpen) return;
            if (!this.IsPlayerInRange()) return;

            this.isShopOpen = true;
            OnShopInteract?.Invoke(this);
        }

        /// <summary>
        /// 商店面板关闭时由 ShopPanel 调用，重置交互状态。
        /// </summary>
        public void OnShopClosed()
        {
            this.isShopOpen = false;
        }

        private bool IsInRange(AWorker worker)
        {
            return Vector3.Distance(worker.transform.position, this.transform.position) <= this.InteractionRadius;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(this.transform.position, this.InteractionRadius);
        }
#endif
    }
}
