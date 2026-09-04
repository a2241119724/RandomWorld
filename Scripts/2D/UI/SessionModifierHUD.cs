namespace LAB2D.UI
{
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 本局天机 HUD — H 键切换显示本局 roll 到的全局修饰符（名称/通道数值/风味描述）。
    /// 纯代码构建（MountainGateHUD 同款 EnsureRuntimePanel 模式，场景无需节点）；
    /// 只读 SessionModifierManager 数据，修饰符整局不变，切换到可见时刷新一次。
    /// 右上角锚定在山门核心 HUD（常驻）正下方。
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionModifierHUD : MonoBehaviour
    {
        /// <summary>HUD 根节点名称。</summary>
        public const string HudRootName = "SessionModifierHUD";

        private CanvasGroup canvasGroup;
        private Text contentText;
        private bool uiBuilt;

        /// <summary>HUD 显示隐藏热键（H）。</summary>
        public KeyCode toggleKey = InputKeyConstant.ToggleSessionModifierHud;

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = InputKeyConstant.ToggleSessionModifierHud;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.BuildUI();
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }
        }

        /// <summary>
        /// 设置 HUD 是否可见；重新显示时立即刷新（读档/roll 都可能发生在隐藏期间）。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (this.canvasGroup == null)
            {
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
            if (visible)
            {
                this.RefreshContent();
            }
        }

        /// <summary>
        /// 确保运行时存在 SessionModifierHUD：Foreground 下按名查找，无则自建节点 + 挂组件。
        /// </summary>
        /// <returns>HUD 实例；UI 父节点不存在时为 null。</returns>
        public static SessionModifierHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            if (parent == null)
            {
                return null;
            }

            Transform existing = parent.Find(HudRootName);
            GameObject root = existing != null ? existing.gameObject : new GameObject(HudRootName, typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(parent, false);
            }

            SessionModifierHUD hud = root.GetComponent<SessionModifierHUD>();
            if (hud == null)
            {
                hud = root.AddComponent<SessionModifierHUD>();
            }

            return hud;
        }

        /// <summary>
        /// 纯代码构建 UI：半透明底 + 单个多行富文本（最多 3 条修饰符，无需滚动）。
        /// </summary>
        private void BuildUI()
        {
            if (this.uiBuilt)
            {
                return;
            }

            this.uiBuilt = true;

            RectTransform root = this.GetComponent<RectTransform>();
            // 右上角锚定：山门核心 HUD（250x175 @ -16,-16）正下方
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-16f, -207f);
            root.sizeDelta = new Vector2(260f, 190f);

            Image background = this.gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = this.gameObject.AddComponent<Image>();
            }

            background.color = PixelUITheme.ViewportBg;
            background.raycastTarget = false;

            GameObject textGo = new GameObject("Content", typeof(RectTransform));
            textGo.transform.SetParent(root, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 6f);
            textRt.offsetMax = new Vector2(-8f, -6f);

            this.contentText = textGo.AddComponent<Text>();
            this.contentText.font = UIFontConfig.GetFont();
            this.contentText.fontSize = 12;
            this.contentText.color = PixelUITheme.TextOnDark;
            this.contentText.alignment = TextAnchor.UpperLeft;
            this.contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            this.contentText.verticalOverflow = VerticalWrapMode.Overflow;
            this.contentText.raycastTarget = false;

            this.RefreshContent();

            // 默认隐藏（与 F 系 HUD 惯例一致）
            this.canvasGroup.alpha = 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 刷新内容：本局激活修饰符逐行（名+通道数值+描述）；读档前 Manager 未就绪时显示占位。
        /// </summary>
        private void RefreshContent()
        {
            if (this.contentText == null)
            {
                return;
            }

            Gameplay.SessionModifierManager manager = Gameplay.SessionModifierManager.Instance;
            if (manager == null || manager.ActiveIds.Count == 0)
            {
                this.contentText.text = $"<color={PixelUITheme.RichGold}>本局天机 (H)</color>\n尚未揭晓";
                return;
            }

            var builder = new System.Text.StringBuilder(512);
            builder.Append($"<color={PixelUITheme.RichGold}>本局天机 (H)</color>\n");
            foreach (string id in manager.ActiveIds)
            {
                Domain.Gameplay.SessionModifierDefinition definition =
                    Domain.Gameplay.SessionModifierRuleService.GetById(id);
                if (definition != null)
                {
                    builder.Append('\n').Append(Domain.Gameplay.SessionModifierRuleService.FormatModifierLine(definition));
                }
            }

            this.contentText.text = builder.ToString();
        }
    }
}
