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

        private Transform content;
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

            this.FindUIElements();

            if (this.npcNameText != null && profile != null)
            {
                this.npcNameText.text = profile.npcName;
            }

            // 清空之前的聊天记录
            if (this.content != null)
            {
                for (int i = this.content.childCount - 1; i >= 0; i--)
                {
                    Destroy(this.content.GetChild(i).gameObject);
                }
            }

            // 显示问候语
            string greeting = DialogueManager.Instance.GetGreeting(profile);
            this.AddNPCBubble(greeting);

            this.gameObject.SetActive(true);

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

            string text = this.inputField != null ? this.inputField.text : string.Empty;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(this.activeNpcId))
            {
                return;
            }

            this.isWaitingResponse = true;

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
        }

        private void HandleComplete(string npcId, string fullResponse)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.isWaitingResponse = false;
            this.currentStreamingText = null;
        }

        private void HandleError(string npcId, string error)
        {
            if (npcId != this.activeNpcId)
            {
                return;
            }

            this.isWaitingResponse = false;
            this.currentStreamingText?.AppendToken("\n[错误: " + error + "]");
            this.currentStreamingText = null;
        }

        private void FindUIElements()
        {
            if (this.npcNameText == null)
            {
                this.npcNameText = Tool.GetComponentInChildren<Text>(this.gameObject, "NpcName");
            }

            if (this.inputField == null)
            {
                this.inputField = Tool.GetComponentInChildren<InputField>(this.gameObject, "Message");
            }

            if (this.sendButton == null)
            {
                this.sendButton = Tool.GetComponentInChildren<Button>(this.gameObject, "Send");
                if (this.sendButton != null)
                {
                    this.sendButton.onClick.RemoveAllListeners();
                    this.sendButton.onClick.AddListener(this.OnSendClicked);
                }
            }

            if (this.backButton == null)
            {
                this.backButton = Tool.GetComponentInChildren<Button>(this.gameObject, "Back");
                if (this.backButton != null)
                {
                    this.backButton.onClick.RemoveAllListeners();
                    this.backButton.onClick.AddListener(this.OnBackClicked);
                }
            }

            if (this.content == null)
            {
                this.content = Tool.GetComponentInChildren<Transform>(this.gameObject, "Content");
            }
        }

        private void AddPlayerBubble(string text)
        {
            if (this.content == null)
            {
                return;
            }

            GameObject g = ResourceManager.Instance.Instantiate(
                PrefabConstant.RIGHT_CHAT_ITEM, this.content, false);
            Text t = Tool.GetComponentInChildren<Text>(g, "Text");
            if (t != null)
            {
                t.text = text;
            }
        }

        private StreamingTextView AddNPCBubble(string initialText)
        {
            if (this.content == null)
            {
                return null;
            }

            GameObject g = ResourceManager.Instance.Instantiate(
                PrefabConstant.LEFT_CHAT_ITEM, this.content, false);

            Text textComponent = Tool.GetComponentInChildren<Text>(g, "Text");
            if (textComponent != null)
            {
                textComponent.text = initialText ?? string.Empty;
            }

            StreamingTextView streaming = g.GetComponent<StreamingTextView>();
            if (streaming == null)
            {
                streaming = g.AddComponent<StreamingTextView>();
            }

            streaming.Clear();
            if (!string.IsNullOrEmpty(initialText))
            {
                streaming.AppendToken(initialText);
            }

            return streaming;
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
            panelRt.anchorMax = new Vector2(1, 0.33f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // 半透明背景
            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            DialoguePanelUI ui = panelGo.AddComponent<DialoguePanelUI>();

            // NpcName 文本
            GameObject nameGo = CreateUIChild(panelRt, "NpcName");
            RectTransform nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.85f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.pivot = new Vector2(0.5f, 1);
            nameRt.anchoredPosition = new Vector2(0, -3);
            nameRt.sizeDelta = new Vector2(0, 22);
            Text nameText = nameGo.AddComponent<Text>();
            nameText.text = "NPC";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontSize = 20;

            // 聊天内容区域（ScrollView）
            GameObject scrollGo = CreateUIChild(panelRt, "ScrollView");
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0.2f);
            scrollRt.anchorMax = new Vector2(1, 0.85f);
            scrollRt.offsetMin = new Vector2(5, 0);
            scrollRt.offsetMax = new Vector2(-5, 0);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
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
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // 底部输入区域
            GameObject bottomGo = CreateUIChild(panelRt, "BottomBar");
            RectTransform bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0.18f);
            bottomRt.offsetMin = new Vector2(3, 3);
            bottomRt.offsetMax = new Vector2(-3, -3);
            Image bottomBg = bottomGo.AddComponent<Image>();
            bottomBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            // InputField
            GameObject inputGo = CreateUIChild(bottomRt, "Message");
            RectTransform inputRt = inputGo.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = new Vector2(0.7f, 1);
            inputRt.offsetMin = new Vector2(5, 5);
            inputRt.offsetMax = new Vector2(-5, -5);
            Image inputBg = inputGo.AddComponent<Image>();
            inputBg.color = Color.white;
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
            sendRt.anchorMin = new Vector2(0.72f, 0);
            sendRt.anchorMax = new Vector2(0.86f, 1);
            sendRt.offsetMin = new Vector2(0, 5);
            sendRt.offsetMax = new Vector2(0, -5);
            Image sendBg = sendGo.AddComponent<Image>();
            sendBg.color = new Color(0.2f, 0.6f, 0.2f);
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
            backRt.anchorMin = new Vector2(0.87f, 0);
            backRt.anchorMax = new Vector2(1, 1);
            backRt.offsetMin = new Vector2(0, 5);
            backRt.offsetMax = new Vector2(-2, -5);
            Image backBg = backGo.AddComponent<Image>();
            backBg.color = new Color(0.5f, 0.2f, 0.2f);
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
        }

        private static GameObject CreateUIChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
