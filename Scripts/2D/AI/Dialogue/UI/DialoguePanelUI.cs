namespace LAB2D.AI.Dialogue.UI
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.UI.Panel;
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
        private string activeNpcId;
        private StreamingTextView currentStreamingText;
        private bool isWaitingResponse;

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

            LogManager.Instance?.Log(
                "DialoguePanelUI: scene instance is missing. Add DialoguePanelUI to Game.unity.",
                LogManager.LogLevelEnum.Error);
            return null;
        }

        public void Awake()
        {
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
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnTokenReceived += this.HandleToken;
                DialogueManager.Instance.OnDialogueComplete += this.HandleComplete;
                DialogueManager.Instance.OnDialogueError += this.HandleError;
            }
        }

        public void OnDisable()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnTokenReceived -= this.HandleToken;
                DialogueManager.Instance.OnDialogueComplete -= this.HandleComplete;
                DialogueManager.Instance.OnDialogueError -= this.HandleError;
            }
        }

        public void Update()
        {
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
            string greeting = DialogueManager.Instance.GetGreeting(profile);
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
        }

        /// <summary>
        /// 关闭对话面板
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(this.activeNpcId))
            {
                DialogueManager.Instance.EndDialogue(this.activeNpcId);
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

            DialogueSession session = DialogueManager.Instance.GetSession(this.activeNpcId);
            if (session != null)
            {
                session.options.deepThinking = ModelSourceSettings.DeepThinkingEnabled;
            }

            DialogueManager.Instance.SendMessage(this.activeNpcId, text);
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
            PanelController.Instance.Close();
        }

        private void OnSettingsClicked()
        {
            PanelController.Instance.Show(LLMSettingsPanel.Instance);
        }

        private void OnDeepThinkingChanged(bool isOn)
        {
            ModelSourceSettings.DeepThinkingEnabled = isOn;
            ModelSourceSettings.Save();

            DialogueSession session = DialogueManager.Instance.GetSession(this.activeNpcId);
            if (session != null)
            {
                session.options.deepThinking = isOn;
            }
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
        }

        private void FindUIElements()
        {
            if (this.npcNameText == null)
            {
                this.npcNameText = FindChildComponent<Text>(this.gameObject, "NpcName");
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
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        }

        private void AddPlayerBubble(string text)
        {
            if (this.content == null || ResourceManager.Instance == null)
            {
                return;
            }

            GameObject g = ResourceManager.Instance.Instantiate(
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
            if (this.content == null || ResourceManager.Instance == null)
            {
                return null;
            }

            GameObject g = ResourceManager.Instance.Instantiate(
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
                textComponent.fontSize = 20;
                textComponent.alignment = TextAnchor.MiddleLeft;
                textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                textComponent.lineSpacing = 1.05f;

                LayoutElement textLayoutElement = textComponent.GetComponent<LayoutElement>();
                if (textLayoutElement == null)
                {
                    textLayoutElement = textComponent.gameObject.AddComponent<LayoutElement>();
                }

                float textWidth = Mathf.Max(1f, bubbleWidth - BubbleHorizontalPadding);
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
            return Mathf.Clamp(preferredTextWidth + BubbleHorizontalPadding, BubbleMinWidth, this.GetBubbleMaxWidth());
        }

        private float GetBubbleMaxWidth()
        {
            RectTransform contentRect = this.content as RectTransform;
            if (contentRect == null || contentRect.rect.width <= 0)
            {
                return BubbleMaxWidth;
            }

            return Mathf.Clamp(contentRect.rect.width * 0.72f, 280f, BubbleMaxWidth);
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
