namespace LAB2D.UI
{
      using LAB2D;
    using LAB2D.Core;
      using LAB2D.Domain.Common;
      using LAB2D.Domain.Worker;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 工人补给缺口 HUD 绑定脚本。
    /// 可由 Editor 菜单创建到 Game.unity，也可手动挂载到包含 WorkerSupplyText 子节点的 UI 根节点。
    /// 本脚本只读取 WorkerSupplyIssueManager 数据，不创建存档，不修改仓库、床位或资源。
    /// </summary>
    [DisallowMultipleComponent]
    public class WorkerSupplyHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 补给缺口显示文本。
        /// </summary>
        public Text supplyText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧拼接文本。
        /// </summary>
        public float refreshInterval = WorkerSupplyConstant.MonitorRefreshInterval;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = WorkerSupplyConstant.HudToggleKey;

        private void Awake()
        {
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = WorkerSupplyConstant.HudToggleKey;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.supplyText == null)
            {
                this.supplyText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WorkerSupplyConstant.HudTextName);
            }

            // 默认隐藏，按 F6 切换显示
            this.canvasGroup.alpha = 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            try
            {
                WorkerSupplyIssueManager manager = Core.ServiceLocator.TryGet(out WorkerSupplyIssueManager mgr) ? mgr : ServiceLocator.Get<WorkerSupplyIssueManager>();
                manager.OnWorkerSupplyReportChanged += this.HandleSupplyReportChanged;
                this.UpdateDisplay();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WorkerSupplyHUD] 绑定补给缺口数据失败: " + exception.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                WorkerSupplyIssueManager manager = Core.ServiceLocator.TryGet(out WorkerSupplyIssueManager mgr) ? mgr : ServiceLocator.Get<WorkerSupplyIssueManager>();
                manager.OnWorkerSupplyReportChanged -= this.HandleSupplyReportChanged;
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
                return;
            }

            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 补给缺口报告变化回调。
        /// </summary>
        /// <param name="report">新的补给缺口报告。</param>
        private void HandleSupplyReportChanged(WorkerSupplyReport report)
        {
            this.UpdateDisplay();
        }

        /// <summary>
        /// 刷新 HUD 文本。
        /// </summary>
        private void UpdateDisplay()
        {
            if (this.supplyText == null)
            {
                return;
            }

            this.supplyText.text = Core.ServiceLocator.Get<WorkerSupplyIssueManager>().BuildSummaryText();
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
        /// 确保运行时存在 WorkerSupplyHUD。挂载到 UIRoot/Foreground 下。
        /// </summary>
        public static WorkerSupplyHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(WorkerSupplyConstant.HudRootName);
            if (existingTransform != null)
            {
                WorkerSupplyHUD existingHud = existingTransform.GetComponent<WorkerSupplyHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, WorkerSupplyConstant.HudToggleKey, false);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            Debug.LogWarning($"[WorkerSupplyHUD] 在 Foreground 下未找到 {WorkerSupplyConstant.HudRootName}，请手动创建。");
            return null;
        }
    }
}
