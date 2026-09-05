namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Gameplay;
    using LAB2D.Tool;
    using LAB2D.UI.Panel;
    using LAB2D.UnityAdapter;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 山门核心常驻 HUD — 等级/耐久/被破次数可视化 + 金币升级按钮 + 终局「查看结算」入口。
    /// 骨架照 WeatherGameplayHUD，两处偏差：①默认常驻显示（核心状态是胜负关键信息）；
    /// ②SetVisible 联动 interactable/blocksRaycasts（本 HUD 含可交互按钮）。
    /// 数据源 MountainGateManager（CoreChanged 事件驱动 + 0.5s 轮询兜底），
    /// 升级消耗规则同 BuildingDamageRuleService.GetCoreUpgradeCost（金币不足置灰）。
    /// UI 全部运行时代码构建（Game.unity 无法手改 YAML），由 GlobalPanelInitializer 装配。
    /// </summary>
    [DisallowMultipleComponent]
    public class MountainGateHUD : MonoBehaviour
    {
        /// <summary>HUD 根节点名称，装配与查找共用。</summary>
        public const string HudRootName = "MountainGateHUD";

        private CanvasGroup canvasGroup;
        private float nextRefreshTime;
        private MountainGateManager gateManager;
        private bool uiBuilt;

        private readonly BuildingDamageRuleService ruleService = new();

        private Text levelText;
        private Text downfallText;
        private Image hpBarFill;
        private Text hpText;
        private Button upgradeButton;
        private Text upgradeText;
        private Button resultButton;

        /// <summary>像素字体缓存（ark-pixel 加载失败回退 Unity 内置）。</summary>
        
        /// <summary>
        /// HUD 刷新间隔（轮询兜底，正常由 CoreChanged 事件驱动），避免每帧拼接文本。
        /// </summary>
        public float refreshInterval = 0.5f;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = InputKeyConstant.ToggleMountainGateHud;

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = InputKeyConstant.ToggleMountainGateHud;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.BuildUI();

            // 偏差①：默认常驻显示（WeatherGameplayHUD 默认隐藏，本 HUD 是胜负关键信息）
            this.SetVisible(true);
            this.RefreshDisplay();
        }

        private void OnEnable()
        {
            try
            {
                this.gateManager = MountainGateManager.Instance;
                this.gateManager.CoreChanged += this.HandleCoreChanged;
            }
            catch (Exception exception)
            {
                AWorkerTask.LogProvider(
                    $"[GateDiag] MountainGateHUD 绑定山门数据失败: {exception.Message}",
                    LogManager.LogLevelEnum.Warning);
            }
        }

        private void OnDisable()
        {
            try
            {
                if (this.gateManager != null)
                {
                    this.gateManager.CoreChanged -= this.HandleCoreChanged;
                }
            }
            catch (Exception ex)
            {
                AWorkerTask.LogProvider(
                    $"[GateDiag] MountainGateHUD 退订失败（退出期，预期内）: {ex.Message}",
                    LogManager.LogLevelEnum.Trace);
            }
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            // 隐藏时不刷新（不可见的拼接纯属浪费）；切回可见时 SetVisible 置 0 补一次刷新
            bool isVisible = this.canvasGroup == null || this.canvasGroup.alpha > 0.5f;
            if (!isVisible)
            {
                return;
            }

            if (Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
                this.RefreshDisplay();
            }
        }

        /// <summary>
        /// 设置 HUD 是否可见。偏差②：联动 interactable/blocksRaycasts（本 HUD 含升级/结算按钮，
        /// 可见时必须可点击、隐藏时不得挡射线）。
        /// </summary>
        /// <param name="visible">是否显示。</param>
        public void SetVisible(bool visible)
        {
            if (this.canvasGroup == null)
            {
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = visible;
            this.canvasGroup.blocksRaycasts = visible;

            // 重新显示时立即刷新（隐藏期间不刷新会残留旧数据），置 0 让下帧 Update 放行
            if (visible)
            {
                this.nextRefreshTime = 0f;
            }
        }

        /// <summary>
        /// 核心状态变化回调（事件驱动刷新）。
        /// </summary>
        private void HandleCoreChanged()
        {
            this.RefreshDisplay();
        }

        /// <summary>
        /// 刷新等级/耐久/被破与升级按钮状态：终局后升级钮置灰「已终局」并露出「查看结算」；
        /// 平时按金币门槛置灰（消耗规则与 TryUpgradeCore 同源）。
        /// </summary>
        private void RefreshDisplay()
        {
            if (!this.uiBuilt)
            {
                return;
            }

            MountainGateManager gate = this.gateManager ?? MountainGateManager.Instance;

            this.levelText.text = $"等级 {gate.CoreLevel}/{BuildingDamageRuleService.CoreMaxLevel}";
            this.downfallText.text = $"被破 {gate.DownfallCount}/{BuildingDamageRuleService.CoreMaxDownfalls}";
            this.hpBarFill.fillAmount = Mathf.Clamp01(gate.CoreHp / BuildingDamageRuleService.CoreMaxHp);
            this.hpText.text = $"{gate.CoreHp:F0}/{BuildingDamageRuleService.CoreMaxHp:F0}";

            bool ended = gate.IsGameOver || gate.IsVictory;
            if (ended)
            {
                this.upgradeButton.interactable = false;
                this.upgradeText.text = "已终局";
                this.resultButton.gameObject.SetActive(true);
                return;
            }

            this.resultButton.gameObject.SetActive(false);
            int cost = this.ruleService.GetCoreUpgradeCost(gate.CoreLevel);
            int gold = 0;
            try
            {
                CurrencyManager currency = Core.ServiceLocator.Get<CurrencyManager>();
                gold = currency?.GetPlayerBalance().Gold ?? 0;
            }
            catch (Exception)
            {
                // 货币服务未就绪（Editor 测试环境）按 0 处理
            }

            this.upgradeButton.interactable = cost > 0 && gold >= cost;
            this.upgradeText.text = cost > 0 ? $"升级核心 ({cost}G)" : "核心已满级";
        }

        /// <summary>
        /// 升级按钮：调 TryUpgradeCore（扣金币/胜利判定/Tip 均由 Manager 出），事件会驱动刷新，此处再兜一次。
        /// </summary>
        private void OnClick_Upgrade()
        {
            MountainGateManager gate = this.gateManager ?? MountainGateManager.Instance;
            bool success = gate.TryUpgradeCore();
            AWorkerTask.LogProvider(
                $"[GateDiag] HUD 升级点击 → {(success ? "成功" : "失败")} level={gate.CoreLevel}",
                LogManager.LogLevelEnum.Debug);
            this.RefreshDisplay();
        }

        /// <summary>
        /// 查看结算按钮：重开终局结算面板（SessionEndPanel.OpenLast 从缓存或终局态推断）。
        /// </summary>
        private void OnClick_ShowResult()
        {
            try
            {
                SessionEndPanel.Instance.OpenLast();
            }
            catch (Exception ex)
            {
                AWorkerTask.LogProvider(
                    $"[GateDiag] HUD 打开结算面板失败: {ex.Message}",
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 构建 HUD 子树（250×175 右上角，底板 ViewportBg；底板不挡射线，按钮可交互）。
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

            // 右上角锚定（ColonyCommandCenterHUD 上方空闲区）
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-16f, -16f);
            root.sizeDelta = new Vector2(250f, 175f);

            Image background = this.gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = this.gameObject.AddComponent<Image>();
            }

            background.color = PixelUITheme.ViewportBg;
            background.raycastTarget = false;

            Text title = CreateText(root, "Title", "山门核心 (G)", 12, PixelUITheme.VictoryTitle);
            SetCenterRect(title.GetComponent<RectTransform>(), new Vector2(0f, 72f), new Vector2(230f, 24f));
            title.alignment = TextAnchor.MiddleCenter;

            this.levelText = CreateText(root, "Level", "等级 1/3", 12, PixelUITheme.TextOnDark);
            SetCenterRect(this.levelText.GetComponent<RectTransform>(), new Vector2(-57f, 45f), new Vector2(110f, 20f));
            this.levelText.alignment = TextAnchor.MiddleLeft;

            this.downfallText = CreateText(root, "Downfall", "被破 0/3", 12, PixelUITheme.HpBarFill);
            SetCenterRect(this.downfallText.GetComponent<RectTransform>(), new Vector2(57f, 45f), new Vector2(110f, 20f));
            this.downfallText.alignment = TextAnchor.MiddleRight;

            // 血条：黑半透明底 + Filled 横向填充 + 居中数字
            GameObject barBgObject = new GameObject("HpBarBg", typeof(RectTransform), typeof(Image));
            barBgObject.transform.SetParent(root, false);
            SetCenterRect(barBgObject.GetComponent<RectTransform>(), new Vector2(0f, 15f), new Vector2(210f, 18f));
            Image barBg = barBgObject.GetComponent<Image>();
            barBg.color = new Color(0f, 0f, 0f, 0.55f);
            barBg.raycastTarget = false;

            GameObject barFillObject = new GameObject("HpBarFill", typeof(RectTransform), typeof(Image));
            barFillObject.transform.SetParent(barBgObject.transform, false);
            StretchFill(barFillObject.GetComponent<RectTransform>());
            this.hpBarFill = barFillObject.GetComponent<Image>();
            this.hpBarFill.type = Image.Type.Filled;
            this.hpBarFill.fillMethod = Image.FillMethod.Horizontal;
            this.hpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            this.hpBarFill.color = PixelUITheme.HpBarFill;
            this.hpBarFill.raycastTarget = false;

            this.hpText = CreateText(barBgObject.transform, "HpText", string.Empty, 12, Color.white);
            StretchFill(this.hpText.GetComponent<RectTransform>());
            this.hpText.alignment = TextAnchor.MiddleCenter;

            this.upgradeButton = this.CreateButton(root, "UpgradeBtn", "升级核心", new Vector2(0f, -24f), new Vector2(210f, 34f), 12);
            this.upgradeButton.onClick.AddListener(this.OnClick_Upgrade);
            this.upgradeText = this.upgradeButton.transform.Find("Text").GetComponent<Text>();

            this.resultButton = this.CreateButton(root, "ResultBtn", "查看结算", new Vector2(0f, -62f), new Vector2(210f, 28f), 12);
            this.resultButton.onClick.AddListener(this.OnClick_ShowResult);
            this.resultButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// 创建按钮（照 WaveBossRewardPanel.CreateOptionButton：Image 四态 + 子 Text）。
        /// </summary>
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

        /// <summary>
        /// 创建文本（照 WorkerMindUI.CreateText，字体优先 ark-pixel 像素字体）。
        /// </summary>
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

        /// <summary>
        /// 居中锚点矩形的 anchor/pivot/位置/尺寸一次设置。
        /// </summary>
        private static void SetCenterRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// 四向 stretch 拉满父节点。
        /// </summary>
        private static void StretchFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 确保运行时存在 MountainGateHUD：Foreground 下按名查找，无则自建节点 + 挂组件（Game.unity 无场景实例）。
        /// </summary>
        /// <returns>HUD 实例；UI 根不存在时为 null。</returns>
        public static MountainGateHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            if (parent == null)
            {
                return null;
            }

            Transform existingTransform = parent.Find(HudRootName);
            MountainGateHUD hud;
            if (existingTransform != null)
            {
                hud = existingTransform.GetComponent<MountainGateHUD>();
                if (hud == null)
                {
                    hud = existingTransform.gameObject.AddComponent<MountainGateHUD>();
                }
            }
            else
            {
                GameObject hudObject = new GameObject(HudRootName, typeof(RectTransform));
                hudObject.transform.SetParent(parent, false);
                hud = hudObject.AddComponent<MountainGateHUD>();
            }

            HudFactory.RepairExisting(hud, InputKeyConstant.ToggleMountainGateHud, true);
            hud.SetVisible(true);
            return hud;
        }
    }
}
