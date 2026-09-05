namespace LAB2D.UI.Panel.PanelUI
{
    using LAB2D.Domain.Gameplay.Cultivation;
    using LAB2D.Domain.TurnBattle;
    using LAB2D.Gameplay.TurnBattle;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 回合制战斗 UI — 卡片舞台 + 行动菜单 + 演出队列。
    /// 静态舞台（Shade/卡区/日志/回合头/子菜单/主菜单/横幅）已在场景搭建，BuildOnce 仅绑定引用；
    /// 单位卡片/子菜单项/飘字等随战斗变化的内容仍由代码创建。
    /// 演出用 unscaledDeltaTime 驱动（面板推栈冻结大世界 timeScale=0，演出照常）。
    /// 流转：OnAwaitPlayerAction 启用菜单 → 玩家提交 → PlayResolutions 逐段演出 →
    /// Manager.NotifyPresentationFinished → 下一回合；PlayBattleFinished 横幅后回调关面板。
    /// </summary>
    public class TurnBattleUI : MonoBehaviour
    {
        // ===== 布局常量 =====
        private const float CardWidth = 180f;
        private const float CardHeight = 230f;
        private const float CardSpacing = 16f;

        /// <summary>顶部回合顺序预览条槽位数。</summary>
        private const int TurnOrderSlotCount = 5;

        // ===== 配色 =====
        private static readonly Color AllyCardBg = new Color(0.14f, 0.22f, 0.15f, 0.92f);
        private static readonly Color EnemyCardBg = new Color(0.28f, 0.13f, 0.13f, 0.92f);
        private static readonly Color CardFlashColor = new Color(1f, 0.45f, 0.35f, 0.95f);
        private static readonly Color TargetHighlightColor = new Color(0.95f, 0.8f, 0.25f, 0.95f);
        private static readonly Color HpColor = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color MpColor = new Color(0.2f, 0.4f, 0.9f);
        private static readonly Color BarBgColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        private static readonly Color MenuBgColor = new Color(0.16f, 0.16f, 0.2f, 0.95f);
        private static readonly Color DamageColor = new Color(1f, 0.85f, 0.85f);
        private static readonly Color CriticalColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color HealColor = new Color(0.4f, 1f, 0.45f);
        private static readonly Color MissColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color BuffColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color DownColor = new Color(1f, 0.3f, 0.3f);
        private static readonly Color StunColor = new Color(0.95f, 0.85f, 0.35f);
        private static readonly Color DefendColor = new Color(0.55f, 0.75f, 1f);
        private static readonly Color DefendBgColor = new Color(0.22f, 0.34f, 0.55f, 0.92f);
        private static readonly Color AllySlotColor = new Color(0.2f, 0.38f, 0.28f, 0.9f);
        private static readonly Color EnemySlotColor = new Color(0.42f, 0.2f, 0.2f, 0.9f);
        private static readonly Color CurrentSlotColor = new Color(0.9f, 0.75f, 0.25f, 0.95f);

        // 血条/蓝条 Fill 的纯白 Sprite — Image 在 sprite==null 时 OnPopulateMesh 走基础满矩形
        // 路径，type=Filled 与 fillAmount 完全无效（条永远满格），必须挂 sprite 才能按量裁剪；
        // 纯白 4×4 程序化生成（同 ShadowTextureFactory 惯例），染色调色靠 Image.color。
        private static Sprite whiteSprite;

        /// <summary>单位卡片 — 演出（前冲/白闪/置灰）与刷新的最小句柄。</summary>
        private class UnitCard
        {
            public long UnitId;
            public TurnBattleFaction Faction;
            public Transform Root;
            public Image Bg;
            public Text NameText;
            public string NameBase = string.Empty;
            public Image HpFill;
            public Image MpFill;
            public Text HpText;
            public Transform StatusRow;
            public CanvasGroup Group;
            public Vector3 HomePos;
            public Color BaseBgColor;
            public float FlashTimer;
            public bool Down;

            public void Flash()
            {
                this.FlashTimer = 0.12f;
                if (this.Bg != null)
                {
                    this.Bg.color = CardFlashColor;
                }
            }
        }

        // ===== 战斗状态 =====
        private TurnBattleState state;
        private readonly Dictionary<long, UnitCard> cards = new Dictionary<long, UnitCard>();
        private readonly Queue<string> logs = new Queue<string>();

        // ===== 控件引用 =====
        private bool built;
        private Transform enemyCardsRoot;
        private Transform allyCardsRoot;
        private Text headerText;
        private Text logText;
        private GameObject subMenuRoot;
        private Transform subMenuContainer;
        private CanvasGroup menuGroup;
        private GameObject bannerRoot;
        private Text bannerText;
        private Transform turnOrderBar;

        // ===== 菜单状态 =====
        private bool subMenuActive;
        private bool targeting;
        private TurnBattleActionKind pendingKind;
        private int pendingSkillIndex;

        // ===== 演出队列 =====
        private readonly List<TurnBattleEffectEntry> playQueue = new List<TurnBattleEffectEntry>();
        private int playIndex;
        private float segTimer;
        private float segDuration = 0.55f;
        private bool playing;
        private bool impactApplied;
        private bool finishedBannerPlaying;

        // ===== 横幅 =====
        private float bannerTimer;
        private bool bannerActive;
        private System.Action bannerCallback;

        #region 绑定与刷新

        /// <summary>战斗开始 — 构建双方卡片（Manager.BattleStarted → Panel.Show 后首个调用）。</summary>
        public void BindBattle(TurnBattleState battleState)
        {
            this.state = battleState;
            this.BuildOnce();

            this.ClearCards();
            int enemyIndex = 0;
            int allyIndex = 0;
            foreach (TurnBattleUnit unit in battleState.Units)
            {
                if (unit.Faction == TurnBattleFaction.Enemy)
                {
                    this.CreateCard(unit, this.enemyCardsRoot, new Vector3(-120f - (enemyIndex * (CardWidth + CardSpacing)), -160f, 0f));
                    enemyIndex++;
                }
                else
                {
                    this.CreateCard(unit, this.allyCardsRoot, new Vector3(120f + (allyIndex * (CardWidth + CardSpacing)), 210f, 0f));
                    allyIndex++;
                }
            }

            this.logs.Clear();
            this.AppendLog($"遭遇战开始：我方 {battleState.AliveAllies.Count} 人 vs 敌方 {battleState.AliveEnemies.Count} 人");
            this.headerText.text = $"⚔ 回合制战斗 — 第 {battleState.Round} 回合";
            this.SetMenuEnabled(false);
            this.HideSubMenu();
            this.CancelTargeting();
            this.RefreshTurnOrderBar();
        }

        /// <summary>等待玩家行动 — 刷新卡片数据并启用菜单。</summary>
        public void OnAwaitPlayerAction(TurnBattleState battleState)
        {
            if (this.state == null)
            {
                return;
            }

            this.RefreshCards();
            this.headerText.text = $"⚔ 回合制战斗 — 第 {battleState.Round} 回合";
            this.SetMenuEnabled(true);
            this.RefreshTurnOrderBar();
        }

        /// <summary>回合结算 — 展平效果队列开始演出（unscaled 计时）。</summary>
        public void PlayResolutions(IReadOnlyList<TurnBattleActionResolution> resolutions)
        {
            if (this.state == null)
            {
                return;
            }

            this.SetMenuEnabled(false);
            this.HideSubMenu();
            this.CancelTargeting();

            this.playQueue.Clear();
            foreach (TurnBattleActionResolution resolution in resolutions)
            {
                this.playQueue.AddRange(resolution.Effects);
            }

            if (this.playQueue.Count == 0)
            {
                // 无效果的回合（如全员倒下）直接推进
                TurnBattleManager.Instance.NotifyPresentationFinished();
                return;
            }

            this.playIndex = 0;
            this.segTimer = 0f;
            this.segDuration = SegmentDurationOf(this.playQueue[0].Kind);
            this.impactApplied = false;
            this.playing = true;
        }

        /// <summary>演出段时长分档：重打击 0.55s / 治疗与 DOT 跳动及击倒 0.45s / 轻提示（闪避/眩晕/防御等）0.4s。</summary>
        private static float SegmentDurationOf(TurnBattleEffectKind kind)
        {
            switch (kind)
            {
                case TurnBattleEffectKind.Damage:
                case TurnBattleEffectKind.StatusApply:
                    return 0.55f;
                case TurnBattleEffectKind.Heal:
                case TurnBattleEffectKind.StatusTick:
                case TurnBattleEffectKind.Down:
                    return 0.45f;
                default:
                    return 0.4f;
            }
        }

        /// <summary>战斗结束横幅 — 演完回调 onDone（Panel 此时 Close 恢复 timeScale）。</summary>
        public void PlayBattleFinished(TurnBattleResult result, System.Action onDone)
        {
            this.finishedBannerPlaying = true;
            this.SetMenuEnabled(false);
            this.HideSubMenu();
            this.CancelTargeting();

            string text;
            if (result == TurnBattleResult.Victory)
            {
                text = "⚔ 战斗胜利！";
            }
            else if (result == TurnBattleResult.Defeat)
            {
                text = "☠ 战败……";
            }
            else
            {
                text = "🏃 逃跑成功";
            }

            this.ShowBanner(text, 1.4f, onDone);
        }

        /// <summary>面板关闭 — 清演出队列与卡片（异常关闭路径也走这里）。</summary>
        public void ClearBattle()
        {
            this.playQueue.Clear();
            this.playing = false;
            this.finishedBannerPlaying = false;
            this.HideBanner();
            this.HideSubMenu();
            this.CancelTargeting();
            this.ClearCards();
            this.ClearTurnOrderBar();
            this.logs.Clear();
            if (this.logText != null)
            {
                this.logText.text = string.Empty;
            }

            this.state = null;
        }

        /// <summary>ESC 层次取消：点选目标 → 子菜单/逃跑确认 → 逃跑确认框。</summary>
        public void HandleBack()
        {
            if (this.targeting)
            {
                this.CancelTargeting();
                return;
            }

            if (this.subMenuActive)
            {
                this.HideSubMenu();
                return;
            }

            if (this.CanUseMenu())
            {
                this.ShowFleeConfirm();
            }
        }

        #endregion

        #region Unity 生命周期

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (this.playing)
            {
                this.StepPlay(dt);
            }

            this.StepFlash(dt);
            this.StepBanner(dt);
            this.ProcessHotkeys();

            if (this.targeting && Input.GetMouseButtonDown(1))
            {
                this.CancelTargeting();
            }
        }

        #endregion

        #region 构建控件

        /// <summary>
        /// 一次性绑定场景已搭建的静态舞台（Shade/卡区/日志/回合头/子菜单/主菜单/横幅，
        /// 节点结构见 Game.unity 的 TurnBattlePanel 子树；缺失节点打 [PanelDiag] 警告）。
        /// </summary>
        private void BuildOnce()
        {
            if (this.built)
            {
                return;
            }

            this.built = true;

            Transform root = this.transform;
            this.enemyCardsRoot = root.Find("EnemyCards");
            this.allyCardsRoot = root.Find("AllyCards");
            this.logText = root.Find("LogText")?.GetComponent<Text>();
            this.headerText = root.Find("Header")?.GetComponent<Text>();
            this.subMenuRoot = root.Find("SubMenu")?.gameObject;
            this.subMenuContainer = this.subMenuRoot != null ? this.subMenuRoot.transform : null;
            this.bannerRoot = root.Find("Banner")?.gameObject;
            this.bannerText = this.bannerRoot != null
                ? this.bannerRoot.transform.Find("BannerLabel")?.GetComponent<Text>()
                : null;

            // 主菜单：绑定场景四按钮（攻击/技能/道具/逃跑），CanvasGroup 整体启停
            Transform actionBar = root.Find("ActionBar");
            this.menuGroup = actionBar?.GetComponent<CanvasGroup>();
            List<string> missing = new List<string>();
            if (!this.BindMenuButton(actionBar, "AttackBtn", this.OnClickAttack))
            {
                missing.Add("ActionBar/AttackBtn");
            }

            if (!this.BindMenuButton(actionBar, "SkillBtn", this.OnClickSkill))
            {
                missing.Add("ActionBar/SkillBtn");
            }

            if (!this.BindMenuButton(actionBar, "ItemBtn", this.OnClickItem))
            {
                missing.Add("ActionBar/ItemBtn");
            }

            if (!this.BindMenuButton(actionBar, "FleeBtn", this.OnClickFlee))
            {
                missing.Add("ActionBar/FleeBtn");
            }

            if (this.enemyCardsRoot == null)
            {
                missing.Add("EnemyCards");
            }

            if (this.allyCardsRoot == null)
            {
                missing.Add("AllyCards");
            }

            if (this.logText == null)
            {
                missing.Add("LogText(Text)");
            }

            if (this.headerText == null)
            {
                missing.Add("Header(Text)");
            }

            if (this.subMenuRoot == null)
            {
                missing.Add("SubMenu");
            }

            if (this.bannerRoot == null || this.bannerText == null)
            {
                missing.Add("Banner/BannerLabel(Text)");
            }

            if (this.menuGroup == null)
            {
                missing.Add("ActionBar(CanvasGroup)");
            }

            if (missing.Count > 0)
            {
                AWorkerTask.LogProvider(
                    $"[PanelDiag] TurnBattlePanel 场景节点绑定不全，缺失：{string.Join("、", missing)}（开战将异常，请补建场景节点）",
                    LogManager.LogLevelEnum.Warning);
            }
            else
            {
                AWorkerTask.LogProvider("[PanelDiag] TurnBattlePanel 场景节点绑定完成", LogManager.LogLevelEnum.Debug);
            }

            this.subMenuRoot?.SetActive(false);
            this.bannerRoot?.SetActive(false);

            // 代码补建：防御按钮（ActionBar 横排自动排版）与回合顺序条
            this.EnsureDefendButton(actionBar);
            this.EnsureTurnOrderBar();
        }

        /// <summary>代码创建防御按钮（148×52 与场景四按钮同规格，HorizontalLayoutGroup 自动排入）。</summary>
        private void EnsureDefendButton(Transform actionBar)
        {
            if (actionBar == null || actionBar.Find("DefendBtn") != null)
            {
                return;
            }

            GameObject go = new GameObject("DefendBtn");
            go.transform.SetParent(actionBar, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(148f, 52f);
            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minWidth = 148f;
            layoutElement.minHeight = 52f;
            layoutElement.preferredWidth = 148f;
            layoutElement.preferredHeight = 52f;

            Image bg = go.AddComponent<Image>();
            bg.color = MenuBgColor;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(this.OnClickDefend);

            this.CreateText(
                go.transform, "Label", "防御(5)", 24, Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 40f));
        }

        /// <summary>代码创建回合顺序条（panel 根下、Header 下方横排 5 槽）。</summary>
        private void EnsureTurnOrderBar()
        {
            if (this.turnOrderBar != null)
            {
                return;
            }

            GameObject bar = new GameObject("TurnOrderBar");
            bar.transform.SetParent(this.transform, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -64f);
            rt.sizeDelta = new Vector2(660f, 62f);

            HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            this.turnOrderBar = bar.transform;
        }

        /// <summary>绑定场景主菜单按钮 — 只清运行时监听（RemoveAllListeners 不动 Inspector 持久绑定），返回是否绑定成功。</summary>
        private bool BindMenuButton(Transform actionBar, string name, System.Action onClick)
        {
            Button button = actionBar?.Find(name)?.GetComponent<Button>();
            if (button == null)
            {
                return false;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
            return true;
        }

        private void CreateCard(TurnBattleUnit unit, Transform container, Vector3 localPos)
        {
            GameObject root = new GameObject($"Card_{unit.DisplayName}");
            root.transform.SetParent(container, false);
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.localPosition = localPos;

            Image bg = root.AddComponent<Image>();
            bg.color = unit.Faction == TurnBattleFaction.Enemy ? EnemyCardBg : AllyCardBg;
            bg.raycastTarget = true;

            // 敌方卡可点击（点选目标模式）
            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            long unitId = unit.UnitId;
            btn.onClick.AddListener(() => this.OnCardClicked(unitId));

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CanvasGroup group = root.AddComponent<CanvasGroup>();

            Image portrait = this.CreateImage(root.transform, "Portrait", 84f, 84f, Color.white);
            portrait.sprite = TurnBattleManager.Instance.GetPortrait(unit.UnitId);
            portrait.preserveAspect = true;

            Text nameText = this.CreateText(
                root.transform, "Name", $"{unit.DisplayName} Lv.{unit.Level}", 24, Color.white,
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 26f));

            this.CreateText(
                root.transform, "Element", this.BuildElementText(unit), 12, new Color(0.75f, 0.72f, 0.55f),
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 20f));

            Image hpBg = this.CreateImage(root.transform, "HpBg", CardWidth - 24f, 15f, BarBgColor);
            Image hpFill = this.CreateStretchImage(hpBg.transform, "Fill", HpColor);
            hpFill.sprite = GetWhiteSprite();
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;

            Image mpBg = this.CreateImage(root.transform, "MpBg", CardWidth - 24f, 11f, BarBgColor);
            Image mpFill = this.CreateStretchImage(mpBg.transform, "Fill", MpColor);
            mpFill.sprite = GetWhiteSprite();
            mpFill.type = Image.Type.Filled;
            mpFill.fillMethod = Image.FillMethod.Horizontal;

            Text hpText = this.CreateText(
                root.transform, "HpText", string.Empty, 12, Color.white,
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 20f));

            // 状态行（末尾追加，最多 4 个 16×16 色块，按状态类型染色）
            GameObject statusRow = new GameObject("StatusRow");
            statusRow.transform.SetParent(root.transform, false);
            RectTransform statusRt = statusRow.AddComponent<RectTransform>();
            statusRt.sizeDelta = new Vector2(CardWidth - 24f, 18f);
            HorizontalLayoutGroup statusLayout = statusRow.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 4f;
            statusLayout.childAlignment = TextAnchor.MiddleCenter;
            statusLayout.childControlWidth = false;
            statusLayout.childControlHeight = false;
            statusLayout.childForceExpandWidth = false;
            statusLayout.childForceExpandHeight = false;

            UnitCard card = new UnitCard
            {
                UnitId = unit.UnitId,
                Faction = unit.Faction,
                Root = root.transform,
                Bg = bg,
                NameText = nameText,
                NameBase = nameText.text,
                HpFill = hpFill,
                MpFill = mpFill,
                HpText = hpText,
                StatusRow = statusRow.transform,
                Group = group,
                HomePos = localPos,
                BaseBgColor = bg.color,
            };
            this.cards[unit.UnitId] = card;
            this.RefreshCard(card, unit);
        }

        private string BuildElementText(TurnBattleUnit unit)
        {
            if (unit.LingGenElements.Count == 0)
            {
                return unit.Faction == TurnBattleFaction.Enemy ? "无灵根" : "无灵根（中性）";
            }

            List<string> names = new List<string>();
            foreach (Element element in unit.LingGenElements)
            {
                names.Add(LingGenRuleService.GetElementName(element));
            }

            return string.Join("·", names) + "灵根";
        }

        private void ClearCards()
        {
            foreach (UnitCard card in this.cards.Values)
            {
                if (card.Root != null)
                {
                    // Root 是 Transform 引用：Destroy(组件) 只会尝试移除该组件，
                    // RectTransform 被 Image/LayoutGroup 依赖必然拒绝并泄漏整张卡——必须销毁 GameObject
                    Destroy(card.Root.gameObject);
                }
            }

            this.cards.Clear();
        }

        private void RefreshCards()
        {
            if (this.state == null)
            {
                return;
            }

            foreach (TurnBattleUnit unit in this.state.Units)
            {
                if (this.cards.TryGetValue(unit.UnitId, out UnitCard card))
                {
                    this.RefreshCard(card, unit);
                }
            }
        }

        private void RefreshCard(UnitCard card, TurnBattleUnit unit)
        {
            float hpRatio = unit.MaxHp > 0f ? Mathf.Clamp01(unit.Hp / unit.MaxHp) : 0f;
            card.HpFill.fillAmount = hpRatio;
            card.MpFill.fillAmount = unit.MaxMp > 0 ? Mathf.Clamp01((float)unit.Mp / unit.MaxMp) : 0f;
            card.HpText.text = $"{Mathf.CeilToInt(Mathf.Max(unit.Hp, 0f))}/{Mathf.CeilToInt(unit.MaxHp)}";

            // 眩晕标记 + 状态图标；恢复防御染色的卡底（演出期间的临时色）
            if (card.NameText != null)
            {
                card.NameText.text = TurnBattleStatusRuleService.IsStunned(unit)
                    ? $"[晕]{card.NameBase}"
                    : card.NameBase;
            }

            if (card.Bg != null && !card.Down && card.FlashTimer <= 0f && !this.IsTargetHighlighted(card))
            {
                card.Bg.color = card.BaseBgColor;
            }

            this.RefreshCardStatus(card, unit);

            if (unit.IsDown && !card.Down)
            {
                this.MarkCardDown(card);
            }
        }

        /// <summary>重建卡片状态图标行（≤4 个色块，同类单实例天然不重复）。</summary>
        private void RefreshCardStatus(UnitCard card, TurnBattleUnit unit)
        {
            if (card.StatusRow == null)
            {
                return;
            }

            for (int i = card.StatusRow.childCount - 1; i >= 0; i--)
            {
                Destroy(card.StatusRow.GetChild(i).gameObject);
            }

            int added = 0;
            foreach (TurnBattleStatusEffect status in unit.StatusEffects)
            {
                if (added >= 4)
                {
                    break;
                }

                Image icon = this.CreateImage(card.StatusRow, $"Status_{status.Kind}", 16f, 16f, this.StatusColorOf(status.Kind, Color.white));
                icon.sprite = GetWhiteSprite();
                added++;
            }
        }

        private void MarkCardDown(UnitCard card)
        {
            card.Down = true;
            card.Group.alpha = 0.4f;
            card.Group.interactable = false;
            card.Root.localPosition = card.HomePos + new Vector3(0f, -14f, 0f);
        }

        /// <summary>重建回合顺序预览条 — 未来 N 手 SPD 序；槽 0=当前行动者（放大金框），眩晕置灰带「晕」角标。</summary>
        private void RefreshTurnOrderBar()
        {
            this.ClearTurnOrderBar();
            if (this.state == null || this.turnOrderBar == null)
            {
                return;
            }

            List<TurnBattleUnit> order = TurnBattleRuleService.PredictTurnOrder(this.state, TurnOrderSlotCount);
            for (int i = 0; i < order.Count; i++)
            {
                TurnBattleUnit unit = order[i];
                GameObject slot = new GameObject($"Slot_{unit.UnitId}");
                slot.transform.SetParent(this.turnOrderBar, false);
                RectTransform slotRt = slot.AddComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(56f, 60f);

                Image frame = slot.AddComponent<Image>();
                frame.color = i == 0 ? CurrentSlotColor
                    : unit.Faction == TurnBattleFaction.Ally ? AllySlotColor : EnemySlotColor;
                frame.raycastTarget = false;

                // 头像贴顶，名字沉底，56×60 内不重叠
                Image portrait = this.CreateImage(slot.transform, "Portrait", 44f, 44f, Color.white);
                RectTransform portraitRt = portrait.rectTransform;
                portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.5f, 1f);
                portraitRt.pivot = new Vector2(0.5f, 1f);
                portraitRt.anchoredPosition = new Vector2(0f, -6f);
                portrait.sprite = TurnBattleManager.Instance.GetPortrait(unit.UnitId);
                portrait.preserveAspect = true;

                this.CreateText(
                    slot.transform, "Name", unit.DisplayName, 12, Color.white,
                    new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(56f, 14f));

                if (i == 0)
                {
                    // 只放大视觉不占布局（LayoutGroup 不改 localScale）
                    slot.transform.localScale = Vector3.one * 1.15f;
                }

                if (TurnBattleStatusRuleService.IsStunned(unit))
                {
                    CanvasGroup stunGroup = slot.AddComponent<CanvasGroup>();
                    stunGroup.alpha = 0.5f;
                    stunGroup.blocksRaycasts = false;

                    Text badge = this.CreateText(
                        slot.transform, "StunBadge", "晕", 12, StunColor,
                        new Vector2(1f, 1f), new Vector2(-4f, -2f), new Vector2(14f, 14f));
                    badge.alignment = TextAnchor.UpperRight;
                }
            }
        }

        private void ClearTurnOrderBar()
        {
            if (this.turnOrderBar == null)
            {
                return;
            }

            for (int i = this.turnOrderBar.childCount - 1; i >= 0; i--)
            {
                Destroy(this.turnOrderBar.GetChild(i).gameObject);
            }
        }

        #endregion

        #region 演出

        private void StepPlay(float dt)
        {
            if (this.playIndex >= this.playQueue.Count)
            {
                this.FinishPlayback();
                return;
            }

            TurnBattleEffectEntry effect = this.playQueue[this.playIndex];
            this.segTimer += dt;
            float t = Mathf.Clamp01(this.segTimer / this.segDuration);

            // 行动者前冲（前 40% 时间向对向位移，sin 曲线去回）
            UnitCard actor = this.GetCard(effect.ActorId);
            if (actor != null && !actor.Down)
            {
                float punch = Mathf.Sin(Mathf.Min(t / 0.4f, 1f) * Mathf.PI) * 30f;
                float dir = actor.Faction == TurnBattleFaction.Ally ? 1f : -1f;
                actor.Root.localPosition = actor.HomePos + new Vector3(punch * dir, 0f, 0f);
            }

            if (!this.impactApplied && t >= 0.4f)
            {
                this.impactApplied = true;
                this.ApplyEffectVisual(effect);
            }

            if (t < 1f)
            {
                return;
            }

            if (actor != null)
            {
                // 倒下卡保持下沉姿态，活着的行为者回位
                actor.Root.localPosition = actor.Down
                    ? actor.HomePos + new Vector3(0f, -14f, 0f)
                    : actor.HomePos;
            }

            this.playIndex++;
            this.segTimer = 0f;
            this.impactApplied = false;
            if (this.playIndex >= this.playQueue.Count)
            {
                this.FinishPlayback();
            }
            else
            {
                this.segDuration = SegmentDurationOf(this.playQueue[this.playIndex].Kind);
            }
        }

        private void FinishPlayback()
        {
            this.playing = false;
            this.playQueue.Clear();
            this.playIndex = 0;
            TurnBattleManager.Instance.NotifyPresentationFinished();
        }

        /// <summary>效果落点视觉：白闪/飘字/血条对齐/置灰 + 日志。</summary>
        private void ApplyEffectVisual(TurnBattleEffectEntry effect)
        {
            string actorName = this.UnitName(effect.ActorId);
            string targetName = this.UnitName(effect.TargetId);
            UnitCard target = this.GetCard(effect.TargetId);

            switch (effect.Kind)
            {
                case TurnBattleEffectKind.Damage:
                {
                    if (target != null)
                    {
                        target.Flash();
                        float maxHp = this.UnitMaxHp(effect.TargetId);
                        target.HpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(effect.RemainingHp / maxHp) : 0f;
                        target.HpText.text = $"{Mathf.CeilToInt(Mathf.Max(effect.RemainingHp, 0f))}/{Mathf.CeilToInt(maxHp)}";
                    }

                    // 序列帧攻击特效：敌方攻击镜像（斩击方向与我方相反）
                    UnitCard actorCard = this.GetCard(effect.ActorId);
                    bool flipX = actorCard != null && actorCard.Faction == TurnBattleFaction.Enemy;
                    TurnBattleEffectLibrary.PlayDamage(target?.Root, effect, flipX);

                    string prefix = (effect.IsCombo ? "追击 " : "")
                        + (effect.ElementMultiplier > 1.05f ? "克制! "
                            : effect.ElementMultiplier < 0.95f ? "抵抗 " : string.Empty);
                    string floatText = $"{prefix}{(effect.IsCritical ? "暴击 " : "")}-{Mathf.RoundToInt(effect.Value)}";
                    int fontSize = effect.IsCritical ? 36 : 24;
                    Color color = effect.IsCritical ? CriticalColor : DamageColor;
                    this.SpawnFloatText(effect.TargetId, floatText, color, fontSize);
                    this.AppendLog(
                        $"{actorName}{(string.IsNullOrEmpty(effect.SourceName) ? string.Empty : $" 使用{effect.SourceName}")}" +
                        $" → {targetName} {-Mathf.RoundToInt(effect.Value)} 伤害" +
                        $"{(effect.IsCritical ? "（暴击）" : string.Empty)}" +
                        $"{(effect.ElementMultiplier > 1.05f ? "（克制）" : effect.ElementMultiplier < 0.95f ? "（被抵抗）" : string.Empty)}");
                    break;
                }

                case TurnBattleEffectKind.Heal:
                    if (target != null)
                    {
                        float maxHp = this.UnitMaxHp(effect.TargetId);
                        target.HpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(effect.RemainingHp / maxHp) : 0f;
                        target.HpText.text = $"{Mathf.CeilToInt(Mathf.Max(effect.RemainingHp, 0f))}/{Mathf.CeilToInt(maxHp)}";
                    }

                    TurnBattleEffectLibrary.PlayHeal(target?.Root);
                    this.SpawnFloatText(effect.TargetId, $"+{Mathf.RoundToInt(effect.Value)}", HealColor, 28);
                    this.AppendLog($"{actorName} 回复 {Mathf.RoundToInt(effect.Value)} HP");
                    break;

                case TurnBattleEffectKind.Miss:
                    this.SpawnFloatText(effect.TargetId, "MISS", MissColor, 26);
                    this.AppendLog($"{actorName} 的攻击落空（{targetName} 闪避）");
                    break;

                case TurnBattleEffectKind.Buff:
                    this.SpawnFloatText(effect.ActorId, "攻击提升↑", BuffColor, 26);
                    this.AppendLog($"{actorName} 攻击力提升（×{effect.Value:0.0}）");
                    break;

                case TurnBattleEffectKind.Down:
                    if (target != null)
                    {
                        this.MarkCardDown(target);
                    }

                    this.SpawnFloatText(effect.TargetId, "击倒！", DownColor, 30);
                    this.AppendLog($"{targetName} 倒下了！");
                    break;

                case TurnBattleEffectKind.FleeFail:
                    this.ShowBanner("逃跑失败！", 0.7f, null);
                    this.AppendLog($"{actorName} 尝试逃跑失败（浪费一回合）");
                    break;

                case TurnBattleEffectKind.FleeSuccess:
                    this.AppendLog($"{actorName} 逃跑成功");
                    break;

                case TurnBattleEffectKind.StatusApply:
                {
                    Color statusColor = this.StatusColorOf(effect.StatusKind, BuffColor);
                    TurnBattleEffectLibrary.PlayStatus(target?.Root, statusColor);
                    this.SpawnFloatText(effect.TargetId, $"{effect.SourceName}！", statusColor, 24);
                    this.AppendLog($"{actorName} 使 {targetName} {effect.SourceName}（{Mathf.RoundToInt(effect.Value)} 回合）");
                    if (target != null)
                    {
                        TurnBattleUnit targetUnit = this.state?.FindUnit(effect.TargetId);
                        if (targetUnit != null)
                        {
                            this.RefreshCardStatus(target, targetUnit); // 图标即时可见
                        }
                    }

                    break;
                }

                case TurnBattleEffectKind.StatusTick:
                {
                    if (target != null)
                    {
                        target.Flash();
                        float maxHp = this.UnitMaxHp(effect.TargetId);
                        target.HpFill.fillAmount = maxHp > 0f ? Mathf.Clamp01(effect.RemainingHp / maxHp) : 0f;
                        target.HpText.text = $"{Mathf.CeilToInt(Mathf.Max(effect.RemainingHp, 0f))}/{Mathf.CeilToInt(maxHp)}";
                    }

                    Color tickColor = this.StatusColorOf(effect.StatusKind, DamageColor);
                    this.SpawnFloatText(effect.TargetId, $"{effect.SourceName} -{Mathf.RoundToInt(effect.Value)}", tickColor, 24);
                    this.AppendLog($"{targetName} 因{effect.SourceName} 受到 {Mathf.RoundToInt(effect.Value)} 伤害");
                    break;
                }

                case TurnBattleEffectKind.Stunned:
                    this.SpawnFloatText(effect.ActorId, "眩晕", StunColor, 24);
                    this.AppendLog($"{actorName} 眩晕，无法行动");
                    break;

                case TurnBattleEffectKind.Defend:
                    this.SpawnFloatText(effect.ActorId, "防御姿态", DefendColor, 24);
                    if (target != null && target.Bg != null)
                    {
                        target.Bg.color = DefendBgColor;
                    }

                    this.AppendLog($"{actorName} 进入防御姿态（本回合受伤减半）");
                    break;
            }

            // 不做全量刷新：快照已是回合末值，全量刷会把后续段的血量变化剧透；
            // 每段只对齐本效果的 RemainingHp，回合末由 OnAwaitPlayerAction 全量对齐
        }

        private void SpawnFloatText(long unitId, string text, Color color, int fontSize)
        {
            UnitCard card = this.GetCard(unitId);
            if (card?.Root != null)
            {
                TurnBattleFloatingText.Create(card.Root, text, color, fontSize);
            }
        }

        private void StepFlash(float dt)
        {
            foreach (UnitCard card in this.cards.Values)
            {
                if (card.FlashTimer <= 0f)
                {
                    continue;
                }

                card.FlashTimer -= dt;
                if (card.FlashTimer <= 0f && card.Bg != null && !this.IsTargetHighlighted(card))
                {
                    card.Bg.color = card.BaseBgColor;
                }
            }
        }

        #endregion

        #region 菜单

        private bool CanUseMenu()
        {
            return this.state != null
                && this.state.Phase == TurnBattlePhase.AwaitPlayerAction
                && !this.playing
                && !this.finishedBannerPlaying;
        }

        private void SetMenuEnabled(bool enabled)
        {
            if (this.menuGroup != null)
            {
                this.menuGroup.interactable = enabled;
                this.menuGroup.alpha = enabled ? 1f : 0.55f;
            }
        }

        private void ProcessHotkeys()
        {
            // 数字键 1-5 主菜单快捷键（主菜单层可用时；子菜单开着时忽略防误触）
            if (!this.CanUseMenu() || this.subMenuActive || this.targeting)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                this.OnClickAttack();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                this.OnClickSkill();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                this.OnClickItem();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                this.OnClickFlee();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                this.OnClickDefend();
            }
        }

        private void OnClickAttack()
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            List<TurnBattleUnit> enemies = this.state.AliveEnemies;
            if (enemies.Count == 1)
            {
                this.SubmitAction(new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = enemies[0].UnitId });
                return;
            }

            this.EnterTargeting(TurnBattleActionKind.Attack, -1);
        }

        private void OnClickSkill()
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            this.ShowSkillMenu();
        }

        private void OnClickItem()
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            this.ShowItemMenu();
        }

        private void OnClickFlee()
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            this.ShowFleeConfirm();
        }

        private void OnClickDefend()
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            this.SubmitAction(new TurnBattleAction { Kind = TurnBattleActionKind.Defend });
        }

        private void SubmitAction(TurnBattleAction action)
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            this.HideSubMenu();
            this.CancelTargeting();
            this.SetMenuEnabled(false);
            TurnBattleManager.Instance.SubmitPlayerAction(action);
        }

        private void ShowSkillMenu()
        {
            this.HideSubMenu();
            TurnBattleUnit player = this.state.PlayerUnit;
            if (player == null || player.Skills.Count == 0)
            {
                this.AddSubMenuItem("无可用技能（普攻永远可用）", false, null);
                this.subMenuRoot.SetActive(true);
                this.subMenuActive = true;
                return;
            }

            for (int i = 0; i < player.Skills.Count; i++)
            {
                TurnBattleSkillState skill = player.Skills[i];
                bool usable = skill.IsUsable(player.Mp) && player.CanAct;
                string stateLabel = skill.RemainingCooldown > 0
                    ? $"冷却剩{skill.RemainingCooldown}回合"
                    : player.Mp < skill.ManaCost ? "蓝不足" : $"蓝{skill.ManaCost}";
                int index = i;
                this.AddSubMenuItem($"{skill.SkillName}（{stateLabel}）", usable, () =>
                {
                    if (skill.Type == SkillType.SingleTarget)
                    {
                        this.EnterTargeting(TurnBattleActionKind.UseSkill, index);
                    }
                    else
                    {
                        // SelfAOE/SelfBuff/SelfHeal 无需选目标
                        this.SubmitAction(new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = index });
                    }
                });
            }

            this.subMenuRoot.SetActive(true);
            this.subMenuActive = true;
        }

        private void ShowItemMenu()
        {
            this.HideSubMenu();

            List<AItem> consumables = BackpackController.Instance != null
                ? BackpackController.Instance.GetConsumables()
                : new List<AItem>();

            int added = 0;
            foreach (AItem item in consumables)
            {
                if (!(item is AddHp))
                {
                    continue;
                }

                ItemData data = ServiceLocator.Get<ItemDataManager>().GetById(item.Id);
                string displayName = data != null ? data.CnName : "血瓶";
                int uid = item.Uid;
                this.AddSubMenuItem($"{displayName} ×{item.Quantity}（回复{AddHp.HealAmount:0} HP）", true, () => this.OnItemClicked(uid, displayName));
                added++;
            }

            if (added == 0)
            {
                this.AddSubMenuItem("没有可用药品", false, null);
            }

            this.subMenuRoot.SetActive(true);
            this.subMenuActive = true;
        }

        /// <summary>道具点击：先扣背包（立即生效，战斗中止不退还——第一版可接受），再走快照回血。</summary>
        private void OnItemClicked(int uid, string displayName)
        {
            if (!this.CanUseMenu())
            {
                return;
            }

            if (BackpackController.Instance == null || !BackpackController.Instance.ConsumeConsumableByUid(uid))
            {
                this.ShowItemMenu(); // 背包并发变化，刷新列表
                return;
            }

            this.SubmitAction(new TurnBattleAction
            {
                Kind = TurnBattleActionKind.UseItem,
                ItemHealAmount = AddHp.HealAmount,
                ItemDisplayName = displayName,
            });
        }

        private void ShowFleeConfirm()
        {
            this.HideSubMenu();
            this.AddSubMenuItem("逃跑可能失败并浪费一回合，确认逃跑？", false, null);
            this.AddSubMenuItem("✔ 确认逃跑", true, () =>
                this.SubmitAction(new TurnBattleAction { Kind = TurnBattleActionKind.Flee }));
            this.AddSubMenuItem("✖ 取消", true, this.HideSubMenu);
            this.subMenuRoot.SetActive(true);
            this.subMenuActive = true;
        }

        private void HideSubMenu()
        {
            this.subMenuActive = false;
            if (this.subMenuRoot == null || this.subMenuContainer == null)
            {
                return;
            }

            this.subMenuRoot.SetActive(false);
            for (int i = this.subMenuContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(this.subMenuContainer.GetChild(i).gameObject);
            }
        }

        private void AddSubMenuItem(string label, bool interactable, System.Action onClick)
        {
            GameObject go = new GameObject("SubItem");
            go.transform.SetParent(this.subMenuContainer, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, 42f);

            Image bg = go.AddComponent<Image>();
            bg.color = interactable ? MenuBgColor : new Color(0.14f, 0.14f, 0.16f, 0.8f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.interactable = interactable;
            if (onClick != null)
            {
                btn.onClick.AddListener(() => onClick());
            }

            this.CreateText(
                go.transform, "Label", label, 24,
                interactable ? Color.white : new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(510f, 40f));
        }

        #endregion

        #region 目标点选

        private void EnterTargeting(TurnBattleActionKind kind, int skillIndex)
        {
            this.HideSubMenu();
            this.targeting = true;
            this.pendingKind = kind;
            this.pendingSkillIndex = skillIndex;

            if (this.state != null)
            {
                foreach (TurnBattleUnit unit in this.state.AliveEnemies)
                {
                    if (this.cards.TryGetValue(unit.UnitId, out UnitCard card) && card.Bg != null)
                    {
                        card.Bg.color = TargetHighlightColor;
                    }
                }
            }

            this.AddSubMenuItem("点击发光的敌方卡片选择目标（右键取消）", false, null);
            this.subMenuRoot.SetActive(true);
            this.subMenuActive = false; // 点选提示不算子菜单（ESC 优先取消点选）
        }

        private void CancelTargeting()
        {
            if (!this.targeting)
            {
                return;
            }

            this.targeting = false;
            foreach (UnitCard card in this.cards.Values)
            {
                if (card.Bg != null && !card.Down)
                {
                    card.Bg.color = card.BaseBgColor;
                }
            }

            this.HideSubMenu();
        }

        private bool IsTargetHighlighted(UnitCard card)
        {
            return this.targeting && !card.Down;
        }

        private void OnCardClicked(long unitId)
        {
            if (!this.targeting || !this.CanUseMenu() || this.state == null)
            {
                return;
            }

            TurnBattleUnit unit = this.state.FindUnit(unitId);
            if (unit == null || unit.Faction != TurnBattleFaction.Enemy || unit.IsDown)
            {
                return;
            }

            TurnBattleAction action = this.pendingKind == TurnBattleActionKind.UseSkill
                ? new TurnBattleAction { Kind = TurnBattleActionKind.UseSkill, SkillIndex = this.pendingSkillIndex, TargetUnitId = unitId }
                : new TurnBattleAction { Kind = TurnBattleActionKind.Attack, TargetUnitId = unitId };
            this.SubmitAction(action);
        }

        #endregion

        #region 横幅与日志

        private void ShowBanner(string text, float duration, System.Action onDone)
        {
            this.bannerText.text = text;
            this.bannerRoot.SetActive(true);
            this.bannerTimer = duration;
            this.bannerActive = true;
            this.bannerCallback = onDone;
        }

        private void StepBanner(float dt)
        {
            if (!this.bannerActive)
            {
                return;
            }

            this.bannerTimer -= dt;
            if (this.bannerTimer <= 0f)
            {
                this.HideBanner();
                System.Action callback = this.bannerCallback;
                this.bannerCallback = null;
                callback?.Invoke();
            }
        }

        private void HideBanner()
        {
            this.bannerActive = false;
            if (this.bannerRoot != null)
            {
                this.bannerRoot.SetActive(false);
            }
        }

        private void AppendLog(string text)
        {
            this.logs.Enqueue(text);
            while (this.logs.Count > 6)
            {
                this.logs.Dequeue();
            }

            if (this.logText != null)
            {
                this.logText.text = string.Join("\n", this.logs);
            }
        }

        #endregion

        #region 工具

        /// <summary>懒创建共享白 Sprite（4×4 Point 过滤，像素风锐利块状）。</summary>
        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }

                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            }

            return whiteSprite;
        }

        private UnitCard GetCard(long unitId)
        {
            return this.cards.TryGetValue(unitId, out UnitCard card) ? card : null;
        }

        /// <summary>状态类型配色 — 状态图标/飘字/角标共用；未知类型回落 fallback。</summary>
        private Color StatusColorOf(TurnBattleStatusKind? kind, Color fallback)
        {
            switch (kind)
            {
                case TurnBattleStatusKind.Poison: return new Color(0.45f, 0.85f, 0.35f);
                case TurnBattleStatusKind.Burn: return new Color(1f, 0.5f, 0.25f);
                case TurnBattleStatusKind.Bleed: return new Color(0.8f, 0.15f, 0.2f);
                case TurnBattleStatusKind.Stun: return StunColor;
                case TurnBattleStatusKind.AttackUp: return new Color(1f, 0.78f, 0.3f);
                case TurnBattleStatusKind.AttackDown: return new Color(0.72f, 0.45f, 0.95f);
                case TurnBattleStatusKind.DefenseUp: return new Color(0.4f, 0.65f, 1f);
                case TurnBattleStatusKind.DefenseDown: return new Color(0.55f, 0.35f, 0.75f);
                default: return fallback;
            }
        }

        private string UnitName(long unitId)
        {
            TurnBattleUnit unit = this.state?.FindUnit(unitId);
            return unit != null ? unit.DisplayName : "？";
        }

        private float UnitMaxHp(long unitId)
        {
            TurnBattleUnit unit = this.state?.FindUnit(unitId);
            return unit != null ? unit.MaxHp : 1f;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            Text txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = AI.Dialogue.LLM.UIFontConfig.GetFont();
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            return txt;
        }

        private Image CreateImage(Transform parent, string name, float width, float height, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Image CreateStretchImage(Transform parent, string name, Color color)
        {
            Image image = this.CreateImage(parent, name, 0f, 0f, color);
            RectTransform rt = image.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(-4f, -4f);
            return image;
        }

        #endregion
    }
}
