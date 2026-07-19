namespace LAB2D.UI
{
    using LAB2D;
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
            this.UpdateDisplay();
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
            return UnityGlobalInputAdapter.CanUseHudHotkey();
        }

        /// <summary>
        /// 确保运行时存在 WorkerTaskQueueHUD。挂载到 UIRoot/Foreground 下。
        /// </summary>
        public static WorkerTaskQueueHUD EnsureRuntimePanel()
        {
            GameObject existing = GameObject.Find(WorkerTaskHudConstant.HudRootName);
            if (existing != null)
            {
                WorkerTaskQueueHUD existingHud = existing.GetComponent<WorkerTaskQueueHUD>();
                if (existingHud != null)
                {
                    HudFactory.RepairExisting(existingHud, WorkerTaskHudConstant.HudToggleKey, true);
                    existingHud.UpdateDisplay();
                    return existingHud;
                }
            }

            Transform parent = HudFactory.FindHudParent();
            GameObject root = CreatePanelRoot(parent);
            WorkerTaskQueueHUD hud = root.GetComponent<WorkerTaskQueueHUD>();
            hud.UpdateDisplay();
            return hud;
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            GameObject root = new GameObject(
                WorkerTaskHudConstant.HudRootName,
                typeof(RectTransform),
                typeof(CanvasGroup));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.0f, 1.0f);
            rootRect.anchorMax = new Vector2(0.0f, 1.0f);
            rootRect.pivot = new Vector2(0.0f, 1.0f);
            rootRect.anchoredPosition = new Vector2(
                WorkerTaskHudConstant.HudAnchoredX,
                WorkerTaskHudConstant.HudAnchoredY);
            rootRect.sizeDelta = new Vector2(
                WorkerTaskHudConstant.HudWidth,
                WorkerTaskHudConstant.HudHeight);

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = PixelUITheme.DialogBoxBg;

            GameObject textObj = new GameObject(
                WorkerTaskHudConstant.HudTextName,
                typeof(RectTransform),
                typeof(Text));
            textObj.transform.SetParent(root.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(
                WorkerTaskHudConstant.HudPaddingX,
                WorkerTaskHudConstant.HudPaddingY);
            textRect.offsetMax = new Vector2(
                -WorkerTaskHudConstant.HudPaddingX,
                -WorkerTaskHudConstant.HudPaddingY);

            Text text = textObj.GetComponent<Text>();
            Font font = Resources.Load<Font>(WorkerConditionConstant.FontResourcePath);
            if (font != null) text.font = font;
            text.fontSize = WorkerTaskHudConstant.HudFontSize;
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.color = PixelUITheme.TextPrimary;
            text.text = WorkerTaskHudConstant.NoTaskText;

            WorkerTaskQueueHUD hud = root.AddComponent<WorkerTaskQueueHUD>();
            hud.queueText = text;
            hud.SetVisible(true);
            return root;
        }
    }
}
