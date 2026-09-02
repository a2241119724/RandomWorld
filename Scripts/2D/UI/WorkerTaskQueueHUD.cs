namespace LAB2D.UI
{
      using LAB2D;
    using LAB2D.Core;
      using LAB2D.Domain.Common;
      using LAB2D.UnityAdapter;
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
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

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
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = WorkerTaskHudConstant.HudToggleKey;

            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.queueText == null)
            {
                this.queueText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(
                    this.gameObject,
                    WorkerTaskHudConstant.HudTextName);
            }
        }

        private void OnEnable()
        {
            this.nextRefreshTime = 0.0f;
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            // 隐藏时（默认隐藏）不构建队列摘要：任务统计+大字符串拼接在不可见时纯属浪费；
            // 切回可见时由 SetVisible 置 0 补一次刷新。
            bool isVisible = this.canvasGroup == null || this.canvasGroup.alpha > 0.5f;
            if (!isVisible)
            {
                return;
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

            // 重新显示时立即刷新（隐藏期间不刷新会残留旧数据），置 0 让下帧 Update 放行
            if (visible)
            {
                this.nextRefreshTime = 0f;
            }
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
                WorkerTaskManager manager = Core.ServiceLocator.TryGet(out WorkerTaskManager mgr) ? mgr : ServiceLocator.Get<WorkerTaskManager>();
                this.queueText.text = manager == null
                    ? WorkerTaskHudConstant.ManagerUnavailableText
                    : manager.GetTaskQueueSummaryText();
            }
            catch (Exception exception)
            {
                this.queueText.text = WorkerTaskHudConstant.ManagerUnavailableText;
                this.GameLogger.LogWarning("[WorkerTaskQueueHUD] 刷新任务队列失败: " + exception.Message);
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

        /// <summary>
        /// 确保运行时存在 WorkerTaskQueueHUD。挂载到 UIRoot/Foreground 下。
        /// </summary>
        public static WorkerTaskQueueHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(WorkerTaskHudConstant.HudRootName);
            if (existingTransform != null)
            {
                WorkerTaskQueueHUD existingHud = existingTransform.GetComponent<WorkerTaskQueueHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, WorkerTaskHudConstant.HudToggleKey, true);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            GameLoggerFactory.Get().LogWarning($"[WorkerTaskQueueHUD] 在 Foreground 下未找到 {WorkerTaskHudConstant.HudRootName}，请手动创建。");
            return null;
        }
    }
}
