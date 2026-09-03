namespace LAB2D.UI
{
      using LAB2D;
    using LAB2D.Core;
      using LAB2D.Character.Worker;
      using LAB2D.Domain.Common;
      using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
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
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

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
            // 强制设置热键，防止场景序列化覆盖默认值
            this.toggleKey = WorkerConditionConstant.HudToggleKey;

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

            // 默认隐藏，按 F5 切换显示
            this.canvasGroup.alpha = 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            try
            {
                WorkerConditionManager manager = Core.ServiceLocator.TryGet(out WorkerConditionManager mgr) ? mgr : ServiceLocator.Get<WorkerConditionManager>();
                manager.OnWorkerConditionChanged += this.HandleWorkerConditionChanged;
                this.UpdateDisplay();
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[WorkerConditionHUD] 绑定工人状态数据失败: " + exception.Message);
            }
        }

        private void OnDisable()
        {
            try
            {
                WorkerConditionManager manager = Core.ServiceLocator.TryGet(out WorkerConditionManager mgr) ? mgr : ServiceLocator.Get<WorkerConditionManager>();
                manager.OnWorkerConditionChanged -= this.HandleWorkerConditionChanged;
            }
            catch (Exception ex)
            {
                AWorkerTask.LogProvider($"[UIDiag] WorkerConditionHUD.OnDisable 取消订阅失败（退出期服务已释放，预期内）: {ex.Message}", LogManager.LogLevelEnum.Trace);
            }
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            // 隐藏时（默认隐藏）不构建摘要文本：BuildSummaryText 是全量扫描+大字符串拼接，
            // 不可见时纯属浪费；切回可见时由 SetVisible 置 0 补一次刷新。
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

            string summary = Core.ServiceLocator.Get<WorkerConditionManager>().BuildSummaryText();
            this.conditionText.text = summary + "\n——最近想法——\n" + this.BuildRecentThoughtsText();
        }

        /// <summary>
        /// 拼接「最近想法」区（心智层运行态队列，上限 6，最新在后）。
        /// </summary>
        private string BuildRecentThoughtsText()
        {
            if (!Core.ServiceLocator.TryGet<WorkerMindService>(out WorkerMindService mindService))
            {
                return "（心智层未就绪）";
            }

            List<string> thoughts = mindService.GetRecentThoughts();
            if (thoughts == null || thoughts.Count == 0)
            {
                return "（暂无）";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(128);
            for (int i = 0; i < thoughts.Count; i++)
            {
                builder.Append("· ").AppendLine(thoughts[i]);
            }

            return builder.ToString();
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
        /// 确保运行时存在 WorkerConditionHUD。挂载到 UIRoot/Foreground 下。
        /// </summary>
        public static WorkerConditionHUD EnsureRuntimePanel()
        {
            Transform parent = HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(WorkerConditionConstant.HudRootName);
            if (existingTransform != null)
            {
                WorkerConditionHUD existingHud = existingTransform.GetComponent<WorkerConditionHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, WorkerConditionConstant.HudToggleKey, false);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            GameLoggerFactory.Get().LogWarning($"[WorkerConditionHUD] 在 Foreground 下未找到 {WorkerConditionConstant.HudRootName}，请手动创建。");
            return null;
        }
    }
}
