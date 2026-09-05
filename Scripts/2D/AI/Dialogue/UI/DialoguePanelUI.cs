namespace LAB2D.AI.Dialogue.UI
{
    using LAB2D;
    using LAB2D.AI.Dialogue.Core;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.UI.Panel;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// NPC 对话面板 UI
    /// </summary>
    public class DialoguePanelUI : MonoBehaviour
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static DialoguePanelUI Instance { get; private set; }

        internal static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (msg, lv) => ServiceLocator.Get<LogManager>().Log(msg, lv);
        internal static System.Func<string, bool, GameObject> ResourceInstantiateProvider { get; set; }
            = (name, active) => ServiceLocator.Get<ResourceManager>().Instantiate(name, active);
        private const float BubbleMinWidth = 96f;
        private const float BubbleMaxWidth = 560f;
        private const float BubbleHorizontalPadding = 32f;

        private Transform content;
        private ScrollRect scrollRect;
        private InputField inputField;
        private Button sendButton;
        private Button backButton;
        private Button settingsButton;
        private Toggle deepThinkingToggle;
        private Text npcNameText;
        private Text tokenUsageText;
        private string activeNpcId;
        private StreamingTextView currentStreamingText;
        private bool isWaitingResponse;
        private TokenUsageInfo currentTokenUsage;
        private readonly Dictionary<DialogueIntentKind, Button> intentButtons = new Dictionary<DialogueIntentKind, Button>();

        /// <summary>
        /// 确保场景中已创建的 UI 实例可用。
        /// </summary>
        public static DialoguePanelUI Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            // 查找已存在的实例
            GameObject existing = GameObject.Find("DialoguePanelUI");
            if (existing != null)
            {
                DialoguePanelUI ui = existing.GetComponent<DialoguePanelUI>();
                if (ui != null)
                {
                    Instance = ui;
                    return ui;
                }
            }

            Transform uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
            Transform inactiveExisting = uiRoot != null ? uiRoot.Find("DialoguePanelUI") : null;
            if (inactiveExisting != null)
            {
                DialoguePanelUI ui = inactiveExisting.GetComponent<DialoguePanelUI>();
                if (ui != null)
                {
                    Instance = ui;
                    return ui;
                }
            }

            // 在报告场景设置缺失之前，先搜索未激活的场景对象。
            foreach (DialoguePanelUI ui in Resources.FindObjectsOfTypeAll<DialoguePanelUI>())
            {
                if (ui != null && ui.gameObject.scene.IsValid())
                {
                    Instance = ui;
                    return ui;
                }
            }

            LogProvider(
                "DialoguePanelUI: scene instance is missing. Add DialoguePanelUI to Game.unity.",
                LogManager.LogLevelEnum.Error);
            return null;
        }

        public void Awake()
        {
            ServiceLocator.Register(this);
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
            this.FindUIElements();
            this.gameObject.SetActive(false);
        }

        public void OnEnable()
        {
            if (ServiceLocator.Get<DialogueManager>() != null)
            {
                ServiceLocator.Get<DialogueManager>().OnTokenReceived += this.HandleToken;
                ServiceLocator.Get<DialogueManager>().OnDialogueComplete += this.HandleComplete;
                ServiceLocator.Get<DialogueManager>().OnDialogueError += this.HandleError;
                ServiceLocator.Get<DialogueManager>().OnTokenUsage += this.HandleTokenUsage;
            }
        }

        public void OnDisable()
        {
            if (ServiceLocator.Get<DialogueManager>() != null)
            {
                ServiceLocator.Get<DialogueManager>().OnTokenReceived -= this.HandleToken;
                ServiceLocator.Get<DialogueManager>().OnDialogueComplete -= this.HandleComplete;
                ServiceLocator.Get<DialogueManager>().OnDialogueError -= this.HandleError;
                ServiceLocator.Get<DialogueManager>().OnTokenUsage -= this.HandleTokenUsage;
            }
        }

        public void Update()
        {
            // Esc 补位：输入框聚焦时全局 Esc 分发被 IsUIInputActive 守卫拦截
            //（GlobalInputProcessor 摸不到面板栈），此处按 Esc 走关闭按钮同款管线。
            // 守卫限定"本面板输入框聚焦"——失焦态由全局分发走 PanelController 栈的
            // OnClick_Back，避免同帧双 Close 把栈下层面板误弹掉；其他面板输入框
            //（如 LLMSettingPanel）聚焦时不误触。
            if (this.inputField != null &&
                UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject == this.inputField.gameObject &&
                Input.GetKeyDown(Constant.InputKeyConstant.CloseOrBuildMenu))
            {
                // 先消费本帧 Esc：OnBackClicked 的关闭会清输入框 selection（IsUIInputActive
                // 翻转），不消费的话同帧稍后的全局 Esc 分发会放行——栈已空 → 误开 BuildPanel
                // 挡住全部点击（表现为"关了对话后点什么都没反应，点下屏幕才恢复"）。
                UnityGlobalInputAdapter.ConsumeCloseMenuKey();
                this.OnBackClicked();
                return;
            }

            if (this.ShouldSubmitFromKeyboard())
            {
                this.OnSendClicked();
            }
        }

        /// <summary>
        /// 打开对话面板
        /// </summary>
        public void Open(string npcId, NPCPromptProfile profile)
        {
            this.activeNpcId = npcId;
            this.isWaitingResponse = false;
            this.currentStreamingText = null;

            this.gameObject.SetActive(true);
            this.FindUIElements();
            this.SetWaitingState(false);

            if (this.npcNameText != null && profile != null)
            {
                this.npcNameText.text = profile.npcName;
            }

            // 清空之前的聊天记录
            if (this.content != null)
            {
                for (int i = this.content.childCount - 1; i >= 0; i--)
                {
                    GameObject child = this.content.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
            }

            // 显示问候语
            string greeting = ServiceLocator.Get<DialogueManager>().GetGreeting(profile);
            this.AddNPCBubble(greeting);
            this.ScrollToLatest();

            if (this.deepThinkingToggle != null)
            {
                this.deepThinkingToggle.isOn = ModelSourceSettings.DeepThinkingEnabled;
            }

            if (this.inputField != null)
            {
                this.inputField.text = string.Empty;
                this.inputField.ActivateInputField();
            }

            // M3 包2.4：预设意图按钮（确定性结算 + LLM 增强措辞）
            this.EnsureIntentButtons();
            this.UpdateIntentButtonStates();
        }

        /// <summary>
        /// 关闭对话面板
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(this.activeNpcId))
            {
                ServiceLocator.Get<DialogueManager>().EndDialogue(this.activeNpcId);
            }

            // 显式释放输入焦点并清 EventSystem selection：直接 SetActive(false) 不会清
            // currentSelectedGameObject（Unity 不自动 deselect 失活对象），selection 残留
            // 指向失活的输入框 → IsUIInputActive() 持续 true → 所有带 !IsUIInputActive()
            // 守卫的输入（全局热键/点击处理）失效，直到玩家点一下屏幕（点击会改 selection）
            // 才恢复——即"Esc 关对话后点什么都没反应"症状。点关闭按钮无此问题：鼠标点击
            // 本身已把 selection 换成按钮。
            if (this.inputField != null && this.inputField.IsActive())
            {
                LogProvider(
                    $"[StateDiag] DialoguePanelUI.Close 清输入焦点: selection="
                    + UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.name,
                    LogManager.LogLevelEnum.Debug);

                this.inputField.DeactivateInputField();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            }

            this.activeNpcId = string.Empty;
            this.currentStreamingText = null;
            this.gameObject.SetActive(false);
        }

        private void OnSendClicked()
        {
            if (this.isWaitingResponse)
            {
                return;
            }

            string text = this.inputField != null ? this.inputField.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(this.activeNpcId))
            {
                return;
            }

            this.SetWaitingState(true);

            this.AddPlayerBubble(text);

            if (this.inputField != null)
            {
                this.inputField.text = string.Empty;
            }

            this.currentStreamingText = this.AddNPCBubble(string.Empty);
            if (this.currentStreamingText != null)
            {
                this.currentStreamingText.TypewriterSpeed = 0;
            }

            DialogueSession session = ServiceLocator.Get<DialogueManager>().GetSession(this.activeNpcId);
            if (session != null)
            {
                session.options.deepThinking = ModelSourceSettings.DeepThinkingEnabled;
            }

            ServiceLocator.Get<DialogueManager>().SendMessage(this.activeNpcId, text);
            this.ScrollToLatest();
        }

        private void OnInputEndEdit(string text)
        {
            if (this.IsSubmitKeyPressed())
            {
                this.OnSendClicked();
            }
        }

        private void OnBackClicked()
        {
            this.Close();
            ServiceLocator.Get<PanelController>().Close();
        }

        private void OnSettingsClicked()
        {
            ServiceLocator.Get<PanelController>().Show(LLMSettingPanel.Instance);
        }

        private void OnDeepThinkingChanged(bool isOn)
        {
            ModelSourceSettings.DeepThinkingEnabled = isOn;
            ModelSourceSettings.Save();

            DialogueSession session = ServiceLocator.Get<DialogueManager>().GetSession(this.activeNpcId);
            if (session != null)
            {
                session.options.deepThinking = isOn;
            }
        }

        /// <summary>
        /// 构建预设意图按钮行（M3 包2.4，纯代码——面板为场景实例无法手摆子物体）。
        /// 挂在面板根底部、输入框上方；位置按 Message 顶边推算，实测可调。
        /// </summary>
        private void EnsureIntentButtons()
        {
            if (this.intentButtons.Count > 0)
            {
                return;
            }

            RectTransform panelRect = this.transform as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            // 挂 Inset 中间层（四边内缩 24px 的内容区，BottomBar 也在其下、底边基准一致），
            // 避免压 9-slice 边框；无 Inset 时退回面板根
            RectTransform contentRoot = panelRect.Find("Inset") as RectTransform ?? panelRect;

            GameObject rowGo = new GameObject("IntentButtonRow", typeof(RectTransform));
            rowGo.transform.SetParent(contentRoot, false);
            RectTransform rowRect = (RectTransform)rowGo.transform;
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0.5f, 0f);
            rowRect.anchoredPosition = new Vector2(0f, this.ComputeIntentRowY());
            rowRect.sizeDelta = new Vector2(-32f, 34f);

            HorizontalLayoutGroup layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            foreach (DialogueIntentKind kind in DialogueIntentRuleService.AllKinds)
            {
                this.CreateIntentButton(rowGo.transform, kind);
            }
        }

        private void CreateIntentButton(Transform parent, DialogueIntentKind kind)
        {
            GameObject go = new GameObject("Intent_" + kind, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.30f, 0.42f, 0.62f, 0.92f);

            Button btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => this.OnIntentClicked(kind));

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Text label = textGo.AddComponent<Text>();
            label.font = PixelUITheme.PixelFont; // 铁律③：中文标签禁内置字体（fallback 系统字体毁点阵）
            label.text = DialogueIntentRuleService.GetDisplayName(kind)
                + (kind == DialogueIntentKind.Gift ? "(" + DialogueIntentRuleService.GiftCoinCost + "金)" : string.Empty);
            label.fontSize = 12;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;

            this.intentButtons[kind] = btn;
        }

        /// <summary>意图按钮行的 y 坐标：Message 输入框顶边 + 6px 间距（anchor 同为底部时成立，兜底 46）。</summary>
        private float ComputeIntentRowY()
        {
            if (this.inputField != null)
            {
                RectTransform messageRect = this.inputField.GetComponent<RectTransform>();
                if (messageRect != null && messageRect.rect.height > 0f)
                {
                    return messageRect.anchoredPosition.y + messageRect.rect.height * (1f - messageRect.pivot.y) + 6f;
                }
            }

            return 46f;
        }

        /// <summary>按日限/金币/等待中刷新意图按钮可用性（面板未开或按钮未建时静默跳过）。</summary>
        private void UpdateIntentButtonStates()
        {
            if (this.intentButtons.Count == 0 || string.IsNullOrEmpty(this.activeNpcId))
            {
                return;
            }

            DialogueManager dm = ServiceLocator.Get<DialogueManager>();
            AWorker worker = dm != null ? dm.TryGetDialogueWorker(this.activeNpcId) : null;
            AWorker.WorkerData wd = worker?.CharacterDataLAB as AWorker.WorkerData;
            WorkerMindService mindService = ServiceLocator.Get<WorkerMindService>();
            CurrencyManager cm = ServiceLocator.Get<CurrencyManager>();
            bool coinsEnough = cm != null
                && cm.GetPlayerBalance().Gold >= DialogueIntentRuleService.GiftCoinCost;

            foreach (KeyValuePair<DialogueIntentKind, Button> kv in this.intentButtons)
            {
                bool usable = worker != null && wd != null && mindService != null
                    && !this.isWaitingResponse
                    && mindService.GetIntentUseCountToday(wd, kv.Key.ToString())
                        < DialogueIntentRuleService.GetDailyCap(kv.Key)
                    && (kv.Key != DialogueIntentKind.Gift || coinsEnough);
                kv.Value.interactable = usable;
            }
        }

        /// <summary>
        /// 预设意图点击：DialogueManager.ApplyIntent 本地确定性结算 → 玩家气泡显示短句 →
        /// PlayerActionText 走 SendMessage 由 LLM 增强 NPC 回复措辞（与自由输入共用流式管线）。
        /// 结算不可用（日限/金币/会话失效）时 ApplyIntent 返回 null，仅刷新按钮状态。
        /// </summary>
        private void OnIntentClicked(DialogueIntentKind kind)
        {
            if (this.isWaitingResponse || string.IsNullOrEmpty(this.activeNpcId))
            {
                return;
            }

            DialogueManager dm = ServiceLocator.Get<DialogueManager>();
            if (dm == null)
            {
                return;
            }

            DialogueIntentResult result = dm.ApplyIntent(this.activeNpcId, kind);
            if (result == null)
            {
                this.UpdateIntentButtonStates();
                return;
            }

            this.SetWaitingState(true);

            this.AddPlayerBubble(result.PlayerDisplayText);

            this.currentStreamingText = this.AddNPCBubble(string.Empty);
            if (this.currentStreamingText != null)
            {
                this.currentStreamingText.TypewriterSpeed = 0;
            }

            DialogueSession session = dm.GetSession(this.activeNpcId);
            if (session != null)
            {
                session.options.deepThinking = ModelSourceSettings.DeepThinkingEnabled;
            }

            dm.SendMessage(this.activeNpcId, result.PlayerActionText);
            this.ScrollToLatest();
        }

        private void HandleToken(string npcId, string token)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.currentStreamingText?.AppendToken(token);
            this.ScrollToLatest();
        }

        private void HandleComplete(string npcId, string fullResponse)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.SetWaitingState(false);
            this.currentStreamingText = null;
            this.ScrollToLatest();
        }

        private void HandleError(string npcId, string error)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.SetWaitingState(false);
            this.currentStreamingText?.AppendToken("\n[错误: " + error + "]");
            this.currentStreamingText = null;
            this.ScrollToLatest();
        }

        private void HandleTokenUsage(string npcId, TokenUsageInfo usage)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.currentTokenUsage = usage;
            this.UpdateTokenUsageDisplay();
        }

        private void UpdateTokenUsageDisplay()
        {
            if (this.tokenUsageText == null)
            {
                return;
            }

            if (this.currentTokenUsage.totalTokens <= 0)
            {
                this.tokenUsageText.text = string.Empty;
                return;
            }

            var u = this.currentTokenUsage;
            if (u.reasoningTokens > 0)
            {
                this.tokenUsageText.text = string.Format(
                    "词元 输入:{0}  输出:{1}  推理:{2}  可见:{3}",
                    FormatTokenCount(u.promptTokens),
                    FormatTokenCount(u.completionTokens),
                    FormatTokenCount(u.reasoningTokens),
                    FormatTokenCount(u.visibleOutputTokens));
            }
            else
            {
                this.tokenUsageText.text = string.Format(
                    "词元 输入:{0}  输出:{1}",
                    FormatTokenCount(u.promptTokens),
                    FormatTokenCount(u.completionTokens));
            }
        }

        private static string FormatTokenCount(int count)
        {
            if (count >= 1000)
            {
                return (count / 1000.0).ToString("0.#") + "K";
            }

            return count.ToString();
        }

        private void SetWaitingState(bool waiting)
        {
            this.isWaitingResponse = waiting;

            if (this.sendButton != null)
            {
                this.sendButton.interactable = !waiting;
            }

            if (this.inputField != null)
            {
                this.inputField.interactable = !waiting;
                if (!waiting && this.gameObject.activeInHierarchy)
                {
                    this.inputField.ActivateInputField();
                }
            }

            this.UpdateIntentButtonStates();
        }

        private void FindUIElements()
        {
            if (this.npcNameText == null)
            {
                this.npcNameText = FindChildComponent<Text>(this.gameObject, "NpcName");
            }

            if (this.tokenUsageText == null)
            {
                this.tokenUsageText = FindChildComponent<Text>(this.gameObject, "TokenUsage");
            }

            if (this.inputField == null)
            {
                this.inputField = FindChildComponent<InputField>(this.gameObject, "Message");
                if (this.inputField != null)
                {
                    this.inputField.onEndEdit.RemoveListener(this.OnInputEndEdit);
                    this.inputField.onEndEdit.AddListener(this.OnInputEndEdit);
                }
            }

            if (this.sendButton == null)
            {
                this.sendButton = FindChildComponent<Button>(this.gameObject, "Send");
                if (this.sendButton != null)
                {
                    this.sendButton.onClick.RemoveAllListeners();
                    this.sendButton.onClick.AddListener(this.OnSendClicked);
                }
            }

            if (this.backButton == null)
            {
                this.backButton = FindChildComponent<Button>(this.gameObject, "Back");
                if (this.backButton != null)
                {
                    this.backButton.onClick.RemoveAllListeners();
                    this.backButton.onClick.AddListener(this.OnBackClicked);
                }
            }

            if (this.settingsButton == null)
            {
                this.settingsButton = FindChildComponent<Button>(this.gameObject, "Settings");
                if (this.settingsButton != null)
                {
                    this.settingsButton.onClick.RemoveAllListeners();
                    this.settingsButton.onClick.AddListener(this.OnSettingsClicked);
                }
            }

            if (this.deepThinkingToggle == null)
            {
                this.deepThinkingToggle = FindChildComponent<Toggle>(this.gameObject, "DeepThinking");
                if (this.deepThinkingToggle != null)
                {
                    this.deepThinkingToggle.onValueChanged.RemoveAllListeners();
                    this.deepThinkingToggle.onValueChanged.AddListener(this.OnDeepThinkingChanged);
                }
            }

            if (this.content == null)
            {
                this.content = FindChildComponent<Transform>(this.gameObject, "Content");
            }

            if (this.scrollRect == null)
            {
                this.scrollRect = FindChildComponent<ScrollRect>(this.gameObject, "ScrollView");
            }
        }

        private bool ShouldSubmitFromKeyboard()
        {
            return this.inputField != null &&
                this.inputField.isFocused &&
                this.IsSubmitKeyPressed();
        }

        private bool IsSubmitKeyPressed()
        {
            return UnityGlobalInputAdapter.GetDialogueSubmitDown();
        }

        private void AddPlayerBubble(string text)
        {
            if (this.content == null)
            {
                return;
            }

            GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(
                PrefabConstant.RIGHT_CHAT_ITEM, this.content, false);
            if (g == null)
            {
                return;
            }

            Text t = FindChildComponent<Text>(g, "Text");
            if (t != null)
            {
                t.text = text;
            }

            this.ConfigureBubble(g, t, true);
            this.ScrollToLatest();
        }

        private StreamingTextView AddNPCBubble(string initialText)
        {
            if (this.content == null)
            {
                return null;
            }

            GameObject g = ServiceLocator.Get<ResourceManager>().Instantiate(
                PrefabConstant.LEFT_CHAT_ITEM, this.content, false);
            if (g == null)
            {
                return null;
            }

            Text textComponent = FindChildComponent<Text>(g, "Text");
            if (textComponent != null)
            {
                textComponent.text = initialText ?? string.Empty;
            }

            LayoutElement bubbleLayoutElement = this.ConfigureBubble(g, textComponent, false);

            StreamingTextView streaming = g.GetComponent<StreamingTextView>();
            if (streaming == null)
            {
                streaming = g.AddComponent<StreamingTextView>();
            }

            LayoutElement textLayoutElement = textComponent != null ? textComponent.GetComponent<LayoutElement>() : null;
            RectTransform textRect = textComponent != null ? textComponent.GetComponent<RectTransform>() : null;
            streaming.ConfigureLayout(
                bubbleLayoutElement,
                textLayoutElement,
                textRect,
                this.content as RectTransform,
                BubbleMinWidth,
                this.GetBubbleMaxWidth(),
                BubbleHorizontalPadding);
            streaming.Clear();
            if (!string.IsNullOrEmpty(initialText))
            {
                streaming.AppendToken(initialText);
            }

            this.ScrollToLatest();
            return streaming;
        }

        private LayoutElement ConfigureBubble(GameObject item, Text textComponent, bool isPlayer)
        {
            if (item == null)
            {
                return null;
            }

            RectTransform rowRect = item.GetComponent<RectTransform>();
            if (rowRect != null)
            {
                rowRect.anchorMin = new Vector2(0, 1);
                rowRect.anchorMax = new Vector2(1, 1);
                rowRect.pivot = new Vector2(0.5f, 1);
                rowRect.anchoredPosition = Vector2.zero;
                rowRect.sizeDelta = new Vector2(0, 0);
            }

            HorizontalOrVerticalLayoutGroup rowLayout = GetOrAddHorizontalOrVerticalLayoutGroup(item);

            if (rowLayout == null)
            {
                return null;
            }

            rowLayout.padding = new RectOffset(12, 12, 4, 4);
            rowLayout.childAlignment = isPlayer ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            rowLayout.spacing = 0;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childScaleWidth = false;
            rowLayout.childScaleHeight = false;

            LayoutElement rowLayoutElement = item.GetComponent<LayoutElement>();
            if (rowLayoutElement == null)
            {
                rowLayoutElement = item.AddComponent<LayoutElement>();
            }

            rowLayoutElement.minHeight = 44f;
            rowLayoutElement.flexibleWidth = 1f;

            Transform bubble = FindChildTransform(item.transform, "Image");
            if (bubble == null)
            {
                return null;
            }

            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.anchorMin = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.anchorMax = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.pivot = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.anchoredPosition = Vector2.zero;
            }

            HorizontalOrVerticalLayoutGroup bubbleLayout = GetOrAddHorizontalOrVerticalLayoutGroup(bubble.gameObject);

            if (bubbleLayout == null)
            {
                return null;
            }

            bubbleLayout.padding = new RectOffset(16, 16, 10, 10);
            bubbleLayout.childAlignment = TextAnchor.MiddleLeft;
            bubbleLayout.spacing = 0;
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = false;
            bubbleLayout.childForceExpandHeight = false;
            bubbleLayout.childScaleWidth = false;
            bubbleLayout.childScaleHeight = false;

            ContentSizeFitter bubbleFitter = bubble.GetComponent<ContentSizeFitter>();
            if (bubbleFitter == null)
            {
                bubbleFitter = bubble.gameObject.AddComponent<ContentSizeFitter>();
            }

            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RoundCorner bubbleGraphic = bubble.GetComponent<RoundCorner>();
            if (bubbleGraphic != null)
            {
                bubbleGraphic.color = isPlayer
                    ? new Color(0.42f, 0.70f, 0.98f, 1f)
                    : new Color(0.96f, 0.94f, 0.88f, 1f);
                bubbleGraphic.Radius = 0.12f;
            }
            else
            {
                Image bubbleImage = bubble.GetComponent<Image>();
                if (bubbleImage != null)
                {
                    bubbleImage.color = isPlayer
                        ? new Color(0.42f, 0.70f, 0.98f, 1f)
                        : new Color(0.96f, 0.94f, 0.88f, 1f);
                }
            }

            float bubbleWidth = this.CalculateBubbleWidth(textComponent);
            LayoutElement bubbleLayoutElement = bubble.GetComponent<LayoutElement>();
            if (bubbleLayoutElement == null)
            {
                bubbleLayoutElement = bubble.gameObject.AddComponent<LayoutElement>();
            }

            bubbleLayoutElement.minWidth = BubbleMinWidth;
            bubbleLayoutElement.preferredWidth = bubbleWidth;
            bubbleLayoutElement.minHeight = 40f;
            bubbleLayoutElement.flexibleWidth = 0f;
            bubbleLayoutElement.flexibleHeight = 0f;

            if (textComponent != null)
            {
                textComponent.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                textComponent.fontSize = 24;
                textComponent.alignment = TextAnchor.MiddleLeft;
                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                textComponent.lineSpacing = 1.05f;

                LayoutElement textLayoutElement = textComponent.GetComponent<LayoutElement>();
                if (textLayoutElement == null)
                {
                    textLayoutElement = textComponent.gameObject.AddComponent<LayoutElement>();
                }

                float textWidth = System.Math.Max(1f, bubbleWidth - BubbleHorizontalPadding);
                textLayoutElement.minWidth = textWidth;
                textLayoutElement.preferredWidth = textWidth;
                textLayoutElement.flexibleWidth = 0f;

                ContentSizeFitter textFitter = textComponent.GetComponent<ContentSizeFitter>();
                if (textFitter == null)
                {
                    textFitter = textComponent.gameObject.AddComponent<ContentSizeFitter>();
                }

                textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return bubbleLayoutElement;
        }

        private static HorizontalOrVerticalLayoutGroup GetOrAddHorizontalOrVerticalLayoutGroup(GameObject target)
        {
            HorizontalOrVerticalLayoutGroup layout = target.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layout != null)
            {
                return layout;
            }

            if (target.GetComponent<LayoutGroup>() != null)
            {
                return null;
            }

            return target.AddComponent<HorizontalLayoutGroup>();
        }

        private float CalculateBubbleWidth(Text textComponent)
        {
            if (textComponent == null)
            {
                return BubbleMinWidth;
            }

            float preferredTextWidth = string.IsNullOrEmpty(textComponent.text)
                ? 0f
                : textComponent.preferredWidth;
            return MathHelper.Clamp(preferredTextWidth + BubbleHorizontalPadding, BubbleMinWidth, this.GetBubbleMaxWidth());
        }

        private float GetBubbleMaxWidth()
        {
            RectTransform contentRect = this.content as RectTransform;
            if (contentRect == null || contentRect.rect.width <= 0)
            {
                return BubbleMaxWidth;
            }

            return MathHelper.Clamp(contentRect.rect.width * 0.72f, 280f, BubbleMaxWidth);
        }

        private void ScrollToLatest()
        {
            if (this.content is RectTransform contentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            if (this.scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                this.scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private static T FindChildComponent<T>(GameObject parent, string name)
            where T : Component
        {
            if (parent == null)
            {
                return null;
            }

            T[] components = parent.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component.name.Trim().Equals(name))
                {
                    return component;
                }
            }

            return null;
        }

        private static Transform FindChildTransform(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (child.name.Trim().Equals(name))
                {
                    return child;
                }
            }
            return null;
        }
    }
}
