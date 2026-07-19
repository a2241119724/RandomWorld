namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 天气玩法 HUD 绑定脚本。
    /// 可由 Editor 菜单创建到 Game.unity，也可手动挂载到包含 WeatherText 子节点的 UI 根节点。
    /// 本脚本只读取 WeatherGameplayEffect 数据，不创建存档，不修改资源。
    /// </summary>
    [DisallowMultipleComponent]
    public class WeatherGameplayHUD : MonoBehaviour
    {
        /// <summary>HUD 根节点名称，Editor 菜单和运行时共用。</summary>
        public const string HudRootName = "WeatherGameplayHUDRoot";

        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 天气效果显示文本。
        /// </summary>
        public Text effectText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧拼接文本。
        /// </summary>
        public float refreshInterval = 0.5f;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = InputKeyConstant.ToggleWeatherHud;

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = InputKeyConstant.ToggleWeatherHud;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.effectText == null)
            {
                this.effectText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "WeatherText");
            }

            // 默认隐藏，按 F4 切换显示
            this.canvasGroup.alpha = 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            try
            {
                IWeatherGameplayService weather = ServiceLocator.Get<IWeatherGameplayService>();
                weather.OnWeatherEffectChanged += this.HandleWeatherChanged;
                this.UpdateDisplay(weather.CurrentState);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WeatherGameplayHUD] 绑定天气玩法数据失败: " + exception.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                ServiceLocator.Get<IWeatherGameplayService>().OnWeatherEffectChanged -= this.HandleWeatherChanged;
            }
            catch (Exception)
            {
            }
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            if (Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + Mathf.Max(0.1f, this.refreshInterval);
                this.UpdateDisplay(ServiceLocator.Get<IWeatherGameplayService>().CurrentState);
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
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 天气效果变化回调。
        /// </summary>
        /// <param name="state">新的天气玩法状态。</param>
        private void HandleWeatherChanged(WeatherGameplayState state)
        {
            this.UpdateDisplay(state);
        }

        /// <summary>
        /// 刷新 HUD 文本。
        /// </summary>
        /// <param name="state">天气玩法状态。</param>
        private void UpdateDisplay(WeatherGameplayState state)
        {
            if (this.effectText == null || state == null)
            {
                return;
            }

            this.effectText.text =
                $"<color={PixelUITheme.RichGold}>天气: {state.WeatherName}</color>\n" +
                $"玩家移动 {state.PlayerMoveSpeedMultiplier:0.00}x | 工人移动 {state.WorkerMoveSpeedMultiplier:0.00}x\n" +
                $"工人工作 {state.WorkerTaskProgressMultiplier:0.00}x | 灵气恢复 {state.EnergyRecoveryMultiplier:0.00}x";
        }

        /// <summary>
        /// 判断当前是否可处理热键。
        /// </summary>
        /// <returns>没有 UI 输入框聚焦时返回 true。</returns>
        private bool CanUseHotkey()
        {
            return UnityGlobalInputAdapter.CanUseHudHotkey();
        }

        /// <summary>
        /// 确保运行时存在 WeatherGameplayHUD。挂载到 UIRoot/Foreground 下。
        /// </summary>
        public static WeatherGameplayHUD EnsureRuntimePanel()
        {
            GameObject existing = GameObject.Find(HudRootName);
            if (existing != null)
            {
                WeatherGameplayHUD existingHud = existing.GetComponent<WeatherGameplayHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, InputKeyConstant.ToggleWeatherHud, false);
                    existingHud.UpdateDisplay(ServiceLocator.Get<IWeatherGameplayService>()?.CurrentState);
                    return existingHud;
                }
            }

            Transform parent = HudFactory.FindHudParent();
            GameObject root = CreatePanelRoot(parent);
            WeatherGameplayHUD hud = root.GetComponent<WeatherGameplayHUD>();
            hud.UpdateDisplay(ServiceLocator.Get<IWeatherGameplayService>()?.CurrentState);
            return hud;
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject root = new GameObject(
                HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(20.0f, -96.0f);
            rootRect.sizeDelta = new Vector2(360.0f, 86.0f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = PixelUITheme.DialogBoxBg;

            GameObject textObj = new GameObject("WeatherText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(root.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12.0f, 8.0f);
            textRect.offsetMax = new Vector2(-12.0f, -8.0f);

            Text text = textObj.GetComponent<Text>();
            text.font = Resources.Load<Font>("Font/ark-pixel-12px-monospaced-zh_cn");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = "天气: 等待运行时数据";

            WeatherGameplayHUD hud = root.AddComponent<WeatherGameplayHUD>();
            hud.effectText = text;
            hud.SetVisible(false);
            return root;
        }
    }
}
