namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.Constant;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 上古洞府探索提示条 — 玩家在已揭示洞府旁时屏幕下方中央提示"按 N 亲自探索 / 按 O 派人"；
    /// 玩家读条期间切换为进度显示。纯代码构建（EnsureRuntimePanel 模式），显隐与进度
    /// 由 GlobalInputProcessor.ProcessCaveExplore 每帧驱动（自身不轮询：根节点 inactive
    /// 时 Update 不跑，判定必须在外部常驻入口做）。与 BattlePromptHUD（150 高）错层抬高。
    /// </summary>
    public class CaveExploreHUD : MonoBehaviour
    {
        private const string RootName = "CaveExploreHUD";

        private static CaveExploreHUD instance;

        private Text label;

        /// <summary>运行时实例（未创建为 null，调用方用 ?. 安全访问）。</summary>
        public static CaveExploreHUD Instance => instance;

        /// <summary>确保运行时提示条已创建（GlobalPanelInitializer 启动时调用）。</summary>
        public static void EnsureRuntimePanel()
        {
            if (instance != null)
            {
                return;
            }

            Transform uiRoot = GameObject.FindGameObjectWithTag(TagConstant.UI_TAG)?.transform;
            if (uiRoot == null)
            {
                return;
            }

            Transform existing = uiRoot.Find(RootName);
            GameObject root = existing != null ? existing.gameObject : new GameObject(RootName, typeof(RectTransform));
            if (existing == null)
            {
                root.transform.SetParent(uiRoot, false);
            }

            instance = root.GetComponent<CaveExploreHUD>();
            if (instance == null)
            {
                instance = root.AddComponent<CaveExploreHUD>();
            }

            instance.BuildUI();
        }

        private void BuildUI()
        {
            RectTransform rt = this.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            // 与 BattlePromptHUD（150 高）错开，再抬一层
            rt.anchoredPosition = new Vector2(0f, 210f);
            rt.sizeDelta = new Vector2(640f, 44f);

            Image bg = this.GetComponent<Image>();
            if (bg == null)
            {
                bg = this.gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.10f, 0.08f, 0.06f, 0.85f);
            bg.raycastTarget = false;

            Transform labelTransform = this.transform.Find("PromptLabel");
            GameObject labelGo = labelTransform != null ? labelTransform.gameObject : new GameObject("PromptLabel");
            if (labelTransform == null)
            {
                labelGo.transform.SetParent(this.transform, false);
            }

            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            if (labelRt == null)
            {
                labelRt = labelGo.AddComponent<RectTransform>();
            }

            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            this.label = labelGo.GetComponent<Text>();
            if (this.label == null)
            {
                this.label = labelGo.AddComponent<Text>();
            }

            this.label.text = "⛩ 上古洞府 — 按 N 亲自探索 / 按 O 派人探索";
            this.label.font = UIFontConfig.GetFont();
            this.label.fontSize = 22;
            this.label.color = new Color(0.95f, 0.85f, 0.6f);
            this.label.alignment = TextAnchor.MiddleCenter;
            this.label.raycastTarget = false;

            this.gameObject.SetActive(false);
        }

        /// <summary>显隐切换（由 ProcessCaveExplore 每帧驱动）。</summary>
        public void SetVisible(bool visible)
        {
            if (this.gameObject.activeSelf != visible)
            {
                this.gameObject.SetActive(visible);
            }
        }

        /// <summary>读条模式：显示探索进度（progress ∈ [0,1]）。</summary>
        public void SetExploring(float progress)
        {
            this.SetVisible(true);
            this.label.text = $"⛏ 探索上古洞府中… {Mathf.Clamp01(progress):P0}（移动/受击会打断）";
        }

        /// <summary>恢复默认提示文本（读条结束/打断后由下一帧 SetVisible 流程重写）。</summary>
        public void SetIdleText()
        {
            this.label.text = "⛩ 上古洞府 — 按 N 亲自探索 / 按 O 派人探索";
        }
    }
}
