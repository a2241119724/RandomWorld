namespace LAB2D
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 工人任务队列 HUD 绑定脚本。
    /// 可由 Editor 菜单创建到 Game.unity，也可手动挂载到包含 WorkerTaskQueueText 子节点的 UI 根节点。
    /// 本脚本只读取 WorkerTaskManager 的任务队列快照，不新增、取消或重排任务。
    /// </summary>
    [DisallowMultipleComponent]
    public class WorkerTaskQueueHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private float nextRefreshTime;

        /// <summary>
        /// 任务队列显示文本。
        /// </summary>
        public Text queueText;

        /// <summary>
        /// HUD 刷新间隔，避免每帧统计任务和拼接文本。
        /// </summary>
        public float refreshInterval = WorkerTaskHudConstant.HudRefreshInterval;

        /// <summary>
        /// HUD 显示隐藏热键。
        /// </summary>
        public KeyCode toggleKey = WorkerTaskHudConstant.HudToggleKey;

        private void Awake()
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.queueText == null)
            {
                this.queueText = Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WorkerTaskHudConstant.HudTextName);
            }
        }

        private void OnEnable()
        {
            this.nextRefreshTime = 0.0f;
            this.UpdateDisplay();
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
        /// 刷新 HUD 文本。
        /// </summary>
        private void UpdateDisplay()
        {
            if (this.queueText == null)
            {
                return;
            }

            try
            {
                WorkerTaskManager manager = WorkerTaskManager.Instance;
                this.queueText.text = manager == null
                    ? WorkerTaskHudConstant.ManagerUnavailableText
                    : manager.GetTaskQueueSummaryText();
            }
            catch (Exception exception)
            {
                this.queueText.text = WorkerTaskHudConstant.ManagerUnavailableText;
                Debug.LogWarning("[WorkerTaskQueueHUD] 刷新任务队列失败: " + exception.Message);
            }
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
