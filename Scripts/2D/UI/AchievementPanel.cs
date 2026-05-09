namespace LAB2D
{
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

        private Canvas canvas;
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

        // 面板尺寸常量 — 2x 放大版
        private const float PanelWidth = 1800f;
        private const float PanelHeight = 1320f;

        public static void EnsureRuntimePanel()
        {
            if (runtimeInstance != null)
            {
                return;
            }

            GameObject canvasObj = AchievementTool.EnsureCanvas(
                AchievementConstant.PanelCanvasName, 150);

            GameObject panelObj = new GameObject(AchievementConstant.PanelRootName);
            panelObj.transform.SetParent(canvasObj.transform, false);
            AchievementPanel panel = panelObj.AddComponent<AchievementPanel>();
            panel.Initialize();
            runtimeInstance = panel;
        }

        public static AchievementPanel RuntimeInstance
        {
            get { return runtimeInstance; }
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

            this.canvas = this.GetComponentInParent<Canvas>();
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
                titleObj.transform, "Title", AchievementConstant.DefaultPanelTitle, 64);
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
            this.summaryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
                this.transform, "CategoryTitle", "战斗", 22);
            RectTransform ctRt = this.currentCategoryText.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0, 1);
            ctRt.anchorMax = new Vector2(1, 1);
            ctRt.pivot = new Vector2(0.5f, 1);
            ctRt.anchoredPosition = new Vector2(0, -268);
            ctRt.sizeDelta = new Vector2(0, 48);
            this.currentCategoryText.alignment = TextAnchor.MiddleCenter;
            this.currentCategoryText.fontSize = 40;

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

                Text tabText = AchievementTool.CreateText(tabObj.transform, "Label", catName, 32);
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
            AchievementManager mgr = AchievementManager.Instance;
            if (mgr == null || !mgr.IsInitialized)
            {
                return;
            }

            int unlocked = mgr.UnlockedCount;
            int total = mgr.TotalCount;
            int points = mgr.TotalPointsEarned;
            this.summaryText.text = $"已解锁 {unlocked}/{total} 个成就 | 成就点数: {points}";
            this.currentCategoryText.text = AchievementTool.GetCategoryDisplayName(this.activeCategory);

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
            Text nameTxt = AchievementTool.CreateText(infoObj.transform, "Name", data.Name, 32);
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
            Text progressTxt = AchievementTool.CreateText(progressObj.transform, "ProgressText", data.ProgressText, 22);
            progressTxt.alignment = TextAnchor.MiddleCenter;
            progressTxt.color = Color.white;
            RectTransform ptRt = progressTxt.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.offsetMin = Vector2.zero;
            ptRt.offsetMax = Vector2.zero;

            // 点数 (font=32, w=180)
            Text pointsTxt = AchievementTool.CreateText(itemObj.transform, "Points", data.PointsText, 32);
            pointsTxt.color = data.State == AchievementState.Locked ? Color.gray : new Color(0.3f, 1f, 0.5f);
            pointsTxt.alignment = TextAnchor.MiddleRight;
            pointsTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 0);
        }
    }
}
