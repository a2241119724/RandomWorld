namespace LAB2D
{
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
        private Text npcNameText;
        private string activeNpcId;
        private StreamingTextView currentStreamingText;
        private bool isWaitingResponse;

        /// <summary>
        /// 确保 UI 实例存在（场景无预制体时自动创建）
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

            // 动态创建 UI 层级
            CreateUIGameObject();
            return Instance;
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

            DialogueManager.Instance.SendMessage(this.activeNpcId, text);
            this.ScrollToLatest();
        }

        private void OnBackClicked()
        {
            this.Close();
            PanelController.Instance.Close();
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

            if (this.content == null)
            {
                this.content = FindChildComponent<Transform>(this.gameObject, "Content");
            }

            if (this.scrollRect == null)
            {
                this.scrollRect = FindChildComponent<ScrollRect>(this.gameObject, "ScrollView");
            }
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

            HorizontalLayoutGroup rowLayout = item.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout == null)
            {
                rowLayout = item.AddComponent<HorizontalLayoutGroup>();
            }

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
                // 如果 prefab 中没有 "Image" 子节点，动态创建
                GameObject bubbleGo = new GameObject("Image", typeof(RectTransform));
                bubbleGo.transform.SetParent(item.transform, false);
                bubbleGo.AddComponent<Image>();
                bubble = bubbleGo.transform;
            }

            RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.anchorMin = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.anchorMax = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.pivot = new Vector2(isPlayer ? 1 : 0, 1);
                bubbleRect.anchoredPosition = Vector2.zero;
            }

            HorizontalLayoutGroup bubbleLayout = bubble.GetComponent<HorizontalLayoutGroup>();
            if (bubbleLayout == null)
            {
                bubbleLayout = bubble.gameObject.AddComponent<HorizontalLayoutGroup>();
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

        private static void CreateUIGameObject()
        {
            // 找到 UI Canvas
            Transform parent = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
            if (parent == null)
            {
                LogManager.Instance.Log("DialoguePanelUI: 未找到 UI Canvas", LogManager.LogLevelEnum.Error);
                return;
            }

            // 创建主面板（仅占屏幕下方 1/3）
            GameObject panelGo = new GameObject("DialoguePanelUI", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0, 0);
            panelRt.anchorMax = new Vector2(1, 0.42f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // 半透明背景
            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.07f, 0.92f);

            // NpcName 文本
            GameObject nameGo = CreateUIChild(panelRt, "NpcName");
            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.86f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0.5f, 1);
            nameRt.offsetMin = new Vector2(18, 0);
            nameRt.offsetMax = new Vector2(-18, -2);
            Text nameText = nameGo.AddComponent<Text>();
            nameText.text = "NPC";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.color = new Color(0.94f, 0.96f, 0.98f, 1f);
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.fontSize = 18;

            // 聊天内容区域（ScrollView）
            GameObject scrollGo = CreateUIChild(panelRt, "ScrollView");
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0.22f);
            scrollRt.anchorMax = new Vector2(1, 0.86f);
            scrollRt.offsetMin = new Vector2(12, 4);
            scrollRt.offsetMax = new Vector2(-12, -4);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0.09f, 0.10f, 0.12f, 0.72f);
            Mask scrollMask = scrollGo.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false;

            // Content
            GameObject contentGo = CreateUIChild(scrollRt, "Content");
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 8;
            layout.padding = new RectOffset(8, 8, 8, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 底部输入区域
            GameObject bottomGo = CreateUIChild(panelRt, "BottomBar");
            RectTransform bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0.20f);
            bottomRt.offsetMin = new Vector2(12, 8);
            bottomRt.offsetMax = new Vector2(-12, -8);
            Image bottomBg = bottomGo.AddComponent<Image>();
            bottomBg.color = new Color(0.11f, 0.12f, 0.14f, 0.94f);

            // InputField
            GameObject inputGo = CreateUIChild(bottomRt, "Message");
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = new Vector2(0.74f, 1);
            inputRt.offsetMin = new Vector2(8, 6);
            inputRt.offsetMax = new Vector2(-6, -6);
            Image inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.95f, 0.96f, 0.94f, 1f);
            InputField inputField = inputGo.AddComponent<InputField>();
            GameObject inputTextGo = CreateUIChild(inputRt, "Text");
            RectTransform inputTextRt = inputTextGo.GetComponent<RectTransform>();
            inputTextRt.anchorMin = Vector2.zero;
            inputTextRt.anchorMax = Vector2.one;
            inputTextRt.offsetMin = new Vector2(5, 2);
            inputTextRt.offsetMax = new Vector2(-5, -2);
            Text inputText = inputTextGo.AddComponent<Text>();
            inputText.text = string.Empty;
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 16;
            inputText.color = Color.black;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;
            inputField.textComponent = inputText;
            GameObject placeholderGo = CreateUIChild(inputRt, "Placeholder");
            RectTransform placeholderRt = placeholderGo.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(5, 2);
            placeholderRt.offsetMax = new Vector2(-5, -2);
            Text placeholderText = placeholderGo.AddComponent<Text>();
            placeholderText.text = "输入对话...";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 16;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            inputField.placeholder = placeholderText;

            // Send 按钮
            GameObject sendGo = CreateUIChild(bottomRt, "Send");
            RectTransform sendRt = sendGo.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(0.76f, 0);
            sendRt.anchorMax = new Vector2(0.88f, 1);
            sendRt.offsetMin = new Vector2(0, 6);
            sendRt.offsetMax = new Vector2(0, -6);
            Image sendBg = sendGo.AddComponent<Image>();
            sendBg.color = new Color(0.22f, 0.55f, 0.78f, 1f);
            Button sendBtn = sendGo.AddComponent<Button>();
            GameObject sendTextGo = CreateUIChild(sendRt, "Text");
            RectTransform sendTextRt = sendTextGo.GetComponent<RectTransform>();
            sendTextRt.anchorMin = Vector2.zero;
            sendTextRt.anchorMax = Vector2.one;
            Text sendLabel = sendTextGo.AddComponent<Text>();
            sendLabel.text = "发送";
            sendLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sendLabel.fontSize = 14;
            sendLabel.color = Color.white;
            sendLabel.alignment = TextAnchor.MiddleCenter;

            // Back 按钮
            GameObject backGo = CreateUIChild(bottomRt, "Back");
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.895f, 0);
            backRt.anchorMax = new Vector2(1, 1);
            backRt.offsetMin = new Vector2(0, 6);
            backRt.offsetMax = new Vector2(-2, -6);
            Image backBg = backGo.AddComponent<Image>();
            backBg.color = new Color(0.34f, 0.34f, 0.38f, 1f);
            Button backBtn = backGo.AddComponent<Button>();
            GameObject backTextGo = CreateUIChild(backRt, "Text");
            RectTransform backTextRt = backTextGo.GetComponent<RectTransform>();
            backTextRt.anchorMin = Vector2.zero;
            backTextRt.anchorMax = Vector2.one;
            Text backLabel = backTextGo.AddComponent<Text>();
            backLabel.text = "关闭";
            backLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backLabel.fontSize = 14;
            backLabel.color = Color.white;
            backLabel.alignment = TextAnchor.MiddleCenter;

            panelGo.AddComponent<DialoguePanelUI>();
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

        private static GameObject CreateUIChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
