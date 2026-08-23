namespace LAB2D.AI.Worker
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Worker;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Worker 间交易服务 — Worker A 饿了可以向 Worker B 购买食物。
    /// 卖家根据人格（社交、心情、贪婪度）决定是否出售。
    /// 纯 C# 服务，不依赖 MonoBehaviour。
    /// </summary>
    public class WorkerTradeService
    {
        /// <summary>交易扫描半径（地图格子）</summary>
        public int TradeRadius = 15;

        /// <summary>食物基础价格（每单位）</summary>
        public int FoodBasePrice = 5;

        /// <summary>卖家拒绝交易时买家的心情惩罚</summary>
        public float RejectionMoodPenalty = 3f;

        /// <summary>
        /// 好感度查询注入点（保持纯 C# 单测能力）：
        /// 返回 (buyer, seller) 交易中 卖者对买者 的好感，默认访问 FavorabilityManager。
        /// </summary>
        public static System.Func<AWorker, AWorker, float> FavorabilityProvider =
            (buyer, seller) => Core.ServiceLocator.Get<FavorabilityManager>()?.GetFavorability(seller, buyer)
                ?? FavorabilityRuleService.InitialFavorability;

        /// <summary>
        /// 尝试让饥饿的买家从附近 Worker 购买食物。
        /// </summary>
        /// <param name="buyer">饥饿的买家</param>
        /// <returns>成功购买返回 true</returns>
        public bool TryBuyFood(AWorker buyer)
        {
            if (buyer == null) return false;

            AWorker.WorkerData buyerData = buyer.CharacterDataLAB as AWorker.WorkerData;
            if (buyerData == null) return false;

            // 1. 先检查自己有没有食物
            ResourceInfo ownFood = this.FindFoodInInventory(buyer);
            if (ownFood != null && ownFood.Count > 0)
            {
                // 自己吃自己的
                int eatCount = Mathf.Min(ownFood.Count, 1);
                buyer.SubResource(new ResourceInfo(ownFood.Id, eatCount));

                // 恢复饥饿值
                buyerData.CurHungry = Mathf.Min(buyerData.MaxHungry, buyerData.CurHungry + Constant.WorkerConditionConstant.HungryRestorePerFood);

                // 心情微升（吃到东西了）
                buyerData.Personality = buyerData.Personality.AfterEarnGold(0); // 复用心情微升

                LogProvider($"{buyer.name} 吃了自己的食物 (id={ownFood.Id}), 饥饿恢复至 {buyerData.CurHungry:F0}");
                return true;
            }

            // 2. 检查仓库
            List<ResourceInfo> storageFoods = this.FindFoodInStorage(buyer);
            if (storageFoods.Count > 0)
            {
                ResourceInfo food = storageFoods[0];
                int take = Mathf.Min(food.Count, 1);
                buyer.WithdrawFromStorage(food.Id, take);
                buyerData.CurHungry = Mathf.Min(buyerData.MaxHungry, buyerData.CurHungry + Constant.WorkerConditionConstant.HungryRestorePerFood);
                LogProvider($"{buyer.name} 从仓库取食物吃, 饥饿恢复至 {buyerData.CurHungry:F0}");
                return true;
            }

            // 3. 没钱 → 无法购买
            if (!buyerData.Wallet.HasEnough(new CurrencyAmount(FoodBasePrice)))
            {
                LogProvider($"{buyer.name} 饥饿但没钱买食物 (余额:{buyerData.Wallet})");
                return false;
            }

            // 4. 扫描附近 Worker 卖食物
            AWorker seller = this.FindNearbyFoodSeller(buyer);
            if (seller == null)
            {
                LogProvider($"{buyer.name} 附近没有卖食物的 Worker");
                return false;
            }

            // 5. 卖家决定是否出售
            AWorker.WorkerData sellerData = seller.CharacterDataLAB as AWorker.WorkerData;
            if (sellerData == null) return false;

            float sellerFavorability = FavorabilityProvider(buyer, seller);

            if (!this.WillSell(sellerData.Personality, sellerFavorability))
            {
                // 卖家拒绝 → 买家心情下降
                buyerData.Personality = new WorkerPersonality(
                    Mathf.Max(0, buyerData.Personality.Mood - RejectionMoodPenalty),
                    buyerData.Personality.Ambition,
                    buyerData.Personality.Diligence,
                    buyerData.Personality.Sociality);

                // 好感度：交易被拒，buyer→seller 好感下降
                Core.ServiceLocator.Get<FavorabilityManager>()?.ModifyFavorability(buyer, seller.GetInstanceID(), FavorabilityConstant.TradeRejectDelta, "交易被拒");

                LogProvider($"{seller.name} 拒绝卖给 {buyer.name} 食物 (社交{sellerData.Personality.Sociality:F0})");
                return false;
            }

            // 6. 交易执行
            int price = this.NegotiatePrice(buyerData.Personality, sellerData.Personality, sellerFavorability);
            ResourceInfo sellerFood = this.FindFoodInInventory(seller);
            if (sellerFood == null || sellerFood.Count <= 0) return false;

            int tradeCount = Mathf.Min(sellerFood.Count, 1);
            seller.SubResource(new ResourceInfo(sellerFood.Id, tradeCount));

            // 转账
            CurrencyAmount cost = new CurrencyAmount(price);
            buyerData.Wallet -= cost;
            sellerData.Wallet += cost;

            // 买家直接吃下食物（恢复饥饿，不经背包）。食物是消耗品，原先
            // AddResource(进背包) + SubResource(扣掉) 是多余绕圈，且首次 AddResource
            // 的 OwnerId=卖家 会污染买家背包同 ID 物品（叠加不改 OwnerId → 自用物被
            // 误判为他人悬赏物不可存）。
            buyerData.CurHungry = Mathf.Min(buyerData.MaxHungry, buyerData.CurHungry + Constant.WorkerConditionConstant.HungryRestorePerFood);

            // 人格更新
            buyerData.Personality = buyerData.Personality.AfterSpendGold(price);
            sellerData.Personality = sellerData.Personality.AfterEarnGold(price);

            // 好感度：交易成功，buyer→seller 好感上升（买方幅度大，卖方小）
            Core.ServiceLocator.Get<FavorabilityManager>()?.ModifyFavorability(buyer, seller.GetInstanceID(), FavorabilityConstant.TradeSuccessBuyerDelta, "交易成功");
            Core.ServiceLocator.Get<FavorabilityManager>()?.ModifyFavorability(seller, buyer.GetInstanceID(), FavorabilityConstant.TradeSuccessSellerDelta, "交易成功");

            LogProvider($"{buyer.name} 从 {seller.name} 购买食物花费 {cost}, 饥饿恢复至 {buyerData.CurHungry:F0}");
            return true;
        }

        /// <summary>
        /// 卖家是否愿意出售（基于人格）。
        /// 社交高的更愿意卖，心情差的不愿意。
        /// </summary>
        public bool WillSell(WorkerPersonality seller, float sellerFavorabilityToBuyer)
        {
            if (seller.Sociality < 35f) return false;  // 太孤僻不卖
            if (seller.Mood < 25f) return false;        // 心情极差不卖
            if (sellerFavorabilityToBuyer < FavorabilityConstant.TradeRefuseThreshold) return false; // 对买家好感过低拒卖

            float baseChance = 0.5f;
            baseChance += (seller.Sociality - 50f) * 0.008f;
            baseChance += (seller.Mood - 50f) * 0.004f;
            return Random.value < Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// 协商价格 — 卖家社交高→便宜，买家社交低→被宰，卖者对买者好感高→折扣。
        /// </summary>
        private int NegotiatePrice(WorkerPersonality buyer, WorkerPersonality seller, float sellerFavorabilityToBuyer)
        {
            float multiplier = 1.0f;
            multiplier -= (seller.Sociality - 50f) * 0.005f; // 卖家社交高→降价最多25%
            multiplier += (50f - buyer.Sociality) * 0.005f;  // 买家社交低→加价最多25%
            multiplier *= FavorabilityRuleService.GetTradePriceMultiplier(sellerFavorabilityToBuyer); // 好感价格乘数 [0.7, 1.3]
            return Mathf.Max(1, Mathf.RoundToInt(FoodBasePrice * Mathf.Clamp(multiplier, 0.5f, 1.5f)));
        }

        /// <summary>
        /// 扫描买家附近有食物的 Worker。
        /// </summary>
        private AWorker FindNearbyFoodSeller(AWorker buyer)
        {
            WorkerManager wm = Core.ServiceLocator.Get<WorkerManager>();
            if (wm == null) return null;

            Vector3 buyerPos = buyer.transform.position;
            AWorker best = null;
            float bestDist = float.MaxValue;

            foreach (AWorker other in wm.Characters)
            {
                if (other == buyer || other == null) continue;

                ResourceInfo food = this.FindFoodInInventory(other);
                if (food == null || food.Count <= 0) continue;

                float dist = Vector3.Distance(buyerPos, other.transform.position);
                if (dist < this.TradeRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }

            return best;
        }

        /// <summary>在 Worker 身上找食物（Food 类型）。</summary>
        private ResourceInfo FindFoodInInventory(AWorker worker)
        {
            List<ResourceInfo> all = worker.GetAllResources();
            foreach (ResourceInfo r in all)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food)
                    return r;
            }
            return null;
        }

        /// <summary>在 Worker 仓库找食物。</summary>
        private List<ResourceInfo> FindFoodInStorage(AWorker worker)
        {
            List<ResourceInfo> result = new List<ResourceInfo>();
            List<ResourceInfo> all = worker.GetStorageResources();
            foreach (ResourceInfo r in all)
            {
                AItem.ItemTypeEnum itemType = AWorkerTask.ItemTypeProvider(r.Id);
                if (itemType == AItem.ItemTypeEnum.Food)
                    result.Add(r);
            }
            return result;
        }

        private static void LogProvider(string msg)
        {
            AWorkerTask.LogProvider($"[Trade] {msg}", LogManager.LogLevelEnum.Debug);
        }
    }
}
