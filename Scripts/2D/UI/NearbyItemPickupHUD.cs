namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Constant;
    using LAB2D.Item.Backpack;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UI;

    /// <summary>
    /// 附近道具拾取 HUD。
    /// 当玩家周围有地面掉落道具时，显示拾取列表供玩家选择拾取。
    /// 运行时动态创建，挂载到 UI/Foreground 下，复用 UI 的 Canvas。
    /// </summary>
    public class NearbyItemPickupHUD : MonoBehaviour
    {
        public static NearbyItemPickupHUD Instance { get; private set; }

        private GameObject panelRoot;
        private GameObject titleBar;
        private GameObject contentArea;
        private GameObject entriesContainer;
        private ScrollRect scrollRect;
        private GameObject emptyHint;
        private Text titleText;
        private float lastPollTime;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        /// <summary>当前显示的条目（key=tilemap坐标）</summary>
        private Dictionary<Vector3Int, NearbyItemEntry> currentEntries = new Dictionary<Vector3Int, NearbyItemEntry>();

        /// <summary>轮询扫描复用缓冲：扫描结果先写这里，确认有变化后与 currentEntries 交换，避免每次轮询分配新字典</summary>
        private Dictionary<Vector3Int, NearbyItemEntry> foundEntriesBuffer = new Dictionary<Vector3Int, NearbyItemEntry>();

        /// <summary>面板隐藏时的降频轮询间隔（秒）</summary>
        private const float HiddenPollInterval = 2f;

        /// <summary>条目对象池</summary>
        private Stack<GameObject> entryPool = new Stack<GameObject>();

        private struct NearbyItemEntry
        {
            public Vector3Int PosMap;
            public int ItemId;
            public int Count;
            public string ItemName;
            public ABackpackItem.BackpackItemQualityEnum Quality;
        }

        /// <summary>
        /// 确保运行时拾取 HUD 已创建。
        /// </summary>
        public static void EnsureRuntimePanel()
        {
            if (Instance != null)
            {
                return;
            }

            // transform.Find 可查找 inactive 子对象
            Transform uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
            Transform foreground = uiRoot?.Find("Foreground");
            Transform rootTransform = foreground?.Find(NearbyItemPickupConstant.CanvasName);
            if (rootTransform != null)
            {
                Instance = rootTransform.GetComponent<NearbyItemPickupHUD>();
                if (Instance != null)
                {
                    Instance.EnsureReferences();
                }
                else
                {
                    GameLoggerFactory.Get().LogWarning($"[NearbyItemPickupHUD] 场景中存在 {NearbyItemPickupConstant.CanvasName} 但未挂载 NearbyItemPickupHUD 组件。");
                }

                return;
            }

            GameLoggerFactory.Get().LogWarning($"[NearbyItemPickupHUD] 场景中未找到 {NearbyItemPickupConstant.CanvasName}，请手动创建。");
        }

        private void Awake()
        {
            this.EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (this.panelRoot != null) return;

            Transform panelT = this.transform.Find(NearbyItemPickupConstant.PanelRootName);
            this.panelRoot = panelT?.gameObject;
            this.titleBar = panelT?.Find("TitleBar")?.gameObject;
            this.titleText = panelT?.Find("TitleBar/TitleLabel")?.GetComponent<Text>();
            this.contentArea = panelT?.Find("ContentArea")?.gameObject;
            this.scrollRect = panelT?.Find("ContentArea")?.GetComponent<ScrollRect>();
            this.entriesContainer = panelT?.Find("ContentArea/EntriesContainer")?.gameObject;
            this.emptyHint = panelT?.Find("EmptyHint")?.gameObject;
        }

        private void CreateUI()
        {
            // 标题栏
            this.titleBar = new GameObject("TitleBar");
            this.titleBar.transform.SetParent(this.panelRoot.transform, false);
            RectTransform titleRect = this.titleBar.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(NearbyItemPickupConstant.PanelWidth - (NearbyItemPickupConstant.Padding * 2), NearbyItemPickupConstant.TitleBarHeight);

            HorizontalLayoutGroup titleLayout = this.titleBar.AddComponent<HorizontalLayoutGroup>();
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlWidth = false;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = true;

            // 面板标题
            this.titleText = this.CreateText(titleBar.transform, "TitleLabel",
                NearbyItemPickupConstant.PanelTitle, NearbyItemPickupConstant.TitleFontSize,
                NearbyItemPickupConstant.TitleColor, TextAnchor.MiddleLeft, 140f, NearbyItemPickupConstant.TitleBarHeight);

            // 关闭按钮容器
            GameObject closeBtnObj = new GameObject("CloseBtn");
            closeBtnObj.transform.SetParent(this.titleBar.transform, false);
            Image closeBg = closeBtnObj.AddComponent<Image>();
            closeBg.color = new Color(0.5f, 0.15f, 0.15f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => this.Hide());
            RectTransform closeRect = closeBtnObj.GetComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(32f, 32f);

            this.CreateText(closeBtnObj.transform, "CloseLabel", "X", 12, Color.white, TextAnchor.MiddleCenter, 32f, 32f);

            // 滚动区域
            this.contentArea = new GameObject("ContentArea");
            this.contentArea.transform.SetParent(this.panelRoot.transform, false);
            RectTransform contentRect = this.contentArea.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(NearbyItemPickupConstant.PanelWidth - (NearbyItemPickupConstant.Padding * 2), NearbyItemPickupConstant.PanelMaxHeight - NearbyItemPickupConstant.TitleBarHeight - NearbyItemPickupConstant.ItemEntryHeight - 40f);

            Image contentBg = this.contentArea.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);

            Mask mask = this.contentArea.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            this.scrollRect = this.contentArea.AddComponent<ScrollRect>();
            this.scrollRect.horizontal = false;
            this.scrollRect.vertical = true;
            this.scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 滚动内容容器
            this.entriesContainer = new GameObject("EntriesContainer");
            this.entriesContainer.transform.SetParent(this.contentArea.transform, false);
            RectTransform entriesRect = this.entriesContainer.AddComponent<RectTransform>();
            entriesRect.anchorMin = new Vector2(0f, 1f);
            entriesRect.anchorMax = new Vector2(1f, 1f);
            entriesRect.pivot = new Vector2(0.5f, 1f);
            entriesRect.anchoredPosition = Vector2.zero;
            entriesRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup entriesLayout = this.entriesContainer.AddComponent<VerticalLayoutGroup>();
            entriesLayout.spacing = NearbyItemPickupConstant.ItemEntrySpacing;
            entriesLayout.padding = new RectOffset(4, 4, 4, 4);
            entriesLayout.childAlignment = TextAnchor.UpperCenter;
            entriesLayout.childControlWidth = true;
            entriesLayout.childControlHeight = false;
            entriesLayout.childForceExpandWidth = true;
            entriesLayout.childForceExpandHeight = false;

            ContentSizeFitter entriesCsf = this.entriesContainer.AddComponent<ContentSizeFitter>();
            entriesCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            this.scrollRect.content = entriesRect;

            // 空提示
            this.emptyHint = new GameObject("EmptyHint");
            this.emptyHint.transform.SetParent(this.panelRoot.transform, false);
            RectTransform emptyRect = this.emptyHint.AddComponent<RectTransform>();
            emptyRect.sizeDelta = new Vector2(NearbyItemPickupConstant.PanelWidth - (NearbyItemPickupConstant.Padding * 2), NearbyItemPickupConstant.ItemEntryHeight);
            this.CreateText(this.emptyHint.transform, "EmptyLabel",
                NearbyItemPickupConstant.EmptyHint, NearbyItemPickupConstant.ItemNameFontSize,
                NearbyItemPickupConstant.CountColor, TextAnchor.MiddleCenter,
                NearbyItemPickupConstant.PanelWidth - (NearbyItemPickupConstant.Padding * 2), NearbyItemPickupConstant.ItemEntryHeight);
            this.emptyHint.SetActive(true);
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Color color, TextAnchor alignment, float width, float height)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (width > 0f && height > 0f)
            {
                rt.sizeDelta = new Vector2(width, height);
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            return txt;
        }

        /// <summary>
        /// 由 GlobalInit.Update 驱动轮询，不依赖自身的 Update（因为根节点默认关闭）。
        /// </summary>
        public void Tick()
        {
            // 面板隐藏（周围无道具/被关闭）时不做全量轮询：7x7 tile 扫描+字典重建在不可见时纯属浪费。
            // 注意不能硬 return——该面板的隐藏恰好是「无道具」常态，轮询本身就是新掉落物的唤醒探测器，
            // 硬停会让面板此后永久无法再唤出；降频轮询保住唤醒（最多延迟 2s），可见时节奏不变。
            bool panelVisible = this.panelRoot != null && this.panelRoot.activeSelf;
            float interval = panelVisible ? NearbyItemPickupConstant.PollInterval : HiddenPollInterval;
            if (Time.time - this.lastPollTime < interval)
            {
                return;
            }

            this.lastPollTime = Time.time;

            if (!Core.ServiceLocator.TryGet(out ItemMap im) || !Core.ServiceLocator.TryGet(out TileMap tmap))
            {
                return;
            }

            this.PollNearbyItems();
        }

        private float debugLogTimer;
        private void PollNearbyItems()
        {
            Player player = Core.ServiceLocator.TryGet(out PlayerManager pm) ? pm.Mine : null;
            if (player == null)
            {
                this.Hide();
                return;
            }

            Vector3Int playerPosMap = Core.ServiceLocator.Get<TileMap>().WorldPosToMapPos(player.transform.position);

            this.foundEntriesBuffer.Clear();
            Dictionary<Vector3Int, NearbyItemEntry> foundEntries = this.foundEntriesBuffer;
            int radius = NearbyItemPickupConstant.DetectionRadius;
            int totalTilesChecked = 0;
            int tilesFound = 0;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int posMap = new Vector3Int(playerPosMap.x + x, playerPosMap.y + y, 0);
                    totalTilesChecked++;
                    TileBase tile = Core.ServiceLocator.Get<ItemMap>().GetTile(posMap);
                    if (tile == null)
                    {
                        continue;
                    }

                    tilesFound++;

                    if (foundEntries.ContainsKey(posMap))
                    {
                        continue;
                    }

                    int itemId;
                    int count = 1;
                    string itemName = tile.name;
                    ABackpackItem.BackpackItemQualityEnum quality = ABackpackItem.BackpackItemQualityEnum.Gray;

                    ResourceInfo ri = Core.ServiceLocator.Get<DropManager>().GetDropByAll(posMap);
                    if (ri != null && ri.Id > 0)
                    {
                        itemId = ri.Id;
                        count = ri.Count;
                        ItemData itemData = Core.ServiceLocator.Get<ItemDataManager>().GetById(ri.Id);
                        if (itemData != null)
                        {
                            itemName = itemData.CnName;
                        }
                    }
                    else
                    {
                        // Try 版本查询替代异常控制流：非背包物品 tile（如 Bounty 任务栏图标）未注册时返回 false，跳过
                        if (!Core.ServiceLocator.Get<ItemInstanceFactory>().TryGetBackpackItemByName(tile.name, out ABackpackItem bpInstance))
                        {
                            continue;
                        }

                        AItem item = bpInstance;
                        if (item == null)
                        {
                            continue;
                        }

                        itemId = item.Id;
                        ItemData itemData = Core.ServiceLocator.Get<ItemDataManager>().GetById(item.Id);
                        if (itemData != null)
                        {
                            itemName = itemData.CnName;
                        }

                        if (item is ABackpackItem bpItem)
                        {
                            quality = bpItem.Quality;
                        }
                    }

                    // 装备掉落优先从 EnemyLootManager 获取稀有度品质
                    EquipmentRarityType? rarity = Core.ServiceLocator.TryGet(out EnemyLootManager elm) ? elm.TryGetRarityByMapPosition(posMap) : null;
                    if (rarity.HasValue)
                    {
                        quality = EquipmentLootTool.MapRarityToQuality(rarity.Value);
                    }

                    foundEntries[posMap] = new NearbyItemEntry
                    {
                        PosMap = posMap,
                        ItemId = itemId,
                        Count = count,
                        ItemName = itemName,
                        Quality = quality,
                    };
                }
            }

            // 每2秒输出一次调试信息（仅在有道具时输出）
            if (Time.time - this.debugLogTimer > 2f)
            {
                this.debugLogTimer = Time.time;
                if (foundEntries.Count > 0)
                {
                    AWorkerTask.LogProvider(
                        string.Format("NearbyItemPickupHUD: 检测了{0}个tile, 发现{1}个道具tile, 收集到{2}个有效条目",
                            totalTilesChecked, tilesFound, foundEntries.Count),
                        LogManager.LogLevelEnum.Trace);
                }
            }

            // 检查是否有变化
            bool changed = foundEntries.Count != this.currentEntries.Count;
            if (!changed)
            {
                foreach (KeyValuePair<Vector3Int, NearbyItemEntry> kv in foundEntries)
                {
                    if (!this.currentEntries.TryGetValue(kv.Key, out NearbyItemEntry existing) ||
                        existing.ItemId != kv.Value.ItemId ||
                        existing.Count != kv.Value.Count ||
                        existing.Quality != kv.Value.Quality)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            // 交换缓冲：新结果成为 currentEntries，旧字典留作下轮扫描复用，避免分配
            Dictionary<Vector3Int, NearbyItemEntry> oldEntries = this.currentEntries;
            this.currentEntries = foundEntries;
            this.foundEntriesBuffer = oldEntries;
            this.RebuildUI();
        }

        private void RebuildUI()
        {
            // 回收所有条目
            foreach (Transform child in this.entriesContainer.transform)
            {
                child.gameObject.SetActive(false);
                this.entryPool.Push(child.gameObject);
            }

            if (this.currentEntries.Count == 0)
            {
                this.emptyHint.SetActive(true);
                this.panelRoot.SetActive(false);
                this.gameObject.SetActive(false);
                return;
            }

            this.gameObject.SetActive(true);
            this.emptyHint.SetActive(false);
            this.panelRoot.SetActive(true);

            int index = 0;
            foreach (KeyValuePair<Vector3Int, NearbyItemEntry> kv in this.currentEntries)
            {
                NearbyItemEntry entry = kv.Value;
                GameObject entryObj = this.GetOrCreateEntry();
                entryObj.SetActive(true);
                entryObj.transform.SetSiblingIndex(index);
                this.PopulateEntry(entryObj, entry);
                index++;
            }
        }

        private GameObject GetOrCreateEntry()
        {
            if (this.entryPool.Count > 0)
            {
                return this.entryPool.Pop();
            }

            return this.CreateEntryTemplate();
        }

        private GameObject CreateEntryTemplate()
        {
            GameObject entryObj = new GameObject("ItemEntry");
            entryObj.transform.SetParent(this.entriesContainer.transform, false);
            RectTransform entryRect = entryObj.AddComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(NearbyItemPickupConstant.PanelWidth - (NearbyItemPickupConstant.Padding * 2), NearbyItemPickupConstant.ItemEntryHeight);

            Image entryBg = entryObj.AddComponent<Image>();
            entryBg.color = NearbyItemPickupConstant.EntryBgColor;

            HorizontalLayoutGroup entryLayout = entryObj.AddComponent<HorizontalLayoutGroup>();
            entryLayout.padding = new RectOffset(8, 8, 0, 0);
            entryLayout.spacing = 8;
            entryLayout.childAlignment = TextAnchor.MiddleLeft;
            entryLayout.childControlWidth = false;
            entryLayout.childControlHeight = true;
            entryLayout.childForceExpandWidth = false;
            entryLayout.childForceExpandHeight = true;

            // 道具名称
            GameObject nameObj = new GameObject("ItemName");
            nameObj.transform.SetParent(entryObj.transform, false);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            nameText.fontSize = NearbyItemPickupConstant.ItemNameFontSize;
            nameText.color = NearbyItemPickupConstant.ItemNameColor;
            nameText.alignment = TextAnchor.MiddleLeft;
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(80f, NearbyItemPickupConstant.ItemEntryHeight);

            // 数量
            GameObject countObj = new GameObject("ItemCount");
            countObj.transform.SetParent(entryObj.transform, false);
            Text countText = countObj.AddComponent<Text>();
            countText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            countText.fontSize = NearbyItemPickupConstant.CountFontSize;
            countText.color = NearbyItemPickupConstant.CountColor;
            countText.alignment = TextAnchor.MiddleLeft;
            RectTransform countRect = countObj.GetComponent<RectTransform>();
            countRect.sizeDelta = new Vector2(40f, NearbyItemPickupConstant.ItemEntryHeight);

            // 拾取按钮
            GameObject btnObj = new GameObject("PickUpBtn");
            btnObj.transform.SetParent(entryObj.transform, false);
            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = NearbyItemPickupConstant.PickUpBtnColor;
            Button btn = btnObj.AddComponent<Button>();
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(NearbyItemPickupConstant.PickUpButtonWidth, NearbyItemPickupConstant.PickUpButtonHeight);

            GameObject btnLabelObj = new GameObject("BtnLabel");
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            Text btnLabel = btnLabelObj.AddComponent<Text>();
            btnLabel.text = NearbyItemPickupConstant.PickUpButtonText;
            btnLabel.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();
            btnLabel.fontSize = NearbyItemPickupConstant.ButtonFontSize;
            btnLabel.color = Color.white;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform btnLabelRect = btnLabelObj.GetComponent<RectTransform>();
            btnLabelRect.anchorMin = Vector2.zero;
            btnLabelRect.anchorMax = Vector2.one;
            btnLabelRect.sizeDelta = Vector2.zero;

            return entryObj;
        }

        private void PopulateEntry(GameObject entryObj, NearbyItemEntry entry)
        {
            // 名称（按品质着色）
            Text nameText = entryObj.transform.Find("ItemName")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = entry.ItemName;
                nameText.color = EquipmentLootTool.GetQualityColor(entry.Quality);
            }

            // 数量
            Text countText = entryObj.transform.Find("ItemCount")?.GetComponent<Text>();
            if (countText != null)
            {
                countText.text = $"x{entry.Count}";
            }

            // 拾取按钮
            Transform btnTransform = entryObj.transform.Find("PickUpBtn");
            if (btnTransform != null)
            {
                Button btn = btnTransform.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                Vector3Int posMap = entry.PosMap;
                btn.onClick.AddListener(() =>
                {
                    this.OnPickUpClick(posMap);
                });
            }

            // 交替背景色
            Image bg = entryObj.GetComponent<Image>();
            int siblingIndex = entryObj.transform.GetSiblingIndex();
            bg.color = siblingIndex % 2 == 0
                ? NearbyItemPickupConstant.EntryBgColor
                : NearbyItemPickupConstant.EntryBgColorAlt;
        }

        private void OnPickUpClick(Vector3Int posMap)
        {
            Core.ServiceLocator.Get<ItemMap>().PickUpItem(posMap);

            // 立即刷新
            this.currentEntries.Remove(posMap);
            this.RebuildUI();
        }

        private void Hide()
        {
            if (this.panelRoot != null)
            {
                this.panelRoot.SetActive(false);
            }

            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示面板（外部调用，如面板被隐藏后重新显示）。
        /// </summary>
        public void Show()
        {
            this.gameObject.SetActive(true);
            if (this.panelRoot != null)
            {
                this.panelRoot.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
