namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Gameplay.Faction;
    using LAB2D.Gameplay;
    using System;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 阵营概览 HUD — 阵营列表（名/成员/金库/核心状态）+ 玩家阵营操作。
    /// 骨架照 MountainGateHUD：全部运行时代码构建（Game.unity 无法手改 YAML），
    /// 由 GlobalPanelInitializer 装配；数据源 FactionManager（FactionChanged 事件驱动
    /// + 1s 轮询兜底）。行槽位固定复用（刷新只改文本，零节点增删）。
    /// 无独立热键（阵营操作低频；显隐由 DebugMenu/后续 FactionPanel 接管）。
    /// </summary>
    [DisallowMultipleComponent]
    public class FactionHUD : MonoBehaviour
    {
        /// <summary>HUD 根节点名称，装配与查找共用。</summary>
        public const string HudRootName = "FactionHUD";

        /// <summary>阵营行槽位上限（超出显示省略行；阵营位随 Worker 数开档，一般 ≤6）。</summary>
        private const int RowSlots = 7;

        private CanvasGroup canvasGroup;
        private float nextRefreshTime;
        private FactionManager factionManager;
        private bool uiBuilt;

        private readonly Text[] rowTexts = new Text[RowSlots];
        private Text playerText;
        private Button createButton;
        private Button leaveButton;
        private Button donateButton;
        private Button exchangeButton;
        private Button upgradeButton;
        private Button warButton;
        private Button peaceButton;

        /// <summary>HUD 刷新间隔（轮询兜底，正常由 FactionChanged 事件驱动）。</summary>
        public float refreshInterval = 1f;

        private void Awake()
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.BuildUI();
            this.SetVisible(true);
            this.RefreshDisplay();
        }

        private void OnEnable()
        {
            try
            {
                this.factionManager = FactionManager.Instance;
                this.factionManager.FactionChanged += this.HandleFactionChanged;
            }
            catch (Exception exception)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] FactionHUD 绑定阵营数据失败: {exception.Message}",
                    LogManager.LogLevelEnum.Warning);
            }
        }

        private void OnDisable()
        {
            try
            {
                if (this.factionManager != null)
                {
                    this.factionManager.FactionChanged -= this.HandleFactionChanged;
                }
            }
            catch (Exception ex)
            {
                AWorkerTask.LogProvider(
                    $"[FactionDiag] FactionHUD 退订失败（退出期，预期内）: {ex.Message}",
                    LogManager.LogLevelEnum.Trace);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
                this.RefreshDisplay();
            }
        }

        /// <summary>设置 HUD 是否可见（含可交互按钮，联动射线阻挡）。</summary>
        public void SetVisible(bool visible)
        {
            if (this.canvasGroup == null)
            {
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = visible;
            this.canvasGroup.blocksRaycasts = visible;
            if (visible)
            {
                this.nextRefreshTime = 0f;
            }
        }

        private void HandleFactionChanged()
        {
            this.RefreshDisplay();
        }

        /// <summary>
        /// 刷新阵营行与玩家操作区：固定槽位改文本（含玩家阵营行与省略行），
        /// 创建/退出按钮按玩家阵营状态切换。
        /// </summary>
        private void RefreshDisplay()
        {
            if (!this.uiBuilt)
            {
                return;
            }

            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            var factions = manager.Factions;

            int row = 0;
            for (; row < factions.Count && row < RowSlots - 1; row++)
            {
                Domain.Gameplay.Faction.WorkerFaction faction = factions[row];
                this.rowTexts[row].gameObject.SetActive(true);
                string leaderTag = string.IsNullOrEmpty(faction.LeaderName) ? string.Empty : $" 领:{faction.LeaderName}";
                this.rowTexts[row].text = faction.Core.IsPlaced
                    ? $"{faction.Name}{leaderTag} {faction.MemberNames.Count}人 库{faction.TreasuryGold} 核Lv{faction.Core.Level} {faction.Core.Hp:F0}hp"
                    : $"{faction.Name}{leaderTag} {faction.MemberNames.Count}人 库{faction.TreasuryGold} 核未立";
            }

            if (row < RowSlots - 1 && factions.Count > 0)
            {
                this.rowTexts[row].gameObject.SetActive(true);
                this.rowTexts[row].text = string.Empty;
                row++;
            }

            for (int i = row; i < RowSlots; i++)
            {
                this.rowTexts[i].gameObject.SetActive(false);
            }

            if (factions.Count == 0)
            {
                this.rowTexts[0].gameObject.SetActive(true);
                this.rowTexts[0].text = "暂无阵营（村民攒钱结社中）";
            }

            bool hasPlayerFaction = manager.PlayerFactionId != 0;
            this.playerText.gameObject.SetActive(true);
            if (hasPlayerFaction && manager.GetFactionById(manager.PlayerFactionId) is WorkerFaction playerFaction)
            {
                float contribution = manager.GetContribution(playerFaction.FactionId, FactionManager.PlayerAccountName);
                FactionRank rank = FactionRankRuleService.GetRank(contribution);
                this.playerText.text =
                    $"玩家:{playerFaction.Name} {FactionRankRuleService.GetRankName(rank)} 贡献{contribution:F0}";
            }
            else
            {
                this.playerText.text = "玩家无阵营";
            }

            this.createButton.gameObject.SetActive(!hasPlayerFaction);
            this.leaveButton.gameObject.SetActive(hasPlayerFaction);
            this.donateButton.gameObject.SetActive(hasPlayerFaction);
            this.exchangeButton.gameObject.SetActive(hasPlayerFaction);
            this.upgradeButton.gameObject.SetActive(hasPlayerFaction);

            // P5 战争操作：宣战对首个他方阵营；和谈对首个交战对手（玩家=自己阵营的领袖）
            WorkerFaction warTarget = hasPlayerFaction ? this.FindFirstOtherFaction(manager) : null;
            this.warButton.gameObject.SetActive(warTarget != null);
            if (warTarget != null)
            {
                this.warButton.GetComponentInChildren<Text>().text = $"宣战 {warTarget.Name}";
            }

            WorkerFaction peaceTarget = hasPlayerFaction ? this.FindFirstWarTarget(manager) : null;
            this.peaceButton.gameObject.SetActive(peaceTarget != null);
            if (peaceTarget != null)
            {
                this.peaceButton.GetComponentInChildren<Text>().text = $"和谈 {peaceTarget.Name}";
            }

            if (hasPlayerFaction && manager.GetFactionById(manager.PlayerFactionId) is WorkerFaction factionForUpgrade)
            {
                int goldCost = FactionCoreRuleService.GetUpgradeGoldCost(factionForUpgrade.Core.Level);
                this.upgradeButton.GetComponentInChildren<Text>().text = goldCost > 0
                    ? $"升级核心 (库{goldCost}G+贡献)"
                    : "核心已满级";
            }
        }

        /// <summary>创建按钮：调 FactionManager.TryCreateFactionByPlayer（扣 800 金入金库）。</summary>
        private void OnClick_Create()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            bool success = manager.TryCreateFactionByPlayer();
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 创建阵营点击 → {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>退出按钮：调 FactionManager.TryLeaveFactionByPlayer。</summary>
        private void OnClick_Leave()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            bool success = manager.TryLeaveFactionByPlayer();
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 退出阵营点击 → {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>捐献按钮：玩家捐 50 金入金库（1:1 折贡献）。</summary>
        private void OnClick_Donate()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            bool success = manager.TryDonateGoldByPlayer(50);
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 捐献50G点击 → {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>兑换按钮：玩家用 50 贡献兑 25 金（日限 50 金内）。</summary>
        private void OnClick_Exchange()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            bool success = manager.TryExchangeContributionByPlayer(25);
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 兑换25G点击 → {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>升级按钮：玩家发起自己阵营核心升级（金库+个人贡献双池扣）。</summary>
        private void OnClick_Upgrade()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            bool success = manager.TryUpgradeCoreByPlayer();
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 升级核心点击 → {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>宣战按钮：玩家（作为领袖）对首个他方阵营宣战（免关系/支持率闸门）。</summary>
        private void OnClick_War()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            WorkerFaction target = this.FindFirstOtherFaction(manager);
            bool success = target != null && manager.TryDeclareWarByPlayer(target.FactionId);
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 宣战点击（→{target?.Name ?? "无目标"}）→ {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>和谈按钮：玩家对首个交战对手和谈。</summary>
        private void OnClick_Peace()
        {
            FactionManager manager = this.factionManager ?? FactionManager.Instance;
            WorkerFaction target = this.FindFirstWarTarget(manager);
            bool success = target != null && manager.TryMakePeaceByPlayer(target.FactionId);
            AWorkerTask.LogProvider(
                $"[FactionDiag] HUD 和谈点击（→{target?.Name ?? "无目标"}）→ {(success ? "成功" : "失败")}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>首个非玩家阵营（宣战按钮目标；无 → null）。</summary>
        private WorkerFaction FindFirstOtherFaction(FactionManager manager)
        {
            foreach (WorkerFaction faction in manager.Factions)
            {
                if (faction.FactionId != manager.PlayerFactionId)
                {
                    return faction;
                }
            }

            return null;
        }

        /// <summary>玩家阵营的首个交战对手（和谈按钮目标；无 → null）。</summary>
        private WorkerFaction FindFirstWarTarget(FactionManager manager)
        {
            if (manager.PlayerFactionId == 0)
            {
                return null;
            }

            foreach (WorkerFaction faction in manager.Factions)
            {
                if (faction.FactionId != manager.PlayerFactionId
                    && manager.IsAtWar(manager.PlayerFactionId, faction.FactionId))
                {
                    return faction;
                }
            }

            return null;
        }

        /// <summary>
        /// 构建 HUD 子树（250×250 右上角、山门 HUD 下方；底板不挡射线，按钮可交互）。
        /// </summary>
        private void BuildUI()
        {
            if (this.uiBuilt)
            {
                return;
            }

            this.uiBuilt = true;

            RectTransform root = this.GetComponent<RectTransform>();
            if (root == null)
            {
                root = this.gameObject.AddComponent<RectTransform>();
            }

            // 右上角锚定（MountainGateHUD 高 175 + 16 间距 → 顶到 -200）
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-16f, -200f);
            root.sizeDelta = new Vector2(250f, 330f);

            Image background = this.gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = this.gameObject.AddComponent<Image>();
            }

            background.color = PixelUITheme.ViewportBg;
            background.raycastTarget = false;

            Text title = CreateText(root, "Title", "阵营", 12, PixelUITheme.VictoryTitle);
            SetCenterRect(title.GetComponent<RectTransform>(), new Vector2(0f, 150f), new Vector2(230f, 24f));
            title.alignment = TextAnchor.MiddleCenter;

            // 阵营行槽位（固定 7 行，从上往下）
            for (int i = 0; i < RowSlots; i++)
            {
                this.rowTexts[i] = CreateText(root, $"Row{i}", string.Empty, 12, PixelUITheme.TextOnDark);
                SetCenterRect(
                    this.rowTexts[i].GetComponent<RectTransform>(),
                    new Vector2(0f, 128f - (i * 20f)),
                    new Vector2(230f, 18f));
                this.rowTexts[i].alignment = TextAnchor.MiddleLeft;
                this.rowTexts[i].gameObject.SetActive(false);
            }

            this.playerText = CreateText(root, "Player", "玩家无阵营", 12, PixelUITheme.TextPrimary);
            SetCenterRect(this.playerText.GetComponent<RectTransform>(), new Vector2(0f, -14f), new Vector2(230f, 20f));
            this.playerText.alignment = TextAnchor.MiddleCenter;

            this.createButton = this.CreateButton(root, "CreateBtn", "创建阵营 (800G)", new Vector2(0f, -40f), new Vector2(210f, 30f), 12);
            this.createButton.onClick.AddListener(this.OnClick_Create);

            // 玩家阵营操作区（P2：捐献/兑换并排，升级/退出全宽）
            this.donateButton = this.CreateButton(root, "DonateBtn", "捐献50G", new Vector2(-56f, -44f), new Vector2(105f, 26f), 12);
            this.donateButton.onClick.AddListener(this.OnClick_Donate);
            this.donateButton.gameObject.SetActive(false);

            this.exchangeButton = this.CreateButton(root, "ExchangeBtn", "兑换25G", new Vector2(56f, -44f), new Vector2(105f, 26f), 12);
            this.exchangeButton.onClick.AddListener(this.OnClick_Exchange);
            this.exchangeButton.gameObject.SetActive(false);

            this.upgradeButton = this.CreateButton(root, "UpgradeBtn", "升级核心", new Vector2(0f, -74f), new Vector2(210f, 26f), 12);
            this.upgradeButton.onClick.AddListener(this.OnClick_Upgrade);
            this.upgradeButton.gameObject.SetActive(false);

            this.leaveButton = this.CreateButton(root, "LeaveBtn", "退出阵营", new Vector2(0f, -104f), new Vector2(210f, 26f), 12);
            this.leaveButton.onClick.AddListener(this.OnClick_Leave);
            this.leaveButton.gameObject.SetActive(false);

            // P5 战争操作（并排，玩家有阵营时显示）
            this.warButton = this.CreateButton(root, "WarBtn", "宣战", new Vector2(-56f, -134f), new Vector2(105f, 26f), 12);
            this.warButton.onClick.AddListener(this.OnClick_War);
            this.warButton.gameObject.SetActive(false);

            this.peaceButton = this.CreateButton(root, "PeaceBtn", "和谈", new Vector2(56f, -134f), new Vector2(105f, 26f), 12);
            this.peaceButton.onClick.AddListener(this.OnClick_Peace);
            this.peaceButton.gameObject.SetActive(false);
        }

        /// <summary>创建按钮（照 MountainGateHUD.CreateButton：Image 四态 + 子 Text）。</summary>
        private Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, int fontSize)
        {
            GameObject buttonObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetCenterRect(buttonObject.GetComponent<RectTransform>(), position, size);

            Image image = buttonObject.GetComponent<Image>();
            image.color = PixelUITheme.ButtonNormal;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = PixelUITheme.ButtonNormal;
            colors.highlightedColor = PixelUITheme.ButtonHighlighted;
            colors.pressedColor = PixelUITheme.ButtonPressed;
            colors.selectedColor = PixelUITheme.ButtonSelected;
            button.colors = colors;

            Text text = CreateText(buttonObject.transform, "Text", label, fontSize, PixelUITheme.TextPrimary);
            StretchFill(text.GetComponent<RectTransform>());
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        /// <summary>创建文本（照 MountainGateHUD.CreateText，字体 ark-pixel 像素字体）。</summary>
        private static Text CreateText(Transform parent, string name, string text, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.font = PixelUITheme.PixelFont;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>居中锚点矩形的 anchor/pivot/位置/尺寸一次设置。</summary>
        private static void SetCenterRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>四向 stretch 拉满父节点。</summary>
        private static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 确保运行时存在 FactionHUD：Foreground 下按名查找，无则自建节点 + 挂组件（Game.unity 无场景实例）。
        /// </summary>
        /// <returns>HUD 实例；UI 根不存在时为 null。</returns>
        public static FactionHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            if (parent == null)
            {
                return null;
            }

            Transform existingTransform = parent.Find(HudRootName);
            FactionHUD hud;
            if (existingTransform != null)
            {
                hud = existingTransform.GetComponent<FactionHUD>();
                if (hud == null)
                {
                    hud = existingTransform.gameObject.AddComponent<FactionHUD>();
                }
            }
            else
            {
                GameObject hudObject = new GameObject(HudRootName, typeof(RectTransform));
                hudObject.transform.SetParent(parent, false);
                hud = hudObject.AddComponent<FactionHUD>();
            }

            hud.SetVisible(true);
            return hud;
        }
    }
}
