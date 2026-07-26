namespace LAB2D.UI
{
      using LAB2D;
    using LAB2D.Core;
      using LAB2D.Domain.Common;
      using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    /// <summary>
    /// A004 波间奖励面板。
    /// 可由 Editor 菜单安装到 Game.unity，也可在运行时出现奖励时自动创建独立 Canvas。
    /// 本脚本只读取 WaveBossRewardManager 的运行时状态，不写存档、不修改资源、不参与 Photon 同步。
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveBossRewardPanel : MonoBehaviour
    {
        private static bool runtimeFallbackRegistered;

        private readonly List<Button> optionButtons = new List<Button>(WaveBossRewardConstant.RewardOptionCount);
        private readonly List<Text> optionTexts = new List<Text>(WaveBossRewardConstant.RewardOptionCount);
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>标题文本。</summary>
        public Text titleText;

        /// <summary>状态摘要文本。</summary>
        public Text summaryText;

        /// <summary>面板刷新间隔。</summary>
        public float refreshInterval = WaveBossRewardConstant.PanelRefreshInterval;

        /// <summary>
        /// 运行时注册兜底 UI。
        /// 当奖励选项出现而场景没有预置面板时，自动创建独立 Canvas，保证功能有可见入口。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterRuntimeFallback()
        {
            if (runtimeFallbackRegistered)
            {
                return;
            }

            runtimeFallbackRegistered = true;
            Core.ServiceLocator.Get<WaveBossRewardManager>().OnRewardOptionsChanged -= HandleRuntimeRewardOptionsChanged;
            Core.ServiceLocator.Get<WaveBossRewardManager>().OnRewardOptionsChanged += HandleRuntimeRewardOptionsChanged;
        }

        /// <summary>
        /// 确保运行时奖励面板存在。
        /// </summary>
        /// <returns>奖励面板组件。</returns>
        public static WaveBossRewardPanel EnsureRuntimePanel()
        {
            WaveBossRewardPanel panel = FindObjectOfType<WaveBossRewardPanel>();
            if (panel != null)
            {
                return panel;
            }

            Canvas canvas = FindOrCreateCanvas();
            GameObject root = CreatePanelRoot(canvas.transform);
            panel = root.GetComponent<WaveBossRewardPanel>();
            panel.UpdateFromManager();
            return panel;
        }

        /// <summary>
        /// 创建完整面板根节点。
        /// Editor 菜单和运行时兜底共用同一套生成逻辑，避免 UI 结构重复实现。
        /// </summary>
        /// <param name="parent">父 Canvas。</param>
        /// <returns>面板根对象。</returns>
        public static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject root = new GameObject(
                WaveBossRewardConstant.PanelRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(WaveBossRewardPanel));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0.0f, 0.0f);
            rootRect.sizeDelta = new Vector2(560.0f, 360.0f);

            Image background = root.GetComponent<Image>();
            background.color = PixelUITheme.DialogBoxBg;

            WaveBossRewardPanel panel = root.GetComponent<WaveBossRewardPanel>();
            panel.BuildLayoutIfNeeded();
            panel.SetVisible(false);
            return root;
        }

        private void Awake()
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.BuildLayoutIfNeeded();
        }

        private void OnEnable()
        {
            try
            {
                WaveBossRewardManager manager = Core.ServiceLocator.TryGet(out WaveBossRewardManager mgr) ? mgr : ServiceLocator.Get<WaveBossRewardManager>();
                manager.OnRewardOptionsChanged += this.HandleRewardOptionsChanged;
                manager.OnStateChanged += this.HandleStateChanged;
                this.UpdateFromManager();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WaveBossRewardPanel] 绑定奖励数据失败: " + exception.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                WaveBossRewardManager manager = Core.ServiceLocator.TryGet(out WaveBossRewardManager mgr) ? mgr : ServiceLocator.Get<WaveBossRewardManager>();
                manager.OnRewardOptionsChanged -= this.HandleRewardOptionsChanged;
                manager.OnStateChanged -= this.HandleStateChanged;
            }
            catch (Exception)
            {
            }
        }

        private void Update()
        {
            this.HandleHotkeys();
            if (Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
                this.RefreshSummary();
            }
        }

        /// <summary>
        /// 设置面板显示状态。
        /// </summary>
        /// <param name="visible">是否显示。</param>
        public void SetVisible(bool visible)
        {
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.GetComponent<CanvasGroup>();
            }

            if (this.canvasGroup == null)
            {
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = visible;
            this.canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 按当前管理器状态刷新面板。
        /// </summary>
        public void UpdateFromManager()
        {
            this.RefreshSummary();
            this.RefreshOptions(Core.ServiceLocator.Get<WaveBossRewardManager>().CurrentOptions);
        }

        /// <summary>
        /// 运行时奖励出现回调。
        /// </summary>
        /// <param name="options">奖励选项。</param>
        private static void HandleRuntimeRewardOptionsChanged(IReadOnlyList<WaveRewardOption> options)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            EnsureRuntimePanel();
        }

        /// <summary>
        /// 查找或创建独立 Canvas。
        /// </summary>
        /// <returns>Canvas 组件。</returns>
        private static Canvas FindOrCreateCanvas()
        {
            GameObject canvasObject = GameObject.Find(WaveBossRewardConstant.CanvasName);
            if (canvasObject == null)
            {
                canvasObject = new GameObject(
                    WaveBossRewardConstant.CanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                Canvas newCanvas = canvasObject.GetComponent<Canvas>();
                newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvasObject.GetComponent<Canvas>();
        }

        /// <summary>
        /// 创建布局，已存在时只做组件缓存。
        /// </summary>
        private void BuildLayoutIfNeeded()
        {
            if (this.titleText == null)
            {
                this.titleText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WaveBossRewardConstant.TitleTextName);
            }

            if (this.summaryText == null)
            {
                this.summaryText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WaveBossRewardConstant.SummaryTextName);
            }

            if (this.titleText == null)
            {
                this.titleText = this.CreateText(
                    WaveBossRewardConstant.TitleTextName,
                    new Vector2(0.0f, 126.0f),
                    new Vector2(520.0f, 44.0f),
                    24,
                    TextAnchor.MiddleCenter);
                this.titleText.text = WaveBossRewardConstant.PanelTitle;
                this.titleText.color = PixelUITheme.TextAccent;
            }

            if (this.summaryText == null)
            {
                this.summaryText = this.CreateText(
                    WaveBossRewardConstant.SummaryTextName,
                    new Vector2(0.0f, 78.0f),
                    new Vector2(520.0f, 58.0f),
                    15,
                    TextAnchor.MiddleCenter);
                this.summaryText.text = WaveBossRewardConstant.EmptyRewardText;
            }

            this.optionButtons.Clear();
            this.optionTexts.Clear();
            for (int i = 0; i < WaveBossRewardConstant.RewardOptionCount; i++)
            {
                string buttonName = WaveBossRewardConstant.OptionButtonPrefix + (i + 1);
                Button button = LAB2D.Tool.Tool.GetComponentInChildren<Button>(this.gameObject, buttonName);
                Text optionText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WaveBossRewardConstant.OptionTextPrefix + (i + 1));

                if (button == null)
                {
                    button = this.CreateOptionButton(i);
                    optionText = button.GetComponentInChildren<Text>();
                }

                int optionIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Core.ServiceLocator.Get<WaveBossRewardManager>().SelectReward(optionIndex));
                this.optionButtons.Add(button);
                this.optionTexts.Add(optionText);
            }
        }

        /// <summary>
        /// 创建文本控件。
        /// </summary>
        /// <param name="name">节点名。</param>
        /// <param name="position">锚点位置。</param>
        /// <param name="size">尺寸。</param>
        /// <param name="fontSize">字号。</param>
        /// <param name="alignment">对齐方式。</param>
        /// <returns>Text 组件。</returns>
        private Text CreateText(string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(this.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = LoadFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            return text;
        }

        /// <summary>
        /// 创建奖励按钮。
        /// </summary>
        /// <param name="index">按钮索引。</param>
        /// <returns>Button 组件。</returns>
        private Button CreateOptionButton(int index)
        {
            GameObject buttonObject = new GameObject(
                WaveBossRewardConstant.OptionButtonPrefix + (index + 1),
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(this.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0.0f, 24.0f - (index * 74.0f));
            buttonRect.sizeDelta = new Vector2(500.0f, 62.0f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = PixelUITheme.ButtonNormal;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = PixelUITheme.ButtonNormal;
            colors.highlightedColor = PixelUITheme.ButtonHighlighted;
            colors.pressedColor = PixelUITheme.ButtonPressed;
            colors.selectedColor = PixelUITheme.ButtonSelected;
            button.colors = colors;

            Text text = this.CreateButtonText(buttonObject.transform, index);
            text.text = $"{index + 1}. {WaveBossRewardConstant.EmptyRewardText}";
            return button;
        }

        /// <summary>
        /// 创建按钮文本。
        /// </summary>
        /// <param name="parent">按钮根节点。</param>
        /// <param name="index">按钮索引。</param>
        /// <returns>Text 组件。</returns>
        private Text CreateButtonText(Transform parent, int index)
        {
            GameObject textObject = new GameObject(
                WaveBossRewardConstant.OptionTextPrefix + (index + 1),
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14.0f, 6.0f);
            rect.offsetMax = new Vector2(-14.0f, -6.0f);

            Text text = textObject.GetComponent<Text>();
            text.font = LoadFont();
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            return text;
        }

        /// <summary>
        /// 加载项目字体，失败时使用 Unity 内置字体。
        /// </summary>
        /// <returns>字体。</returns>
        private static Font LoadFont()
        {
            Font font = Resources.Load<Font>(WaveBossRewardConstant.FontResourcePath);
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// 奖励选项变化回调。
        /// </summary>
        /// <param name="options">奖励选项。</param>
        private void HandleRewardOptionsChanged(IReadOnlyList<WaveRewardOption> options)
        {
            this.RefreshOptions(options);
        }

        /// <summary>
        /// 状态变化回调。
        /// </summary>
        /// <param name="state">新状态。</param>
        private void HandleStateChanged(WaveBossRewardState state)
        {
            this.RefreshSummary();
        }

        /// <summary>
        /// 刷新状态摘要文本。
        /// </summary>
        private void RefreshSummary()
        {
            if (this.summaryText == null)
            {
                return;
            }

            this.summaryText.text = Core.ServiceLocator.Get<WaveBossRewardManager>().CurrentState.ToSummaryText();
        }

        /// <summary>
        /// 刷新奖励按钮。
        /// </summary>
        /// <param name="options">奖励选项。</param>
        private void RefreshOptions(IReadOnlyList<WaveRewardOption> options)
        {
            bool hasOptions = options != null && options.Count > 0;
            this.SetVisible(hasOptions);

            for (int i = 0; i < this.optionButtons.Count; i++)
            {
                bool active = hasOptions && i < options.Count;
                this.optionButtons[i].gameObject.SetActive(active);
                if (active && this.optionTexts[i] != null)
                {
                    this.optionTexts[i].text = options[i].ToButtonText(i);
                }
            }
        }

        /// <summary>
        /// 处理数字键快捷选择。
        /// </summary>
        private void HandleHotkeys()
        {
            if (UnityGlobalInputAdapter.TryGetWaveBossRewardOptionDown(out int optionIndex))
            {
                Core.ServiceLocator.Get<WaveBossRewardManager>().SelectReward(optionIndex);
            }
        }
    }
}
