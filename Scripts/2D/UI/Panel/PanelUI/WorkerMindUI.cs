namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D;
    using LAB2D.Character.Worker;
    using LAB2D.Core;
    using LAB2D.Domain.Gameplay.AwakenedPower;
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.Gameplay.GongFa;
    using LAB2D.Domain.Worker;
    using LAB2D.Gameplay;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GrowthData = LAB2D.Domain.Character.Growth.GrowthData;

    /// <summary>
    /// Worker 心智+修仙可视化 UI — 挂载在 WorkerMindPanel 的 Panel 上，纯代码构建（无 prefab/场景依赖）。
    /// 纯读展示 WorkerData.Mind（信念/意志/记忆/关系/执念）+ Personality + GrowthData（境界/灵根/功法/异能）。
    /// 左列 Worker 选择，右侧详情分区滚动；激活期每 RefreshInterval 秒全量刷新。
    /// </summary>
    public class WorkerMindUI : MonoBehaviour
    {
        /// <summary>详情刷新间隔（秒），避免每帧重建行 UI。</summary>
        private const float RefreshInterval = 1.5f;

        // ---- 骨架引用（Awake 懒构建） ----
        private Transform workerListContent;
        private Transform detailContent;
        private Text emptyHintText;

        // ---- 状态 ----
        private AWorker selectedWorker;
        private float nextRefreshTime;
        private string lastListKey;

        // ---- 颜色 ----
        private static readonly Color TitleColor = new Color(0.95f, 0.83f, 0.35f);
        private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.62f);
        private static readonly Color RowDimColor = new Color(1f, 1f, 1f, 0.40f);
        private static readonly Color SelectedRowColor = new Color(0.35f, 0.55f, 0.90f, 0.85f);
        private static readonly Color BarBgColor = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color WillColor = new Color(0.45f, 0.72f, 0.95f);
        private static readonly Color ResentColor = new Color(0.90f, 0.34f, 0.30f);
        private static readonly Color GratitudeColor = new Color(0.95f, 0.78f, 0.30f);
        private static readonly Color BeliefColor = new Color(0.40f, 0.85f, 0.80f);
        private static readonly Color PersonalityColor = new Color(0.55f, 0.85f, 0.45f);
        private static readonly Color CultivationColor = new Color(0.78f, 0.60f, 0.95f);

        /// <summary>事件类型键 → 短标签（记忆流行展示用，确定性映射避免语料随机闪烁）。</summary>
        private static readonly Dictionary<string, string> EventLabels = new Dictionary<string, string>
        {
            { WorkerMindConstant.EVT_PLAYER_HELP, "被玩家救援" },
            { WorkerMindConstant.EVT_PLAYER_ATTACK, "被玩家攻击" },
            { WorkerMindConstant.EVT_PLAYER_KILL, "目睹同伴被杀" },
            { WorkerMindConstant.EVT_WORKER_ATTACK, "被工友攻击" },
            { WorkerMindConstant.EVT_BOUNTY_COMPLETED, "完成悬赏" },
            { WorkerMindConstant.EVT_BOUNTY_ACCEPTED, "接下悬赏" },
            { WorkerMindConstant.EVT_BOUNTY_REFUSED, "拒绝悬赏" },
            { WorkerMindConstant.EVT_TRADE_SUCCESS, "交易成功" },
            { WorkerMindConstant.EVT_TRADE_REJECTED, "被拒交易" },
            { WorkerMindConstant.EVT_CONVERSATION, "与玩家闲谈" },
            { WorkerMindConstant.EVT_TASK_COMPLETED, "完成任务" },
            { WorkerMindConstant.EVT_STAGE_UP, "进阶" },
            { WorkerMindConstant.EVT_NEAR_DEATH, "濒死经历" },
            { WorkerMindConstant.EVT_GROUND_SLEEP, "露宿地面" },
            { WorkerMindConstant.EVT_FOUND_ITEM, "拾获物品" },
            { WorkerMindConstant.EVT_WIND_FALL, "横财" },
            { WorkerMindConstant.EVT_ILLNESS, "患病" },
            { WorkerMindConstant.EVT_INSIGHT, "领悟" },
            { WorkerMindConstant.EVT_MISFORTUNE, "变故" },
            { WorkerMindConstant.EVT_ENLIGHTENMENT, "顿悟" },
            { WorkerMindConstant.EVT_SMALL_JOY, "小确幸" },
            { WorkerMindConstant.EVT_NIGHTMARE, "梦魇" },
            { WorkerMindConstant.EVT_CULTIVATION_BREAKTHROUGH, "修为突破" },
            { WorkerMindConstant.EVT_POWER_AWAKEN, "异能觉醒" },
            { WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH, "工友突破" },
            { WorkerMindConstant.EVT_FELLOW_BREAKTHROUGH_ENVY, "嫉妒工友突破" },
        };

        /// <summary>关系类型 → 中文标签。</summary>
        private static string GetRelationLabel(RelationKind kind)
        {
            switch (kind)
            {
                case RelationKind.Friendship: return "友谊";
                case RelationKind.Enmity: return "敌意";
                case RelationKind.Admiration: return "爱慕";
                case RelationKind.Grudge: return "记仇";
                default: return "泛泛";
            }
        }

        public void Awake()
        {
            this.BuildUi();
        }

        public void OnEnable()
        {
            this.nextRefreshTime = 0f;
            this.RefreshAll();
        }

        public void Update()
        {
            if (Time.unscaledTime < this.nextRefreshTime)
            {
                return;
            }

            this.nextRefreshTime = Time.unscaledTime + RefreshInterval;
            this.RefreshAll();
        }

        /// <summary>全量刷新：Worker 列表 + 详情。</summary>
        public void RefreshAll()
        {
            if (this.detailContent == null)
            {
                this.BuildUi();
            }

            if (this.detailContent == null)
            {
                return;
            }

            this.RefreshWorkerList();
            this.RefreshDetail();
        }

        // ---- 数据访问 ----

        private static List<AWorker> GetWorkers()
        {
            WorkerManager wm = ServiceLocator.Get<WorkerManager>();
            List<AWorker> workers = new List<AWorker>();
            if (wm?.Characters != null)
            {
                foreach (AWorker w in wm.Characters)
                {
                    if (w != null)
                    {
                        workers.Add(w);
                    }
                }
            }

            return workers;
        }

        private void SelectWorker(AWorker worker)
        {
            this.selectedWorker = worker;
            this.RefreshWorkerList();
            this.RefreshDetail();
        }

        // ---- Worker 列表 ----

        private void RefreshWorkerList()
        {
            if (this.workerListContent == null)
            {
                return;
            }

            List<AWorker> workers = GetWorkers();

            // 列表未变（名字指纹一致）且选中者仍有效 → 只刷新选中高亮，避免整列重建
            string listKey = BuildListKey(workers);
            bool selectedAlive = this.selectedWorker != null && workers.Contains(this.selectedWorker);
            if (listKey == this.lastListKey && selectedAlive)
            {
                this.ApplySelectionHighlight(workers);
                return;
            }

            this.lastListKey = listKey;
            ClearChildren(this.workerListContent);

            if (this.emptyHintText != null)
            {
                this.emptyHintText.gameObject.SetActive(workers.Count == 0);
            }

            if (!selectedAlive)
            {
                this.selectedWorker = workers.Count > 0 ? workers[0] : null;
            }

            foreach (AWorker w in workers)
            {
                this.CreateWorkerRow(w);
            }

            this.ApplySelectionHighlight(workers);
        }

        private static string BuildListKey(List<AWorker> workers)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            foreach (AWorker w in workers)
            {
                sb.Append(w.name).Append('|');
            }

            return sb.ToString();
        }

        private void ApplySelectionHighlight(List<AWorker> workers)
        {
            for (int i = 0; i < this.workerListContent.childCount && i < workers.Count; i++)
            {
                Image bg = this.workerListContent.GetChild(i).GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = workers[i] == this.selectedWorker ? SelectedRowColor : BarBgColor;
                }
            }
        }

        private void CreateWorkerRow(AWorker worker)
        {
            GameObject row = new GameObject("WorkerRow", typeof(RectTransform));
            row.transform.SetParent(this.workerListContent, false);

            Image bg = row.AddComponent<Image>();
            bg.color = BarBgColor;

            Button btn = row.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => this.SelectWorker(worker));

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 34f;
            rowLayout.flexibleWidth = 1f;

            HorizontalOrVerticalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 4, 2, 2);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = CreateText(row.transform, "Name", worker.name, 12, Color.white);
            label.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        // ---- 详情区 ----

        private void RefreshDetail()
        {
            ClearChildren(this.detailContent);
            if (this.selectedWorker == null)
            {
                this.CreateSectionTitle(this.detailContent, "（暂无 Worker）");
                return;
            }

            AWorker.WorkerData wd = this.selectedWorker.CharacterDataLAB as AWorker.WorkerData;
            if (wd == null)
            {
                this.CreateSectionTitle(this.detailContent, "（数据不可用）");
                return;
            }

            WorkerMindData.Ensure(wd);
            GrowthData.Ensure(ref wd.Growth);
            WorkerMindData mind = wd.Mind;

            // ---- 头部 ----
            FavorabilityManager fm = ServiceLocator.Get<FavorabilityManager>();
            string favorText = fm != null
                ? $"好感 {fm.GetFavorabilityWithPlayer(this.selectedWorker):F0}（{fm.GetAttitudeLabel(this.selectedWorker)}）"
                : "好感 未知";
            this.CreateSectionTitle(this.detailContent, $"{this.selectedWorker.name} · {favorText}");

            // ---- 对玩家意志 ----
            this.CreateSectionTitle(this.detailContent, "对玩家");
            this.CreateBarRow(this.detailContent, "服从意愿", mind.WillingnessToObey, WillColor);
            this.CreateBarRow(this.detailContent, "怨恨", mind.ResentmentToPlayer, ResentColor);
            this.CreateBarRow(this.detailContent, "感恩", mind.GratitudeToPlayer, GratitudeColor);
            this.CreateInfoRow(this.detailContent,
                $"接玩家悬赏 {mind.AcceptedPlayerBountyCount} · 拒绝 {mind.RefusedPlayerBountyCount} · 被强制 {mind.ForcedCommandCount}");

            // ---- 信念四轴 ----
            this.CreateSectionTitle(this.detailContent, "信念");
            this.CreateBarRow(this.detailContent, "信任世界", mind.TrustInWorld, BeliefColor);
            this.CreateBarRow(this.detailContent, "信任玩家", mind.TrustInPlayer, BeliefColor);
            this.CreateBarRow(this.detailContent, "自尊", mind.SelfEsteem, BeliefColor);
            this.CreateBarRow(this.detailContent, "归属感", mind.SenseOfBelonging, BeliefColor);

            // ---- 人格 ----
            this.CreateSectionTitle(this.detailContent, "人格");
            this.CreateBarRow(this.detailContent, "心情", wd.Personality.Mood, PersonalityColor);
            this.CreateBarRow(this.detailContent, "事业心", wd.Personality.Ambition, PersonalityColor);
            this.CreateBarRow(this.detailContent, "勤奋", wd.Personality.Diligence, PersonalityColor);
            this.CreateBarRow(this.detailContent, "社交", wd.Personality.Sociality, PersonalityColor);
            this.CreateInfoRow(this.detailContent, $"贪婪 {wd.Greed:F0} · 懒惰 {wd.Laziness:F0}");
            if (!mind.ActiveDream.IsEmpty)
            {
                this.CreateInfoRow(this.detailContent,
                    $"执念：{mind.ActiveDream.Description}（热情 {mind.ActiveDream.Passion:F0}）");
            }

            // ---- 修仙 ----
            this.AppendCultivationSection(wd.Growth);

            // ---- 关系网 ----
            this.CreateSectionTitle(this.detailContent, "关系网");
            if (mind.Relations.Count == 0)
            {
                this.CreateInfoRow(this.detailContent, "（尚无深刻关系）", true);
            }
            else
            {
                foreach (WorkerRelationEntry rel in mind.Relations)
                {
                    this.CreateInfoRow(this.detailContent,
                        $"{rel.TargetName} 【{GetRelationLabel(rel.Kind)}】 亲密{rel.Affinity:F0} 爱慕{rel.AdmirationLevel:F0} 记仇{rel.GrudgeLevel:F0}");
                }
            }

            // ---- 记忆流 ----
            this.CreateSectionTitle(this.detailContent, "记忆流（近 12 条）");
            this.AppendMemorySection(mind);
        }

        private void AppendCultivationSection(GrowthData growth)
        {
            this.CreateSectionTitle(this.detailContent, "修仙");
            bool hasGongFa = growth.LearnedGongFaIds != null && growth.LearnedGongFaIds.Count > 0;
            bool hasPower = growth.AwakenedPowerIds != null && growth.AwakenedPowerIds.Count > 0;
            if (growth.RealmIndex <= 0 && !hasGongFa && !hasPower)
            {
                this.CreateInfoRow(this.detailContent, "未入修炼之门", true);
                return;
            }

            RealmDef realm = RealmRuleService.GetRealm(growth);
            string qiText = RealmLibrary.IsMax(growth.RealmIndex)
                ? $"灵气 {growth.Qi:F0}（巅峰）"
                : $"灵气 {growth.Qi:F0}/{realm.QiToNext:F0}";
            this.CreateInfoRow(this.detailContent, $"{realm.Name} · {qiText}");

            // 灵根
            if (growth.LingGenElements != null && growth.LingGenElements.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(32);
                foreach (int e in growth.LingGenElements)
                {
                    sb.Append(LingGenRuleService.GetElementName((Element)e)).Append(' ');
                }

                this.CreateInfoRow(this.detailContent, $"灵根：{sb.ToString().TrimEnd()}");
            }
            else
            {
                this.CreateInfoRow(this.detailContent, "灵根：无", true);
            }

            // 运转内功
            string neiGongText = string.IsNullOrEmpty(growth.ActiveNeiGongId)
                ? "未运转"
                : GongFaLibrary.Get(growth.ActiveNeiGongId).Name;
            this.CreateInfoRow(this.detailContent, $"运转内功：{neiGongText}");

            // 已学功法
            if (hasGongFa)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(48);
                foreach (string id in growth.LearnedGongFaIds)
                {
                    sb.Append(GongFaLibrary.Get(id).Name).Append(' ');
                }

                this.CreateInfoRow(this.detailContent, $"已学功法：{sb.ToString().TrimEnd()}");
            }

            // 异能
            if (hasPower)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(32);
                foreach (string id in growth.AwakenedPowerIds)
                {
                    sb.Append(AwakenedPowerLibrary.Get(id).Name).Append(' ');
                }

                this.CreateInfoRow(this.detailContent, $"异能：{sb.ToString().TrimEnd()}");
            }
        }

        private void AppendMemorySection(WorkerMindData mind)
        {
            if (mind.Memories.Count == 0)
            {
                this.CreateInfoRow(this.detailContent, "（记忆空白）", true);
                return;
            }

            List<WorkerMemoryEntry> sorted = new List<WorkerMemoryEntry>(mind.Memories);
            sorted.Sort((a, b) => b.Day != a.Day ? b.Day.CompareTo(a.Day) : b.Intensity.CompareTo(a.Intensity));

            int count = Mathf.Min(sorted.Count, 12);
            for (int i = 0; i < count; i++)
            {
                WorkerMemoryEntry m = sorted[i];
                string label = EventLabels.TryGetValue(m.TypeKey ?? string.Empty, out string text) ? text : m.TypeKey;
                string target = string.IsNullOrEmpty(m.TargetName)
                    ? string.Empty
                    : (m.TargetName == WorkerMindService.PlayerTargetName ? "对玩家" : $"对{m.TargetName}");
                this.CreateInfoRow(this.detailContent,
                    $"D{m.Day} · {label}{target} · 强度{m.Intensity:F0}",
                    m.Valence == MemoryValence.Negative);
            }
        }

        // ---- UI 构建 ----

        /// <summary>构建静态骨架：标题/关闭按钮/左列 Worker 滚动列表/右侧详情滚动区。</summary>
        private void BuildUi()
        {
            if (this.detailContent != null)
            {
                return;
            }

            RectTransform root = (RectTransform)this.transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(780f, 560f);

            Image bg = this.gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.96f);

            // 标题
            Text title = CreateText(root, "Title", "心智 · 修仙", 24, TitleColor);
            SetAnchors(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            title.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            title.rectTransform.sizeDelta = new Vector2(-140f, 28f);
            title.alignment = TextAnchor.MiddleLeft;

            // 关闭按钮
            GameObject closeGo = new GameObject("CloseBtn", typeof(RectTransform));
            closeGo.transform.SetParent(root, false);
            SetAnchors((RectTransform)closeGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            ((RectTransform)closeGo.transform).anchoredPosition = new Vector2(-16f, -16f);
            ((RectTransform)closeGo.transform).sizeDelta = new Vector2(72f, 28f);
            Image closeBg = closeGo.AddComponent<Image>();
            closeBg.color = new Color(0.85f, 0.30f, 0.28f, 0.9f);
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            Text closeText = CreateText(closeGo.transform, "Text", "× 关闭", 12, Color.white);
            SetAnchors(closeText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            closeText.rectTransform.offsetMin = Vector2.zero;
            closeText.rectTransform.offsetMax = Vector2.zero;
            closeText.alignment = TextAnchor.MiddleCenter;
            // 点击回调由 WorkerMindPanel.BindUI 统一绑定（走 PanelController 栈关闭）

            // 左列 Worker 列表（x: 10..180）
            this.workerListContent = this.CreateScroll(root, "WorkerList", new Vector2(0f, 0f), new Vector2(0.225f, 1f), 10f, -44f, -10f, -10f);

            // 空态提示
            GameObject hintGo = new GameObject("EmptyHint", typeof(RectTransform));
            hintGo.transform.SetParent(this.workerListContent.parent, false);
            RectTransform hintRect = (RectTransform)hintGo.transform;
            SetAnchors(hintRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 1f));
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            this.emptyHintText = hintGo.AddComponent<Text>();
            this.emptyHintText.text = "暂无 Worker\n（招募后显示）";
            this.emptyHintText.fontSize = 12;
            this.emptyHintText.color = RowDimColor;
            this.emptyHintText.alignment = TextAnchor.MiddleCenter;
            this.emptyHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            this.emptyHintText.verticalOverflow = VerticalWrapMode.Overflow;
            this.emptyHintText.gameObject.SetActive(false);

            // 右侧详情（x: 190..右边界）
            this.detailContent = this.CreateScroll(root, "Detail", new Vector2(0.245f, 0f), new Vector2(1f, 1f), 10f, -44f, -10f, -10f);
        }

        /// <summary>
        /// 创建纯代码 ScrollView，返回 Content Transform（VerticalLayoutGroup 向下生长）。
        /// offsets 顺序：left/top/right/bottom（anchoredPosition 语义，top/bottom 为负）。
        /// </summary>
        private Transform CreateScroll(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            float left, float top, float right, float bottom)
        {
            GameObject scrollGo = new GameObject(name, typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            RectTransform scrollRect = (RectTransform)scrollGo.transform;
            SetAnchors(scrollRect, anchorMin, anchorMax, new Vector2(0.5f, 0.5f));
            scrollRect.offsetMin = new Vector2(left, bottom);
            scrollRect.offsetMax = new Vector2(-right, -top);

            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0.05f);

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRect = (RectTransform)viewportGo.transform;
            SetAnchors(viewportRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = Color.clear;
            viewportGo.AddComponent<RectMask2D>();

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRect = (RectTransform)contentGo.transform;
            SetAnchors(contentRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            return contentGo.transform;
        }

        private void CreateSectionTitle(Transform parent, string text)
        {
            Text t = CreateText(parent, "SectionTitle", text, 12, TitleColor);
            t.alignment = TextAnchor.MiddleLeft;
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 24f;
            le.flexibleWidth = 1f;
        }

        private void CreateInfoRow(Transform parent, string text, bool dim = false)
        {
            Text t = CreateText(parent, "InfoRow", text, 12, dim ? RowDimColor : RowColor);
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 19f;
            le.flexibleWidth = 1f;
        }

        private void CreateBarRow(Transform parent, string label, float value, Color color)
        {
            GameObject row = new GameObject("BarRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 20f;
            rowLayout.flexibleWidth = 1f;

            Text labelText = CreateText(row.transform, "Label", label, 12, RowColor);
            labelText.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = 76f;
            labelLayout.preferredWidth = 76f;

            // 条背景（子物体 Fill 用锚点比例控制填充，不受 Layout 控制）
            GameObject barBg = new GameObject("Bar", typeof(RectTransform));
            barBg.transform.SetParent(row.transform, false);
            Image bgImage = barBg.AddComponent<Image>();
            bgImage.color = BarBgColor;
            LayoutElement barLayout = barBg.AddComponent<LayoutElement>();
            barLayout.minWidth = 170f;
            barLayout.preferredWidth = 170f;
            barLayout.minHeight = 12f;
            barLayout.preferredHeight = 12f;

            RectTransform fillRect = (RectTransform)barBg.transform;
            SetAnchors(fillRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(barBg.transform, false);
            RectTransform fill = (RectTransform)fillGo.transform;
            float ratio = Mathf.Clamp01(value / 100f);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(ratio, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = color;

            Text valueText = CreateText(row.transform, "Value", value.ToString("F0"), 12, RowColor);
            valueText.alignment = TextAnchor.MiddleRight;
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.minWidth = 34f;
            valueLayout.preferredWidth = 34f;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
