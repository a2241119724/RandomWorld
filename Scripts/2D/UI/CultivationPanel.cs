namespace LAB2D.UI
{
    using LAB2D.Domain.Character.Growth;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Gameplay.GongFa;
    using LAB2D.Gameplay;
    using LAB2D.Tool;
    using LAB2D.UnityAdapter;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCharacter = LAB2D.Character.Character;

    /// <summary>
    /// 修仙面板（K）— 展示灵根/境界/灵气/永久加成，提供打坐与突破按钮。
    /// UI 由场景直接摆放（UI 根下 CultivationPanel 节点），代码只按名绑定子物体并刷新，不再自建。
    /// 数据读取自 CultivationManager 与玩家 GrowthData，不持久化任何状态。
    /// </summary>
    public class CultivationPanel : MonoBehaviour
    {
        /// <summary>面板根节点名（ESC 关闭列表与 EnsureRuntimePanel 共用）。</summary>
        public const string PanelRootName = "CultivationPanel";

        private static CultivationPanel runtimeInstance;

        /// <summary>BuildUI 防重入：Awake 与 EnsureRuntimePanel 可能各调一次。</summary>
        private bool uiBuilt;

        private GameObject rootObj;
        private Text infoText;
        private Button meditateButton;
        private Text meditateButtonText;
        private Button breakthroughButton;
        private Text[] gongFaRowTexts;
        private Button[] gongFaRowButtons;
        private float nextRefreshTime;
        private bool isVisible;

        /// <summary>运行时实例。</summary>
        public static CultivationPanel RuntimeInstance
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

            runtimeInstance = existing.GetComponent<CultivationPanel>();
            if (runtimeInstance == null)
            {
                runtimeInstance = existing.gameObject.AddComponent<CultivationPanel>();
            }

            runtimeInstance.BindSceneUI();
            runtimeInstance.rootObj.SetActive(false);
            runtimeInstance.isVisible = false;
            AWorkerTask.LogProvider(
                "[PanelDiag] CultivationPanel 场景节点绑定完成", LogManager.LogLevelEnum.Debug);
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

            this.infoText = this.transform.Find("Info")?.GetComponent<Text>();
            Transform buttonRow = this.transform.Find("ButtonRow");
            this.meditateButton = buttonRow != null ? buttonRow.Find("MeditateBtn")?.GetComponent<Button>() : null;
            this.meditateButtonText = this.meditateButton != null ? this.meditateButton.GetComponentInChildren<Text>() : null;
            this.breakthroughButton = buttonRow != null ? buttonRow.Find("BreakthroughBtn")?.GetComponent<Button>() : null;

            int gongFaCount = GongFaLibrary.All.Count;
            this.gongFaRowTexts = new Text[gongFaCount];
            this.gongFaRowButtons = new Button[gongFaCount];
            for (int i = 0; i < gongFaCount; i++)
            {
                this.gongFaRowTexts[i] = this.transform.Find("GongFaRow" + i)?.GetComponent<Text>();
                Button rowButton = this.transform.Find("GongFaBtn" + i)?.GetComponent<Button>();
                if (rowButton != null)
                {
                    rowButton.onClick.AddListener(this.OnGongFaClicked);
                }

                this.gongFaRowButtons[i] = rowButton;
            }

            if (this.meditateButton != null)
            {
                this.meditateButton.onClick.AddListener(this.OnMeditateClicked);
            }

            if (this.breakthroughButton != null)
            {
                this.breakthroughButton.onClick.AddListener(this.OnBreakthroughClicked);
            }
        }

        private void OnMeditateClicked()
        {
            CultivationManager.Instance.ToggleMeditate();
            this.RefreshPanel();
        }

        private void OnBreakthroughClicked()
        {
            CultivationManager.Instance.TryBreakthrough();
            this.RefreshPanel();
        }

        /// <summary>功法行按钮回调：未学→学习；已学内功→运转。</summary>
        private void OnGongFaClicked()
        {
            GameObject sender = UnityEngine.EventSystems.EventSystem.current != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
                : null;
            if (sender == null || this.gongFaRowButtons == null)
            {
                return;
            }

            for (int i = 0; i < this.gongFaRowButtons.Length; i++)
            {
                if (this.gongFaRowButtons[i] != null && this.gongFaRowButtons[i].gameObject == sender)
                {
                    GongFaClickedAt(i);
                    break;
                }
            }

            this.RefreshPanel();
        }

        /// <summary>按功法索引执行学习或激活（内功）。</summary>
        private void GongFaClickedAt(int index)
        {
            if (index < 0 || index >= GongFaLibrary.All.Count)
            {
                return;
            }

            GongFaDef def = GongFaLibrary.All[index];
            GongFaManager mgr = GongFaManager.Instance;
            GameCharacter.CharacterData data = CultivationManager.GetPlayerData();
            if (data == null || def == null || mgr == null)
            {
                return;
            }

            if (!mgr.IsLearned(data, def.Id))
            {
                mgr.Learn(def.Id);
            }
            else if (def.IsNeiGong)
            {
                mgr.ActivateNeiGong(def.Id);
            }
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

        /// <summary>按玩家成长数据刷新全部文本与按钮状态。</summary>
        public void RefreshPanel()
        {
            GameCharacter.CharacterData data = CultivationManager.GetPlayerData();
            if (this.infoText == null)
            {
                return;
            }

            if (data == null)
            {
                this.infoText.text = "未找到玩家数据";
                if (this.meditateButtonText != null)
                {
                    this.meditateButtonText.text = "开始打坐";
                }

                if (this.breakthroughButton != null)
                {
                    this.breakthroughButton.interactable = false;
                }

                return;
            }

            GrowthData.Ensure(ref data.Growth);
            RealmDef realm = RealmRuleService.GetRealm(data.Growth);
            bool isMax = RealmLibrary.IsMax(data.Growth.RealmIndex);

            string lingGenText = "无（尚未感知）";
            if (data.Growth.LingGenElements.Count > 0)
            {
                lingGenText = string.Join("、",
                    data.Growth.LingGenElements.ConvertAll(e => LingGenRuleService.GetElementName((Element)e)).ToArray());
            }

            string qiLine = isMax
                ? $"灵气: {data.Growth.Qi:F0}（已至巅峰）"
                : $"灵气: {data.Growth.Qi:F0} / {realm.QiToNext:F0}";

            // M4 灵气环境：速率随玩家位置浓度浮动（地形×灵脉×聚灵阵×天气），RefreshPanel 周期刷新
            float envMultiplier = EnvironmentManager.Instance.CurDensity / 100f;
            float speedMul = 1f + data.Growth.Special.CultivationSpeedMul + TechManager.Instance.GetMeditateSpeedBonus();
            float qiRate = RealmRuleService.MeditateQiPerSec * speedMul * envMultiplier;
            string meditateLine = CultivationManager.Instance.IsMeditating
                ? $"<color=#8cff8c>【打坐中】灵气 +{qiRate:F1}/秒（环境 ×{envMultiplier:F2}），法力 +2/秒</color>"
                : "未打坐（打坐中受击或移动会打断）";

            string powerLine = "异能: 无（濒死受击时可能觉醒）";
            if (data.Growth.AwakenedPowerIds.Count > 0)
            {
                powerLine = "异能: " + string.Join("、",
                    data.Growth.AwakenedPowerIds.ConvertAll(id =>
                        LAB2D.Domain.Gameplay.AwakenedPower.AwakenedPowerLibrary.Get(id)?.Name ?? id).ToArray());
            }

            this.infoText.text =
                $"灵根: {lingGenText}\n" +
                $"境界: {realm.Name}{(isMax ? "（巅峰）" : "")}\n" +
                $"{qiLine}\n" +
                $"修炼速度加成: +{data.Growth.Special.CultivationSpeedMul:P0}\n" +
                $"境界永久加成: {FormatBonus(data.Growth.PermanentRealmBonus)}\n" +
                $"{powerLine}\n" +
                meditateLine;

            if (this.meditateButtonText != null)
            {
                this.meditateButtonText.text = CultivationManager.Instance.IsMeditating ? "停止打坐" : "开始打坐";
            }

            if (this.breakthroughButton != null)
            {
                this.breakthroughButton.interactable = RealmRuleService.CanBreakthrough(data.Growth);
            }

            this.RefreshGongFaRows(data);
        }

        /// <summary>刷新功法行：名称/元素/境界要求/状态文本与按钮（学习/运转/已完成）。</summary>
        private void RefreshGongFaRows(GameCharacter.CharacterData data)
        {
            if (this.gongFaRowTexts == null)
            {
                return;
            }

            GongFaManager mgr = GongFaManager.Instance;
            for (int i = 0; i < GongFaLibrary.All.Count; i++)
            {
                if (i >= this.gongFaRowTexts.Length || this.gongFaRowTexts[i] == null)
                {
                    continue;
                }

                GongFaDef def = GongFaLibrary.All[i];
                bool learned = mgr != null && mgr.IsLearned(data, def.Id);
                bool realmOk = data.Growth.RealmIndex >= def.RequiredRealmIndex;
                string kind = def.IsNeiGong ? "内功" : "外功";

                string stateText;
                UnityEngine.Color color;
                if (!learned)
                {
                    stateText = realmOk ? "可学" : $"需{RealmLibrary.Get(def.RequiredRealmIndex).Name}";
                    color = realmOk ? new UnityEngine.Color(0.6f, 1f, 0.6f) : new UnityEngine.Color(1f, 0.7f, 0.5f);
                }
                else if (def.IsNeiGong && data.Growth.ActiveNeiGongId == def.Id)
                {
                    stateText = "运转中";
                    color = new UnityEngine.Color(0.5f, 1f, 0.5f);
                }
                else
                {
                    stateText = "已学";
                    color = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
                }

                this.gongFaRowTexts[i].text = $"{def.Name}（{kind}·{LingGenRuleService.GetElementName(def.Element)}） {stateText}";
                this.gongFaRowTexts[i].color = color;

                if (this.gongFaRowButtons[i] != null)
                {
                    Button btn = this.gongFaRowButtons[i];
                    Text label = btn.GetComponentInChildren<Text>();
                    if (!learned)
                    {
                        btn.interactable = realmOk;
                        if (label != null)
                        {
                            label.text = "学习";
                        }
                    }
                    else if (def.IsNeiGong)
                    {
                        bool active = data.Growth.ActiveNeiGongId == def.Id;
                        btn.interactable = !active;
                        if (label != null)
                        {
                            label.text = active ? "运转中" : "运转";
                        }
                    }
                    else
                    {
                        btn.interactable = false;
                        if (label != null)
                        {
                            label.text = "已学";
                        }
                    }
                }
            }
        }

        /// <summary>格式化永久加成为非零维度列表（如 "ATN+12 DEF+6 MaxHp+50"）。</summary>
        private static string FormatBonus(GrowthBonus bonus)
        {
            BattleStats s = bonus.Stats;
            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
            if (s.ATN > 0.001f) parts.Add("ATN+" + s.ATN.ToString("F0"));
            if (s.INT > 0.001f) parts.Add("INT+" + s.INT.ToString("F0"));
            if (s.DEF > 0.001f) parts.Add("DEF+" + s.DEF.ToString("F0"));
            if (s.RES > 0.001f) parts.Add("RES+" + s.RES.ToString("F0"));
            if (s.SPD > 0.001f) parts.Add("SPD+" + s.SPD.ToString("F0"));
            if (s.HIT > 0.001f) parts.Add("HIT+" + s.HIT.ToString("F0"));
            if (bonus.MaxHpFlat > 0.001f) parts.Add("MaxHp+" + bonus.MaxHpFlat.ToString("F0"));

            return parts.Count > 0 ? string.Join(" ", parts.ToArray()) : "无";
        }
    }
}
