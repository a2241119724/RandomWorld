namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using LAB2D.Constant;
    using LAB2D.Enum;
    using LAB2D.Gameplay;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 成就浏览面板
    /// 用途：按类别展示所有成就的状态、进度和点数，玩家可在此查看已解锁和未解锁成就。
    /// 通过 F7 键或调用 TogglePanel() 显示/隐藏。
    /// </summary>
    public class AchievementPanel : MonoBehaviour
    {
        private static AchievementPanel runtimeInstance;

        private CanvasGroup canvasGroup;
        private GameObject rootObj;
        private Text titleText;
        private Text summaryText;
        private RectTransform contentArea;
        private RectTransform categoryTabs;
        private GameObject scrollViewObj;
        private Text currentCategoryText;
        private Dictionary<AchievementCategory, GameObject> categoryTabButtons;
        private AchievementCategory activeCategory;
        private bool isVisible;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        // 面板尺寸常量 — 2x 放大版
        private const float PanelWidth = 1800f;
        private const float PanelHeight = 1320f;

        public static void EnsureRuntimePanel()
        {
            if (runtimeInstance != null)
            {
                return;
            }

            // transform.Find 可查找 inactive 子对象，面板已移至 UI/ 下
            Transform uiRoot = AchievementTool.FindUIRoot();
            Transform panelTransform = uiRoot?.Find(AchievementConstant.PanelRootName);
            if (panelTransform != null)
            {
                runtimeInstance = panelTransform.GetComponent<AchievementPanel>();
                if (runtimeInstance != null)
                {
                    runtimeInstance.EnsureReferences();
                }
                else
                {
                    GameLoggerFactory.Get().LogWarning($"[AchievementPanel] 场景中存在 {AchievementConstant.PanelRootName} 但未挂载 AchievementPanel 组件。");
                }

                return;
            }

            GameLoggerFactory.Get().LogWarning($"[AchievementPanel] 场景中未找到 {AchievementConstant.PanelRootName}，请手动创建。");
        }

        public static AchievementPanel RuntimeInstance
        {
            get { return runtimeInstance; }
        }

        private void Awake()
        {
            this.EnsureReferences();
        }

        public void EnsureReferences()
        {
            if (this.rootObj != null) return;

            this.rootObj = this.gameObject;
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            this.categoryTabButtons = new Dictionary<AchievementCategory, GameObject>();
            this.activeCategory = AchievementCategory.Combat;

            this.summaryText = this.transform.Find("Summary")?.GetComponent<Text>();
            this.currentCategoryText = this.transform.Find("CategoryTitle")?.GetComponent<Text>();
            this.categoryTabs = this.transform.Find("CategoryTabs")?.GetComponent<RectTransform>();
            this.scrollViewObj = this.transform.Find("ScrollView")?.gameObject;
            if (this.scrollViewObj != null)
            {
                // 尝试标准路径和直接子节点两种层级
                Transform contentT = this.scrollViewObj.transform.Find("Viewport/Content")
                    ?? this.scrollViewObj.transform.Find("Content");
                this.contentArea = contentT?.GetComponent<RectTransform>();
            }

            // 从场景查找已有 Tab 按钮并连线
            this.WireUpCategoryTabs();
        }

        private void WireUpCategoryTabs()
        {
            if (this.categoryTabs == null) return;

            // 构建反向查找: 显示名称 → AchievementCategory
            var nameToCategory = new Dictionary<string, AchievementCategory>();
            foreach (AchievementCategory cat in System.Enum.GetValues(typeof(AchievementCategory)))
            {
                nameToCategory[AchievementTool.GetCategoryDisplayName(cat)] = cat;
            }

            foreach (Transform child in this.categoryTabs)
            {
                if (!child.name.StartsWith("Tab_")) continue;

                string displayName = child.name.Substring(4); // 去掉 "Tab_" 前缀
                if (!nameToCategory.TryGetValue(displayName, out AchievementCategory cat)) continue;

                GameObject tabObj = child.gameObject;
                this.categoryTabButtons[cat] = tabObj;

                Button tabBtn = tabObj.GetComponent<Button>();
                if (tabBtn != null)
                {
                    tabBtn.onClick.RemoveAllListeners();
                    AchievementCategory captured = cat;
                    tabBtn.onClick.AddListener(() =>
                    {
                        this.activeCategory = captured;
                        this.RefreshPanel();
                        this.HighlightActiveTab(captured);
                    });
                }
            }
        }

        private void HighlightActiveTab(AchievementCategory active)
        {
            foreach (var kvp in this.categoryTabButtons)
            {
                Image img = kvp.Value.GetComponent<Image>();
                if (img != null)
                {
                    img.color = kvp.Key == active ?
                        new Color(0.3f, 0.5f, 0.8f) : new Color(0.2f, 0.2f, 0.25f);
                }
            }
        }

        public void TogglePanel()
        {
            if (this.isVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        public void Show()
        {
            if (!this.isVisible)
            {
                this.rootObj.SetActive(true);
                this.isVisible = true;
                this.RefreshPanel();
            }
        }

        public void Hide()
        {
            if (this.isVisible)
            {
                this.rootObj.SetActive(false);
                this.isVisible = false;
            }
        }

        private void Initialize()
        {
            this.categoryTabButtons = new Dictionary<AchievementCategory, GameObject>();
            this.activeCategory = AchievementCategory.Combat;

            this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            this.rootObj = this.gameObject;

            RectTransform rt = this.gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 60);
            rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            // 面板背景
            Image bg = this.gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

            // 标题栏 (h=128)
            GameObject titleObj = new GameObject("TitleBar");
            titleObj.transform.SetParent(this.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0, 128);

            Image titleBg = titleObj.AddComponent<Image>();
            titleBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            this.titleText = AchievementTool.CreateText(
                titleObj.transform, "Title", AchievementConstant.DefaultPanelTitle, 60);
            RectTransform tRt = this.titleText.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            this.titleText.alignment = TextAnchor.MiddleCenter;
            this.titleText.color = new Color(1f, 0.85f, 0.3f);

            // 摘要行 (y=-128, h=50, font=36)
            GameObject summaryObj = new GameObject("Summary");
            summaryObj.transform.SetParent(this.transform, false);
            RectTransform summaryRt = summaryObj.AddComponent<RectTransform>();
            summaryRt.anchorMin = new Vector2(0, 1);
            summaryRt.anchorMax = new Vector2(1, 1);
            summaryRt.pivot = new Vector2(0.5f, 1);
            summaryRt.anchoredPosition = new Vector2(0, -128);
            summaryRt.sizeDelta = new Vector2(0, 50);

            this.summaryText = summaryObj.AddComponent<Text>();
            this.summaryText.fontSize = 36;
            this.summaryText.color = new Color(0.7f, 0.7f, 0.7f);
            this.summaryText.alignment = TextAnchor.MiddleCenter;
            this.summaryText.raycastTarget = false;
            this.summaryText.font = LAB2D.AI.Dialogue.LLM.UIFontConfig.GetFont();

            // 类别标签栏 (y=-182, h=80)
            GameObject tabsObj = new GameObject("CategoryTabs");
            tabsObj.transform.SetParent(this.transform, false);
            RectTransform tabsRt = tabsObj.AddComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0, 1);
            tabsRt.anchorMax = new Vector2(1, 1);
            tabsRt.pivot = new Vector2(0.5f, 1);
            tabsRt.anchoredPosition = new Vector2(0, -182);
            tabsRt.sizeDelta = new Vector2(0, 80);
            this.categoryTabs = tabsRt;

            Image tabsBg = tabsObj.AddComponent<Image>();
            tabsBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
            tabsBg.raycastTarget = false;

            this.CreateCategoryTabs(tabsRt);

            // 当前类别标题 (y=-268, h=48, font=40)
            this.currentCategoryText = AchievementTool.CreateText(
                this.transform, "CategoryTitle", "战斗", 24);
            RectTransform ctRt = this.currentCategoryText.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 1);
            ctRt.anchorMax = new Vector2(1, 1);
            ctRt.pivot = new Vector2(0.5f, 1);
            ctRt.anchoredPosition = new Vector2(0, -268);
            ctRt.sizeDelta = new Vector2(0, 48);
            this.currentCategoryText.alignment = TextAnchor.MiddleCenter;
            this.currentCategoryText.fontSize = 36;

            // 内容滚动区
            this.CreateScrollContent();

            this.rootObj.SetActive(false);
            this.isVisible = false;
        }

        private void CreateCategoryTabs(RectTransform parent)
        {
            AchievementCategory[] categories =
            {
                AchievementCategory.Combat,
                AchievementCategory.Collection,
                AchievementCategory.Survival,
                AchievementCategory.Wave,
                AchievementCategory.Worker,
            };

            float tabWidth = 300f;
            float spacing = 16f;
            float totalWidth = (tabWidth * categories.Length) + (spacing * (categories.Length - 1));
            float startX = -totalWidth / 2f + tabWidth / 2f;

            for (int i = 0; i < categories.Length; i++)
            {
                AchievementCategory cat = categories[i];
                string catName = AchievementTool.GetCategoryDisplayName(cat);

                GameObject tabObj = new GameObject($"Tab_{catName}");
                tabObj.transform.SetParent(parent, false);
                RectTransform tabRt = tabObj.AddComponent<RectTransform>();
                tabRt.anchorMin = new Vector2(0.5f, 0.5f);
                tabRt.anchorMax = new Vector2(0.5f, 0.5f);
                tabRt.anchoredPosition = new Vector2(startX + (i * (tabWidth + spacing)), 0);
                tabRt.sizeDelta = new Vector2(tabWidth, 70);

                Image tabImg = tabObj.AddComponent<Image>();
                tabImg.color = cat == this.activeCategory ?
                    new Color(0.3f, 0.5f, 0.8f) : new Color(0.2f, 0.2f, 0.25f);

                Button tabBtn = tabObj.AddComponent<Button>();
                AchievementCategory captured = cat;

                tabBtn.onClick.AddListener(() =>
                {
                    this.activeCategory = captured;
                    this.RefreshPanel();
                    foreach (var kvp in this.categoryTabButtons)
                    {
                        Image img = kvp.Value.GetComponent<Image>();
                        img.color = kvp.Key == captured ?
                            new Color(0.3f, 0.5f, 0.8f) : new Color(0.2f, 0.2f, 0.25f);
                    }
                });

                Text tabText = AchievementTool.CreateText(tabObj.transform, "Label", catName, 36);
                RectTransform tRt = tabText.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;
                tabText.alignment = TextAnchor.MiddleCenter;

                this.categoryTabButtons[cat] = tabObj;
            }
        }

        private void CreateScrollContent()
        {
            this.scrollViewObj = new GameObject("ScrollView");
            this.scrollViewObj.transform.SetParent(this.transform, false);
            RectTransform svRt = this.scrollViewObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0, 0);
            svRt.anchorMax = new Vector2(1, 1);
            svRt.offsetMin = new Vector2(30, 40);
            svRt.offsetMax = new Vector2(-30, -324);
            ScrollRect scrollRect = this.scrollViewObj.AddComponent<ScrollRect>();

            Image maskImg = this.scrollViewObj.AddComponent<Image>();
            maskImg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
            Mask mask = this.scrollViewObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(this.scrollViewObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 0);
            this.contentArea = contentRt;

            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 16, 16);

            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        public void RefreshPanel()
        {
            AchievementManager mgr = Core.ServiceLocator.Get<AchievementManager>();
            if (mgr == null || !mgr.IsInitialized)
            {
                return;
            }

            int unlocked = mgr.UnlockedCount;
            int total = mgr.TotalCount;
            int points = mgr.TotalPointsEarned;
            if (this.summaryText != null)
                this.summaryText.text = $"已解锁 {unlocked}/{total} 个成就 | 成就点数: {points}";
            if (this.currentCategoryText != null)
                this.currentCategoryText.text = AchievementTool.GetCategoryDisplayName(this.activeCategory);

            if (this.contentArea == null) return;
            foreach (Transform child in this.contentArea)
            {
                Destroy(child.gameObject);
            }

            foreach (AchievementData data in mgr.AllAchievements)
            {
                if (data.Category != this.activeCategory)
                {
                    continue;
                }

                this.CreateAchievementItem(data);
            }
        }

        private void CreateAchievementItem(AchievementData data)
        {
            GameObject itemObj = new GameObject($"Achievement_{data.Id}");
            itemObj.transform.SetParent(this.contentArea, false);
            RectTransform itemRt = itemObj.AddComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(0, 140);

            Image itemBg = itemObj.AddComponent<Image>();
            switch (data.State)
            {
                case AchievementState.Claimed:
                    itemBg.color = new Color(0.1f, 0.25f, 0.1f, 0.8f);
                    break;
                case AchievementState.Unlocked:
                    itemBg.color = new Color(0.3f, 0.25f, 0.1f, 0.8f);
                    break;
                default:
                    itemBg.color = new Color(0.15f, 0.15f, 0.18f, 0.8f);
                    break;
            }

            HorizontalLayoutGroup hLayout = itemObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 24;
            hLayout.padding = new RectOffset(24, 24, 0, 0);

            // 状态图标 (font=48, w=80)
            string stateIcon = data.State == AchievementState.Claimed ? "[V]" :
                               data.State == AchievementState.Unlocked ? "[!]" : "[ ]";
            AchievementTool.CreateText(itemObj.transform, "StateIcon", stateIcon, 48)
                .GetComponent<RectTransform>().sizeDelta = new Vector2(80, 0);

            // 信息区 (w=800)
            GameObject infoObj = new GameObject("Info");
            infoObj.transform.SetParent(itemObj.transform, false);
            RectTransform infoRt = infoObj.AddComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(800, 0);
            VerticalLayoutGroup vLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 6;

            // 成就名称 (font=32, h=40)
            Text nameTxt = AchievementTool.CreateText(infoObj.transform, "Name", data.Name, 36);
            nameTxt.color = data.State == AchievementState.Locked ? Color.gray : Color.white;
            nameTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

            // 条件描述 (font=24, h=30)
            Text condTxt = AchievementTool.CreateText(infoObj.transform, "Condition", data.ConditionText, 24);
            condTxt.color = new Color(0.6f, 0.6f, 0.6f);
            condTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            // 进度条 (h=28)
            GameObject progressObj = new GameObject("ProgressBar");
            progressObj.transform.SetParent(infoObj.transform, false);
            RectTransform progressRt = progressObj.AddComponent<RectTransform>();
            progressRt.sizeDelta = new Vector2(0, 28);

            GameObject barBg = new GameObject("BarBg");
            barBg.transform.SetParent(progressObj.transform, false);
            RectTransform barBgRt = barBg.AddComponent<RectTransform>();
            barBgRt.anchorMin = Vector2.zero;
            barBgRt.anchorMax = Vector2.one;
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            Image barBgImg = barBg.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f);
            barBgImg.raycastTarget = false;

            GameObject barFill = new GameObject("BarFill");
            barFill.transform.SetParent(progressObj.transform, false);
            RectTransform barFillRt = barFill.AddComponent<RectTransform>();
            barFillRt.anchorMin = new Vector2(0, 0);
            barFillRt.anchorMax = new Vector2(data.ProgressRatio, 1);
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            Image barFillImg = barFill.AddComponent<Image>();
            barFillImg.color = data.State == AchievementState.Locked ?
                new Color(0.4f, 0.6f, 1f) : new Color(0.3f, 1f, 0.5f);
            barFillImg.raycastTarget = false;

            // 进度文本 (font=22)
            Text progressTxt = AchievementTool.CreateText(progressObj.transform, "ProgressText", data.ProgressText, 24);
            progressTxt.alignment = TextAnchor.MiddleCenter;
            progressTxt.color = Color.white;
            RectTransform ptRt = progressTxt.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero;
            ptRt.offsetMax = Vector2.zero;

            // 点数 (font=32, w=180)
            Text pointsTxt = AchievementTool.CreateText(itemObj.transform, "Points", data.PointsText, 36);
            pointsTxt.color = data.State == AchievementState.Locked ? Color.gray : new Color(0.3f, 1f, 0.5f);
            pointsTxt.alignment = TextAnchor.MiddleRight;
            pointsTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 0);
        }
    }
}
