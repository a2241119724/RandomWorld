namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 常驻提示 HUD — 挂在场景 "TipHUD" 对象上，在其 "Text" 子物体显示最近一条 Tip。
    /// 与 5 秒淡出的 TipUI 弹窗互补：弹窗销毁后最新提示仍留在此处。
    /// 按 P 键切换显示/隐藏。未挂载时 PublishLatest 静默跳过（仅首条 Debug 日志）。
    /// </summary>
    [DisallowMultipleComponent]
    public class TipHUD : MonoBehaviour
    {
        private const string HudTextName = "Text";
        private const int MaxEntries = 14; // 保留的最近 Tip 条数

        private static TipHUD instance;
        private static bool warnedMissing;

        private readonly List<string> entries = new List<string>();

        private CanvasGroup canvasGroup;
        private Text hudText;

        public KeyCode toggleKey = InputKeyConstant.ToggleTipHud;

        /// <summary>
        /// 发布最新一条 Tip 文本到常驻 HUD。
        /// </summary>
        /// <param name="message">Tip 文本。</param>
        public static void PublishLatest(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            // 用 Unity 重载的 == 做存活检查（假 null 场景下 ?. 会穿透调用已销毁对象）
            if (instance == null)
            {
                if (!warnedMissing)
                {
                    warnedMissing = true;
                    AWorkerTask.LogProvider(
                        "[TipHUD] 场景中未挂载 TipHUD 组件，最新 Tip 将不常驻显示",
                        LogManager.LogLevelEnum.Debug);
                }

                return;
            }

            instance.SetText(message);
        }

        public void Awake()
        {
            this.toggleKey = InputKeyConstant.ToggleTipHud; // 防场景序列化残留 None
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.hudText = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, HudTextName);
            instance = this;

            this.SetVisible(true); // 常驻 HUD，默认显示
        }

        public void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleKey))
            {
                this.SetVisible(this.canvasGroup == null || this.canvasGroup.alpha < 0.5f);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                warnedMissing = false; // 切场景重挂后允许再次提示
            }
        }

        private void SetVisible(bool visible)
        {
            if (this.canvasGroup == null) return;
            this.canvasGroup.alpha = visible ? 1.0f : 0.0f;
            this.canvasGroup.interactable = false;
            this.canvasGroup.blocksRaycasts = false;
        }

        private void SetText(string message)
        {
            if (this.hudText == null)
            {
                return;
            }

            // 保留最近 MaxEntries 条，最新在最上一行
            this.entries.Add(message);
            if (this.entries.Count > MaxEntries)
            {
                this.entries.RemoveAt(0);
            }

            StringBuilder builder = new StringBuilder();
            for (int i = this.entries.Count - 1; i >= 0; i--)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(this.entries[i]);
            }

            this.hudText.text = builder.ToString();
        }
    }
}
