namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Gameplay;
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 工人状态 HUD 绑定脚本。
    /// 可由 Editor 菜单创建到 Game.unity，也可手动挂载到包含 WorkerConditionText 子节点的 UI 根节点。
    /// 本脚本只读取 WorkerConditionManager 数据，不创建存档，不修改资源。
    /// </summary>
    [DisallowMultipleComponent]
    public class WorkerConditionHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 工人状态显示文本。
        /// </summary>
        public Text conditionText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧拼接文本。
        /// </summary>
        public float refreshInterval = WorkerConditionConstant.HudRefreshInterval;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = WorkerConditionConstant.HudToggleKey;

        private void Awake()
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.conditionText == null)
            {
                this.conditionText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WorkerConditionConstant.HudTextName);
            }
        }

        private void OnEnable()
        {
            try
            {
                WorkerConditionManager.Instance.OnWorkerConditionChanged += this.HandleWorkerConditionChanged;
                this.UpdateDisplay();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[WorkerConditionHUD] 绑定工人状态数据失败: " + exception.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                WorkerConditionManager.Instance.OnWorkerConditionChanged -= this.HandleWorkerConditionChanged;
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
        /// 工人状态变化回调。
        /// </summary>
        /// <param name="worker">状态变化的工人。</param>
        /// <param name="snapshot">新的状态快照。</param>
        private void HandleWorkerConditionChanged(AWorker worker, WorkerConditionSnapshot snapshot)
        {
            this.UpdateDisplay();
        }

        /// <summary>
        /// 刷新 HUD 文本。
        /// </summary>
        private void UpdateDisplay()
        {
            if (this.conditionText == null)
            {
                return;
            }

            this.conditionText.text = WorkerConditionManager.Instance.BuildSummaryText();
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
