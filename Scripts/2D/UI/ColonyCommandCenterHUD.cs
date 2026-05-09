namespace LAB2D
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A006 殖民地运营指挥中心 HUD。
    /// 可运行时动态创建，也可由 Editor 菜单安装到 Game.unity 或生成 ResourcesLocal Prefab。
    /// 本脚本只展示 `ColonyCommandCenterManager` 的只读报告，不改变 Worker、任务、补给或存档。
    /// </summary>
    [DisallowMultipleComponent]
    public class ColonyCommandCenterHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 标题文本。
        /// </summary>
        public Text titleText;

        /// <summary>
        /// 主摘要文本。
        /// </summary>
        public Text mainText;

        /// <summary>
        /// 细节文本。
        /// </summary>
        public Text detailText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧统计任务和拼接文本。
        /// </summary>
        public float refreshInterval = ColonyCommandCenterConstant.RefreshInterval;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = ColonyCommandCenterConstant.HudToggleKey;

        /// <summary>
        /// 确保运行时存在指挥中心 HUD。
        /// 该方法只会创建带 A006 前缀的独立 Canvas 和根节点，不修改已有 UI 层级。
        /// </summary>
        /// <returns>指挥中心 HUD 组件。</returns>
        public static ColonyCommandCenterHUD EnsureRuntimePanel()
        {
            GameObject existing = GameObject.Find(ColonyCommandCenterConstant.HudRootName);
            if (existing != null)
            {
                ColonyCommandCenterHUD existingHud = existing.GetComponent<ColonyCommandCenterHUD>();
                if (existingHud != null)
                {
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            GameObject canvasObject = GameObject.Find(ColonyCommandCenterConstant.CanvasName);
            if (canvasObject == null)
            {
                canvasObject = CreateCanvasObject();
            }

            GameObject root = CreatePanelRoot(canvasObject.transform);
            ColonyCommandCenterHUD hud = root.GetComponent<ColonyCommandCenterHUD>();
            hud.UpdateDisplay();
            return hud;
        }

        /// <summary>
        /// 创建独立 Canvas 对象。
        /// 运行时和 Editor 生成 Prefab 时共用，避免重复维护 UI 基础结构。
        /// </summary>
        /// <returns>Canvas 根对象。</returns>
        public static GameObject CreateCanvasObject()
        {
            GameObject canvasObject = new GameObject(
                ColonyCommandCenterConstant.CanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        /// <summary>
        /// 创建 HUD 根节点和完整 UI 层级。
        /// </summary>
        /// <param name="parent">Canvas 或目标父节点。</param>
        /// <returns>HUD 根对象。</returns>
        public static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject root = new GameObject(
                ColonyCommandCenterConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(ColonyCommandCenterHUD));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1.0f, 1.0f);
            rootRect.anchorMax = new Vector2(1.0f, 1.0f);
            rootRect.pivot = new Vector2(1.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(
                ColonyCommandCenterConstant.HudAnchoredX,
                ColonyCommandCenterConstant.HudAnchoredY);
            rootRect.sizeDelta = new Vector2(
                ColonyCommandCenterConstant.HudWidth,
                ColonyCommandCenterConstant.HudHeight);

            GameObject background = new GameObject(
                ColonyCommandCenterConstant.BackgroundName,
                typeof(RectTransform),
                typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(
                PixelUITheme.DialogBoxBg.r,
                PixelUITheme.DialogBoxBg.g,
                PixelUITheme.DialogBoxBg.b,
                0.94f);
            backgroundImage.raycastTarget = false;

            Text title = CreateText(
                root.transform,
                ColonyCommandCenterConstant.TitleTextName,
                ColonyCommandCenterConstant.TitleFontSize,
                TextAnchor.MiddleLeft);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.0f, 1.0f);
            titleRect.anchorMax = new Vector2(1.0f, 1.0f);
            titleRect.pivot = new Vector2(0.5f, 1.0f);
            titleRect.offsetMin = new Vector2(ColonyCommandCenterConstant.Padding, -44.0f);
            titleRect.offsetMax = new Vector2(-ColonyCommandCenterConstant.Padding, -8.0f);

            Text main = CreateText(
                root.transform,
                ColonyCommandCenterConstant.MainTextName,
                ColonyCommandCenterConstant.MainFontSize,
                TextAnchor.UpperLeft);
            RectTransform mainRect = main.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0.0f, 0.48f);
            mainRect.anchorMax = new Vector2(1.0f, 0.88f);
            mainRect.offsetMin = new Vector2(ColonyCommandCenterConstant.Padding, 0.0f);
            mainRect.offsetMax = new Vector2(-ColonyCommandCenterConstant.Padding, 0.0f);

            Text detail = CreateText(
                root.transform,
                ColonyCommandCenterConstant.DetailTextName,
                ColonyCommandCenterConstant.DetailFontSize,
                TextAnchor.UpperLeft);
            RectTransform detailRect = detail.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.0f, 0.0f);
            detailRect.anchorMax = new Vector2(1.0f, 0.46f);
            detailRect.offsetMin = new Vector2(ColonyCommandCenterConstant.Padding, ColonyCommandCenterConstant.Padding);
            detailRect.offsetMax = new Vector2(-ColonyCommandCenterConstant.Padding, -4.0f);

            ColonyCommandCenterHUD hud = root.GetComponent<ColonyCommandCenterHUD>();
            hud.titleText = title;
            hud.mainText = main;
            hud.detailText = detail;
            hud.SetVisible(true);
            return root;
        }

        private void Awake()
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.titleText == null)
            {
                this.titleText = Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.TitleTextName);
            }

            if (this.mainText == null)
            {
                this.mainText = Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.MainTextName);
            }

            if (this.detailText == null)
            {
                this.detailText = Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.DetailTextName);
            }
        }

        private void OnEnable()
        {
            try
            {
                ColonyCommandCenterManager.Instance.OnCommandReportChanged += this.HandleCommandReportChanged;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(ColonyCommandCenterConstant.LogPrefix + " 绑定报告事件失败: " + exception.Message);
            }

            this.nextRefreshTime = 0.0f;
            this.UpdateDisplay();
        }

        private void OnDisable()
        {
            try
            {
                ColonyCommandCenterManager.Instance.OnCommandReportChanged -= this.HandleCommandReportChanged;
            }
            catch (Exception)
            {
            }
        }

        private void Update()
        {
            if (this.CanUseHotkey() && Input.GetKeyDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            if (Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, this.refreshInterval);
                this.UpdateDisplay();
            }
        }

        /// <summary>
        /// 设置 HUD 是否可见。
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
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 指挥报告变化回调。
        /// </summary>
        /// <param name="report">新的指挥报告。</param>
        private void HandleCommandReportChanged(ColonyCommandCenterReport report)
        {
            this.ApplyReport(report);
        }

        /// <summary>
        /// 刷新 HUD 文本。
        /// </summary>
        private void UpdateDisplay()
        {
            try
            {
                this.ApplyReport(ColonyCommandCenterManager.Instance.Refresh(false));
            }
            catch (Exception exception)
            {
                if (this.mainText != null)
                {
                    this.mainText.text = "殖民地指挥中心刷新失败: " + exception.Message;
                }
            }
        }

        /// <summary>
        /// 应用报告到 HUD。
        /// </summary>
        /// <param name="report">指挥报告。</param>
        private void ApplyReport(ColonyCommandCenterReport report)
        {
            if (report == null)
            {
                if (this.titleText != null)
                {
                    this.titleText.text = "殖民地指挥中心";
                }

                if (this.mainText != null)
                {
                    this.mainText.text = ColonyCommandCenterConstant.EmptyText;
                }

                if (this.detailText != null)
                {
                    this.detailText.text = string.Empty;
                }

                return;
            }

            if (this.titleText != null)
            {
                this.titleText.text = "殖民地指挥中心 <color=" +
                    ColonyCommandCenterTool.GetAlertLevelRichColor(report.AlertLevel) +
                    ">[" + ColonyCommandCenterTool.GetAlertLevelName(report.AlertLevel) + "]</color>";
            }

            if (this.mainText != null)
            {
                this.mainText.text = report.ToMainText();
            }

            if (this.detailText != null)
            {
                this.detailText.text = report.ToDetailText();
            }
        }

        /// <summary>
        /// 创建 Text 节点。
        /// </summary>
        /// <param name="parent">父节点。</param>
        /// <param name="name">节点名。</param>
        /// <param name="fontSize">字号。</param>
        /// <param name="alignment">对齐方式。</param>
        /// <returns>Text 组件。</returns>
        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            Font font = Resources.Load<Font>(WorkerConditionConstant.FontResourcePath);
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = PixelUITheme.TextPrimary;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 判断当前是否可处理热键。
        /// </summary>
        /// <returns>没有 UI 输入框聚焦时返回 true。</returns>
        private bool CanUseHotkey()
        {
            try
            {
                return !Tool.IsUIInputActive();
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
