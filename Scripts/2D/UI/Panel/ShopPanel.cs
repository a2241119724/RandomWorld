namespace LAB2D.UI.Panel
{
    using LAB2D;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Gameplay;
    using LAB2D.Item;
    using LAB2D.Map;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UI;

    /// <summary>
    /// 商店 UI 面板 — 玩家与 ShopNPC 交互时显示。
    /// 继承 ABasePanel，IsOverlay=true（不暂停游戏）。
    /// UI 结构在 Unity Editor 中搭建，代码只负责数据绑定和交互。
    /// </summary>
    public class ShopPanel : ABasePanel<ShopPanel>
    {
        /// <summary>当前打开的商店</summary>
        public static ShopNPC CurrentShop { get; private set; }

        // ---- 缓存的 UI 引用 ----
        private Text titleText;
        private Text goldText;
        private Transform buyContent;
        private Transform sellContent;

        public ShopPanel()
        {
            this.Name = "ShopPanel";
            this.Init();
            this.BindUI();
            ShopNPC.OnShopInteract = this.OpenShop;
        }

        /// <inheritdoc/>
        public override bool IsOverlay => true;

        // ============================================================
        // UI 绑定（从 Editor 搭建的层级中查找子物体）
        // ============================================================

        private void BindUI()
        {
            Transform t = this.Panel.transform;

            this.titleText = t.Find("Title")?.GetComponent<Text>();
            this.goldText = t.Find("Gold")?.GetComponent<Text>();

            this.buyContent = t.Find("BuyScroll/Viewport/Content");
            this.sellContent = t.Find("SellScroll/Viewport/Content");

            Button closeBtn = t.Find("CloseBtn")?.GetComponent<Button>();
            if (closeBtn != null) closeBtn.onClick.AddListener(() => this.Controller.Close());
        }

        // ============================================================
        // 公共接口
        // ============================================================

        /// <summary>
        /// 打开指定商店的面板。
        /// </summary>
        public void OpenShop(ShopNPC shop)
        {
            if (shop == null) return;
            CurrentShop = shop;
            this.RefreshUI();
            this.Controller.Show(this);
        }

        /// <inheritdoc/>
        public override void OnEnter()
        {
            base.OnEnter();
            this.RefreshUI();
        }

        /// <inheritdoc/>
        public override void OnExit()
        {
            base.OnExit();
            this.ClearContent(this.buyContent);
            this.ClearContent(this.sellContent);
            if (CurrentShop != null) CurrentShop.OnShopClosed();
        }

        /// <inheritdoc/>
        public override void OnClick_Back()
        {
            this.Controller.Close();
        }

        // ============================================================
        // UI 刷新
        // ============================================================

        public void RefreshUI()
        {
            if (CurrentShop == null) return;

            if (this.titleText != null)
                this.titleText.text = CurrentShop.ShopName;

            if (this.goldText != null)
            {
                CurrencyManager currency = ServiceLocator.Get<CurrencyManager>();
                this.goldText.text = $"金币: {currency.GetPlayerBalance().Gold}G";
            }

            this.PopulateBuyList();
            this.PopulateSellList();
        }

        private void PopulateBuyList()
        {
            if (this.buyContent == null) return;
            this.ClearContent(this.buyContent);

            if (CurrentShop.ItemsForSale == null || CurrentShop.ItemsForSale.Count == 0)
                return;

            foreach (ShopNPC.ShopItem item in CurrentShop.ItemsForSale)
            {
                this.CreateItemRow(this.buyContent, item);
            }
        }

        private void PopulateSellList()
        {
            if (this.sellContent == null) return;
            this.ClearContent(this.sellContent);

            List<ResourceInfo> playerItems = this.GetPlayerResources();
            if (playerItems.Count == 0) return;

            foreach (ResourceInfo res in playerItems)
            {
                if (res.Count <= 0) continue;
                ShopNPC.ShopItem shopItem = CurrentShop.FindShopItemPublic(res.Id);
                if (shopItem == null) continue;

                int buybackPrice = Mathf.FloorToInt(shopItem.Price * CurrentShop.BuybackRate);
                string itemName = string.IsNullOrEmpty(shopItem.CnName) ? $"id={res.Id}" : shopItem.CnName;
                this.CreateSellRow(this.sellContent, res.Id, itemName, res.Count, buybackPrice);
            }
        }

        // ---- 行创建（使用预制体 BuyItem / SellItem） ----

        private void CreateItemRow(Transform parent, ShopNPC.ShopItem item)
        {
            GameObject go = Core.ServiceLocator.Get<ResourceManager>().Instantiate("BuyItem", parent, false);
            if (go == null) return;

            string stockText = item.Stock < 0 ? "∞" : item.Stock.ToString();
            string nameText = string.IsNullOrEmpty(item.CnName) ? $"id={item.ItemId}" : item.CnName;

            SetChildText(go.transform, "Name", nameText);
            SetChildText(go.transform, "Price", $"{item.Price}G");
            SetChildText(go.transform, "Stock", $"x{stockText}");

            Button buyBtn = go.transform.Find("BuyBtn")?.GetComponent<Button>();
            if (buyBtn != null)
            {
                ShopNPC shop = CurrentShop;
                buyBtn.onClick.AddListener(() =>
                {
                    if (shop.PlayerBuyFromShop(item.ItemId, 1))
                    {
                        this.AddItemToPlayer(item.ItemId, 1, item.CnName);
                        this.RefreshUI();
                    }
                });
            }
        }

        private void CreateSellRow(Transform parent, int itemId, string itemName, int count, int price)
        {
            GameObject go = Core.ServiceLocator.Get<ResourceManager>().Instantiate("SellItem", parent, false);
            if (go == null) return;

            SetChildText(go.transform, "Name", $"{itemName} ×{count}");
            SetChildText(go.transform, "Price", $"卖{price}G");

            Button sellBtn = go.transform.Find("SellBtn")?.GetComponent<Button>();
            if (sellBtn != null)
            {
                ShopNPC shop = CurrentShop;
                int capturedId = itemId;
                sellBtn.onClick.AddListener(() =>
                {
                    int earned = shop.PlayerSellToShop(capturedId, 1);
                    if (earned > 0)
                    {
                        this.RemoveItemFromPlayer(capturedId, 1);
                        this.RefreshUI();
                    }
                });
            }
        }

        private static void SetChildText(Transform parent, string childName, string text)
        {
            Text txt = parent.Find(childName)?.GetComponent<Text>();
            if (txt != null) txt.text = text;
        }

        private void ClearContent(Transform content)
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(content.GetChild(i).gameObject);
            }
        }

        // ============================================================
        // Player 物品管理（本地字典 + ItemMap 落地）
        // ============================================================

        private static readonly Dictionary<int, int> ownedItems = new Dictionary<int, int>();

        private List<ResourceInfo> GetPlayerResources()
        {
            List<ResourceInfo> result = new List<ResourceInfo>();
            foreach (var kv in ownedItems)
            {
                if (kv.Value > 0)
                    result.Add(new ResourceInfo(kv.Key, kv.Value) { OwnerId = 0 });
            }
            return result;
        }

        private void AddItemToPlayer(int itemId, int count, string itemName)
        {
            if (ownedItems.ContainsKey(itemId))
                ownedItems[itemId] += count;
            else
                ownedItems[itemId] = count;

            try
            {
                PlayerManager pm = ServiceLocator.Get<PlayerManager>();
                if (pm?.Mine != null)
                {
                    TileMap tileMap = ServiceLocator.Get<TileMap>();
                    Vector3Int playerPos = tileMap.WorldPosToMapPos(pm.Mine.transform.position);
                    Vector3Int dropPos = FindNearbyEmptyPos(playerPos);
                    TileBase tile = (TileBase)AWorkerTask.ResourceLoadProvider(GetItemTileName(itemId));
                    ResourceInfo ri = new ResourceInfo(itemId, count) { OwnerId = 0 };
                    ItemMap.Instance.PutDownToDrop(dropPos, tile, ri);
                }
            }
            catch (System.Exception ex)
            {
                AWorkerTask.LogProvider(
                    $"[ShopPanel] 物品落地失败 {itemName}: {ex.Message}", LogManager.LogLevelEnum.Warning);
            }
        }

        private void RemoveItemFromPlayer(int itemId, int count)
        {
            if (ownedItems.TryGetValue(itemId, out int current))
            {
                int remaining = current - count;
                if (remaining <= 0) ownedItems.Remove(itemId);
                else ownedItems[itemId] = remaining;
            }
        }

        private static Vector3Int FindNearbyEmptyPos(Vector3Int center)
        {
            for (int r = 1; r <= 3; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);
                        TileMap tileMap = ServiceLocator.Get<TileMap>();
                        if (tileMap.IsCanReach(pos) && ItemMap.Instance.IsFreeTile(pos))
                            return pos;
                    }
                }
            }
            return center;
        }

        private static string GetItemTileName(int itemId)
        {
            try
            {
                ItemDataManager idm = ServiceLocator.Get<ItemDataManager>();
                ItemData itemData = idm.GetById(itemId);
                if (itemData != null && !string.IsNullOrEmpty(itemData.CnName))
                {
                    var rm = ServiceLocator.Get<ResourceManager>();
                    if (rm.GetAsset(itemData.CnName) != null) return itemData.CnName;
                }
            }
            catch { }
            return "CustomWood";
        }
    }
}
