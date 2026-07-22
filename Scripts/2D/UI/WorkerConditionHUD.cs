namespace LAB2D.UI
{
      using LAB2D;
      using LAB2D.Character.Worker;
      using LAB2D.Domain.Common;
      using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
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
                Core.ServiceLocator.Get<WorkerConditionManager>().OnWorkerConditionChanged += this.HandleWorkerConditionChanged;
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
                Core.ServiceLocator.Get<WorkerConditionManager>().OnWorkerConditionChanged -= this.HandleWorkerConditionChanged;
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

            this.conditionText.text = Core.ServiceLocator.Get<WorkerConditionManager>().BuildSummaryText();
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
            GameObject existing = GameObject.Find(WorkerConditionConstant.HudRootName);
            if (existing != null)
            {
                WorkerConditionHUD existingHud = existing.GetComponent<WorkerConditionHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, WorkerConditionConstant.HudToggleKey, false);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            Transform parent = HudFactory.FindHudParent();
            GameObject root = CreatePanelRoot(parent);
            WorkerConditionHUD hud = root.GetComponent<WorkerConditionHUD>();
            hud.UpdateDisplay();
            return hud;
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject root = new GameObject(
                WorkerConditionConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(20.0f, -190.0f);
            rootRect.sizeDelta = new Vector2(520.0f, 150.0f);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = PixelUITheme.DialogBoxBg;

            GameObject textObj = new GameObject(
                WorkerConditionConstant.HudTextName,
                typeof(RectTransform),
                typeof(Text));
            textObj.transform.SetParent(root.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12.0f, 8.0f);
            textRect.offsetMax = new Vector2(-12.0f, -8.0f);

            Text text = textObj.GetComponent<Text>();
            Font font = Resources.Load<Font>(WorkerConditionConstant.FontResourcePath);
            if (font != null) text.font = font;
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = WorkerConditionConstant.EmptyHudText;

            WorkerConditionHUD hud = root.AddComponent<WorkerConditionHUD>();
            hud.conditionText = text;
            hud.SetVisible(false);
            return root;
        }
    }
}
