namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Domain.Worker;
    using LAB2D.Gameplay;
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
        }

        private void OnEnable()
        {
            try
            {
                WorkerSupplyIssueManager.Instance.OnWorkerSupplyReportChanged += this.HandleSupplyReportChanged;
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
                WorkerSupplyIssueManager.Instance.OnWorkerSupplyReportChanged -= this.HandleSupplyReportChanged;
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

            this.supplyText.text = WorkerSupplyIssueManager.Instance.BuildSummaryText();
        }

        /// <summary>
        /// 判断当前是否可处理热键。
        /// </summary>
        /// <returns>没有 UI 输入框聚焦时返回 true。</returns>
        private bool CanUseHotkey()
        {
            try
            {
                return !LAB2D.Tool.Tool.IsUIInputActive();
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
