namespace LAB2D.UI
{
      using LAB2D;
    using LAB2D.Core;
      using LAB2D.Constant;
      using LAB2D.Domain.Common;
      using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
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
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

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
        /// HUD 根节点会放在 UIRoot/Foreground 下，复用已有 UI Canvas，不再创建独立 Canvas。
        /// </summary>
        /// <returns>指挥中心 HUD 组件。</returns>
        public static ColonyCommandCenterHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(ColonyCommandCenterConstant.HudRootName);
            if (existingTransform != null)
            {
                ColonyCommandCenterHUD existingHud = existingTransform.GetComponent<ColonyCommandCenterHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, ColonyCommandCenterConstant.HudToggleKey);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            GameLoggerFactory.Get().LogWarning($"[ColonyCommandCenterHUD] 在 Foreground 下未找到 {ColonyCommandCenterConstant.HudRootName}，请手动创建。");
            return null;
        }

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值导致 F8 无效
            this.toggleKey = ColonyCommandCenterConstant.HudToggleKey;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.titleText == null)
            {
                this.titleText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.TitleTextName);
            }

            if (this.mainText == null)
            {
                this.mainText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.MainTextName);
            }

            if (this.detailText == null)
            {
                this.detailText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    ColonyCommandCenterConstant.DetailTextName);
            }
        }

        private void OnEnable()
        {
            try
            {
                IColonyCommandCenterService service = ServiceLocator.TryGet(out IColonyCommandCenterService svc) ? svc : ServiceLocator.Get<ColonyCommandCenterManager>();
                service.OnCommandReportChanged += this.HandleCommandReportChanged;
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning(ColonyCommandCenterConstant.LogPrefix + " 绑定报告事件失败: " + exception.Message);
            }

            this.nextRefreshTime = 0.0f;
            this.UpdateDisplay();
        }

        private void OnDisable()
        {
            try
            {
                IColonyCommandCenterService service = ServiceLocator.TryGet(out IColonyCommandCenterService svc) ? svc : ServiceLocator.Get<ColonyCommandCenterManager>();
                service.OnCommandReportChanged -= this.HandleCommandReportChanged;
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
                this.nextRefreshTime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
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
                this.ApplyReport(ServiceLocator.Get<IColonyCommandCenterService>().Refresh(false));
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
        /// 判断当前是否可处理热键。
        /// </summary>
        /// <returns>没有 UI 输入框聚焦时返回 true。</returns>
        private bool CanUseHotkey()
        {
            return UnityGlobalInputAdapter.CanUseHudHotkey();
        }
    }
}
