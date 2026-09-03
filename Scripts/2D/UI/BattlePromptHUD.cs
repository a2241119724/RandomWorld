namespace LAB2D.UI
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.Core;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 附近交战提示条 — 玩家进入交战检测半径时屏幕下方中央提示"按 B 加入战斗"。
    /// 纯代码构建（EnsureRuntimePanel 模式），显隐由 GlobalInputProcessor.ProcessJoinBattle 驱动
    /// （自身不轮询：根节点 inactive 时 Update 不跑，显隐判定必须在外部常驻入口做）。
    /// </summary>
    public class BattlePromptHUD : MonoBehaviour
    {
        private const string RootName = "BattlePromptHUD";

        private static BattlePromptHUD instance;

        /// <summary>运行时实例（未创建为 null，调用方用 ?. 安全访问）。</summary>
        public static BattlePromptHUD Instance => instance;

        /// <summary>确保运行时提示条已创建（GlobalPanelInitializer 启动时调用）。</summary>
        public static void EnsureRuntimePanel()
        {
            if (instance != null)
            {
                return;
            }

            Transform uiRoot = GameObject.FindGameObjectWithTag(Constant.TagConstant.UI_TAG)?.transform;
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

            instance = root.GetComponent<BattlePromptHUD>();
            if (instance == null)
            {
                instance = root.AddComponent<BattlePromptHUD>();
            }

            instance.BuildUI();
        }

        private void BuildUI()
        {
            RectTransform rt = this.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            // 与 NearbyItemPickupHUD（贴底左侧列表）错开，抬高避让
            rt.anchoredPosition = new Vector2(0f, 150f);
            rt.sizeDelta = new Vector2(600f, 48f);

            Image bg = this.GetComponent<Image>();
            if (bg == null)
            {
                bg = this.gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
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

            Text label = labelGo.GetComponent<Text>();
            if (label == null)
            {
                label = labelGo.AddComponent<Text>();
            }

            label.text = "⚔ 附近 Worker 正在战斗 — 按 B 加入";
            label.font = UIFontConfig.GetFont();
            label.fontSize = 24;
            label.color = new Color(1f, 0.92f, 0.7f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;

            this.gameObject.SetActive(false);
        }

        /// <summary>显隐切换（由 ProcessJoinBattle 每帧驱动，战斗中/无交战时隐藏）。</summary>
        public void SetVisible(bool visible)
        {
            if (this.gameObject.activeSelf != visible)
            {
                this.gameObject.SetActive(visible);
            }
        }
    }
}
