namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Gameplay;
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
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.effectText == null)
            {
                this.effectText = Tool.GetComponentInChildren<Text>(this.gameObject, "WeatherText");
            }
        }

        private void OnEnable()
        {
            try
            {
                WeatherGameplayEffect effect = WeatherGameplayEffect.Instance;
                effect.OnWeatherEffectChanged += this.HandleWeatherChanged;
                this.UpdateDisplay(effect.CurrentState);
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
                WeatherGameplayEffect.Instance.OnWeatherEffectChanged -= this.HandleWeatherChanged;
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
                this.UpdateDisplay(WeatherGameplayEffect.Instance.CurrentState);
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
