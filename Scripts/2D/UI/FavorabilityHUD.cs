namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 好感度 HUD 绑定脚本 — F11 切换显示每名 Worker 对玩家的好感值+态度标签，附最强 Worker↔Worker 关系。
    /// 只读 FavorabilityManager 数据，不创建存档、不修改资源。
    /// </summary>
    [DisallowMultipleComponent]
    public class FavorabilityHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 好感度显示文本。
        /// </summary>
        public Text favorabilityText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧拼接文本。
        /// </summary>
        public float refreshInterval = FavorabilityConstant.HudRefreshInterval;

        /// <summary>
        /// HUD 显示隐藏热键（F11）。
        /// </summary>
        public KeyCode toggleKey = FavorabilityConstant.HudToggleKey;

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = FavorabilityConstant.HudToggleKey;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.favorabilityText == null)
            {
                this.favorabilityText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    FavorabilityConstant.HudTextName);
            }

            // 默认隐藏，按 F11 切换显示
            this.canvasGroup.alpha = 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
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
                this.nextRefreshTime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
                this.UpdateDisplay();
            }
        }

        /// <summary>
        /// 设置 HUD 是否可见。
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
        }

        /// <summary>
        /// 刷新 HUD 文本。
        /// </summary>
        private void UpdateDisplay()
        {
            if (this.favorabilityText == null)
            {
                return;
            }

            FavorabilityManager manager = Core.ServiceLocator.Get<FavorabilityManager>();
            if (manager == null)
            {
                return;
            }

            this.favorabilityText.text = manager.BuildSummaryText();
        }

        /// <summary>
        /// 确保运行时存在 FavorabilityHUD。挂载到 UIRoot/Foreground 下。
        /// 场景中需手动创建面板（根节点挂 CanvasGroup+FavorabilityHUD，子节点 FavorabilityText 挂 Text），
        /// 此处只查找并修复已有实例。
        /// </summary>
        public static FavorabilityHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(FavorabilityConstant.HudRootName);
            if (existingTransform != null)
            {
                FavorabilityHUD existingHud = existingTransform.GetComponent<FavorabilityHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, FavorabilityConstant.HudToggleKey, false);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            Debug.LogWarning("[FavorabilityHUD] 在 Foreground 下未找到 FavorabilityHUD，请手动创建。");
            return null;
        }
    }
}
