namespace LAB2D.UI
{
    using LAB2D.Domain.Tech;
    using LAB2D.Gameplay;
    using LAB2D.Tool;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 科技面板（T）— 展示研究点/研究台数量/已建成聚灵阵，提供科技研究按钮。
    /// UI 由场景直接摆放（UI 根下 TechPanel 节点），代码只按名绑定子物体并刷新，不再自建。
    /// 数据读取自 TechManager（存档由 ArchiveManager 驱动），不持久化任何状态。
    /// </summary>
    public class TechPanel : MonoBehaviour
    {
        /// <summary>面板根节点名（ESC 关闭列表与 EnsureRuntimePanel 共用）。</summary>
        public const string PanelRootName = "TechPanel";

        private static TechPanel runtimeInstance;

        /// <summary>BuildUI 防重入：Awake 与 EnsureRuntimePanel 可能各调一次。</summary>
        private bool uiBuilt;

        private GameObject rootObj;
        private Text infoText;
        private Text[] techRowTexts;
        private Button[] techRowButtons;
        private float nextRefreshTime;
        private bool isVisible;

        /// <summary>运行时实例。</summary>
        public static TechPanel RuntimeInstance
        {
            get { return runtimeInstance; }
        }

        /// <summary>
        /// 绑定场景中的面板节点：UI 根下按名查找，缺失时 Error 提示（代码不自建，UI 由场景摆放）。
        /// </summary>
        public static void EnsureRuntimePanel()
        {
            if (runtimeInstance != null)
            {
                return;
            }

            Transform uiRoot = HudFactory.FindUiRoot();
            Transform existing = uiRoot != null ? uiRoot.Find(PanelRootName) : null;
            if (existing == null)
            {
                // 不在 UIRoot 直接下时全局按名兜底（拷贝/挪动位置不影响）
                GameObject found = GameObject.Find(PanelRootName);
                existing = found != null ? found.transform : null;
            }

            if (existing == null)
            {
                AWorkerTask.LogProvider(
                    $"[PanelDiag] {PanelRootName} 场景节点缺失：请在 Game.unity 的 UI 节点下摆放（代码不自建）",
                    LogManager.LogLevelEnum.Error);
                return;
            }

            runtimeInstance = existing.GetComponent<TechPanel>();
            if (runtimeInstance == null)
            {
                runtimeInstance = existing.gameObject.AddComponent<TechPanel>();
            }

            runtimeInstance.BindSceneUI();
            runtimeInstance.rootObj.SetActive(false);
            runtimeInstance.isVisible = false;
            AWorkerTask.LogProvider(
                "[PanelDiag] TechPanel 场景节点绑定完成", LogManager.LogLevelEnum.Debug);
        }

        private void Awake()
        {
            if (!this.uiBuilt)
            {
                this.BindSceneUI();
            }
        }

        /// <summary>绑定场景 UI：按名查找子物体回填字段引用并挂按钮监听（防重入）。</summary>
        private void BindSceneUI()
        {
            if (this.uiBuilt)
            {
                return;
            }

            this.uiBuilt = true;
            this.rootObj = this.gameObject;

            // 内容都挂在 Inset 中间层下（四边内缩 24px 让内容落在 9-slice 边框内）
            this.infoText = this.transform.Find("Inset/Info")?.GetComponent<Text>();
            int techCount = TechLibrary.All.Count;
            this.techRowTexts = new Text[techCount];
            this.techRowButtons = new Button[techCount];
            for (int i = 0; i < techCount; i++)
            {
                this.techRowTexts[i] = this.transform.Find("Inset/TechRow" + i)?.GetComponent<Text>();
                Button rowButton = this.transform.Find("Inset/TechBtn" + i)?.GetComponent<Button>();
                if (rowButton != null)
                {
                    rowButton.onClick.AddListener(this.OnTechClicked);
                }

                this.techRowButtons[i] = rowButton;
            }
        }

        /// <summary>科技行按钮回调：研究对应科技。</summary>
        private void OnTechClicked()
        {
            GameObject sender = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
                : null;
            if (sender == null || this.techRowButtons == null)
            {
                return;
            }

            for (int i = 0; i < this.techRowButtons.Length; i++)
            {
                if (this.techRowButtons[i] != null && this.techRowButtons[i].gameObject == sender)
                {
                    if (i >= 0 && i < TechLibrary.All.Count)
                    {
                        TechManager.Instance.Research(TechLibrary.All[i].Id);
                    }

                    break;
                }
            }

            this.RefreshPanel();
        }

        private void Update()
        {
            // 热键检测在 GlobalInit.Update 统一分发——本面板 GameObject 默认 inactive，
            // inactive 时 Update 不跑，面板自身检测会导致热键永远无法唤醒面板
            if (this.isVisible && Time.unscaledTime >= this.nextRefreshTime)
            {
                this.nextRefreshTime = Time.unscaledTime + 0.5f;
                this.RefreshPanel();
            }
        }

        /// <summary>
        /// 热键入口（GlobalInit.Update 分发，本类 GameObject inactive 时也可达）。
        /// 以节点实际激活态为准——ESC 关闭列表直接 SetActive(false)，不同步 isVisible。
        /// </summary>
        internal static void ToggleHotkey()
        {
            if (runtimeInstance == null)
            {
                return;
            }

            bool show = !runtimeInstance.rootObj.activeSelf;
            runtimeInstance.rootObj.SetActive(show);
            runtimeInstance.isVisible = show;
            if (show)
            {
                runtimeInstance.RefreshPanel();
            }
        }

        /// <summary>切换面板显隐。</summary>
        public void TogglePanel()
        {
            if (this.isVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        /// <summary>显示面板并立即刷新。</summary>
        public void Show()
        {
            if (!this.isVisible)
            {
                this.rootObj.SetActive(true);
                this.isVisible = true;
                this.RefreshPanel();
            }
        }

        /// <summary>隐藏面板。</summary>
        public void Hide()
        {
            if (this.isVisible)
            {
                this.rootObj.SetActive(false);
                this.isVisible = false;
            }
        }

        /// <summary>按 TechManager 状态刷新全部文本与按钮。</summary>
        public void RefreshPanel()
        {
            if (this.infoText == null)
            {
                return;
            }

            TechManager mgr = TechManager.Instance;
            mgr.RescanBuildings();

            string researchTableLine = mgr.ResearchTableCount > 0
                ? $"研究台: {mgr.ResearchTableCount} 座（产出 {mgr.ResearchTableCount * (1f + mgr.GetResearchSpeedBonus()):F1} 点/分）"
                : "研究台: 0 座（建造研究台后开始产出研究点）";

            // M4 起聚灵阵效果由 LingQiManager 提供（半径 4 格内浓度 ×1.3，多阵指数叠至 3 层），
            // 科技只解锁建造（GetMeditateSpeedBonus 恒 0，不再显示百分比）
            int arrayCount = LingQiManager.Instance.SpiritArrayCount;
            string spiritArrayLine = arrayCount > 0
                ? $"<color=#8cff8c>聚灵阵: {arrayCount} 座（半径 4 格内灵气 ×1.3，可叠至 3 层）</color>"
                : "聚灵阵: 0 座（建阵提升周边修炼灵气）";

            this.infoText.text =
                $"研究点: {mgr.ResearchPoints:F0}\n" +
                $"{researchTableLine}\n" +
                $"{spiritArrayLine}";

            for (int i = 0; i < TechLibrary.All.Count; i++)
            {
                if (i >= this.techRowTexts.Length || this.techRowTexts[i] == null)
                {
                    continue;
                }

                TechDef def = TechLibrary.All[i];
                bool researched = mgr.IsResearched(def.Id);
                bool canResearch = TechRuleService.CanResearch(researched, mgr.ResearchPoints, def);

                string stateText;
                Color color;
                if (researched)
                {
                    stateText = "已研究";
                    color = new Color(0.5f, 1f, 0.5f);
                }
                else if (mgr.ResearchPoints >= def.Cost)
                {
                    stateText = "可研究";
                    color = new Color(0.6f, 1f, 0.6f);
                }
                else
                {
                    stateText = $"需 {def.Cost:F0} 点";
                    color = new Color(1f, 0.7f, 0.5f);
                }

                this.techRowTexts[i].text = $"{def.Name}（{def.Cost:F0}点） {def.Description}\n    {stateText}";
                this.techRowTexts[i].color = color;

                if (this.techRowButtons[i] != null)
                {
                    Button btn = this.techRowButtons[i];
                    btn.interactable = canResearch;
                    Text label = btn.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = researched ? "已完成" : "研究";
                    }
                }
            }
        }
    }
}
