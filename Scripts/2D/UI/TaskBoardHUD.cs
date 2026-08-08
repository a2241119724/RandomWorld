namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 任务栏列表 HUD — 按数字6键切换显示。
    /// 展示任务栏中存放的物品（按物主分组）。
    /// </summary>
    [DisallowMultipleComponent]
    public class TaskBoardHUD : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private Text boardText;
        private float nextRefreshTime;
        private const float RefreshInterval = 0.5f;
        private const string HudRootName = "TaskBoardHUD";
        private const string HudTextName = "TaskBoardText";

        public KeyCode toggleKey = InputKeyConstant.ToggleTaskBoardHud;

        private void Awake()
        {
            this.toggleKey = InputKeyConstant.ToggleTaskBoardHud;
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            if (this.boardText == null)
            {
                this.boardText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, HudTextName);
            }
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                bool visible = this.canvasGroup == null || this.canvasGroup.alpha < 0.5f;
                this.SetVisible(visible);
            }

            if (this.IsVisible() && Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + RefreshInterval;
                this.UpdateDisplay();
            }
        }

        private bool IsVisible()
        {
            return this.canvasGroup != null && this.canvasGroup.alpha >= 0.5f;
        }

        private void SetVisible(bool visible)
        {
            if (this.canvasGroup == null) return;
            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        private void UpdateDisplay()
        {
            if (this.boardText == null) return;

            TaskBoardManager board = Core.ServiceLocator.Get<TaskBoardManager>();
            if (board == null || !board.IsInitialized)
            {
                this.boardText.text = "任务栏未初始化";
                return;
            }

            string displayText = board.GetDisplayText();
            this.boardText.text = string.IsNullOrEmpty(displayText) ? "任务栏为空" : displayText;
        }

        /// <summary>
        /// 确保运行时存在 TaskBoardHUD。
        /// </summary>
        public static TaskBoardHUD EnsureRuntimePanel()
        {
            Transform parent = Tool.HudFactory.FindHudParent();
            Transform existingTransform = parent?.Find(HudRootName);
            if (existingTransform != null)
            {
                TaskBoardHUD existingHud = existingTransform.GetComponent<TaskBoardHUD>();
                if (existingHud != null)
                {
                    Tool.HudFactory.RepairExisting(existingHud, InputKeyConstant.ToggleTaskBoardHud, true);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            Debug.LogWarning($"[TaskBoardHUD] 在 Foreground 下未找到 {HudRootName}，请在 Game.unity 中手动创建。");
            return null;
        }
    }
}
