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
    /// 全部代码构建（项目约定 Game.unity 不手改 YAML）；
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

        // ===== 配色 =====
        private static readonly Color AllyCardBg = new Color(0.14f, 0.22f, 0.15f, 0.92f);
        private static readonly Color EnemyCardBg = new Color(0.28f, 0.13f, 0.13f, 0.92f);
        private static readonly Color CardFlashColor = new Color(1f, 0.45f, 0.35f, 0.95f);
        private static readonly Color TargetHighlightColor = new Color(0.95f, 0.8f, 0.25f, 0.95f);
        private static readonly Color HpColor = new Color(0.85f, 0.2f, 0.2f);
        private static readonly Color MpColor = new Color(0.2f, 0.4f, 0.9f);
        private static readonly Color BarBgColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        private static readonly Color MenuBgColor = new Color(0.16f, 0.16f, 0.2f, 0.95f);
        private static readonly Color SubMenuBgColor = new Color(0.1f, 0.1f, 0.14f, 0.96f);
        private static readonly Color DamageColor = new Color(1f, 0.85f, 0.85f);
        private static readonly Color CriticalColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color HealColor = new Color(0.4f, 1f, 0.45f);
        private static readonly Color MissColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color BuffColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color DownColor = new Color(1f, 0.3f, 0.3f);

        /// <summary>单位卡片 — 演出（前冲/白闪/置灰）与刷新的最小句柄。</summary>
        private class UnitCard
        {
            public long UnitId;
            public TurnBattleFaction Faction;
            public Transform Root;
            public Image Bg;
            public Text NameText;
            public Image HpFill;
            public Image MpFill;
            public Text HpText;
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
            this.segDuration = 0.55f;
            this.impactApplied = false;
            this.playing = true;
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

        /// <summary>一次性构建静态舞台（Shade/卡区/日志/菜单/横幅）。</summary>
        private void BuildOnce()
        {
            if (this.built)
            {
                return;
            }

            this.built = true;

            // 全屏遮罩：挡视线 + 挡点击（大世界已冻结，双保险）
            GameObject shade = new GameObject("Shade");
            shade.transform.SetParent(this.transform, false);
            Image shadeImage = shade.AddComponent<Image>();
            shadeImage.color = new Color(0f, 0f, 0f, 0.72f);
            this.StretchFull(shade);

            // 敌我卡区（纯定位容器，卡片按索引手动排列，演出位移不受 Layout 干扰）
            this.enemyCardsRoot = this.CreateAnchorNode("EnemyCards", new Vector2(1f, 1f), Vector2.zero);
            this.allyCardsRoot = this.CreateAnchorNode("AllyCards", new Vector2(0f, 0f), Vector2.zero);

            // 战斗日志（左上）
            GameObject logGo = new GameObject("LogText");
            logGo.transform.SetParent(this.transform, false);
            RectTransform logRt = logGo.AddComponent<RectTransform>();
            logRt.anchorMin = logRt.anchorMax = new Vector2(0f, 1f);
            logRt.pivot = new Vector2(0f, 1f);
            logRt.anchoredPosition = new Vector2(24f, -66f);
            logRt.sizeDelta = new Vector2(460f, 200f);
            this.logText = logGo.AddComponent<Text>();
            this.logText.font = AI.Dialogue.LLM.UIFontConfig.GetFont();
            this.logText.fontSize = 12;
            this.logText.color = new Color(0.85f, 0.85f, 0.8f, 0.9f);
            this.logText.alignment = TextAnchor.UpperLeft;
            this.logText.raycastTarget = false;
            this.logText.supportRichText = false;

            // 回合头（顶部中央）
            this.headerText = this.CreateText(
                this.transform, "Header", "⚔ 回合制战斗", 24, new Color(1f, 0.93f, 0.75f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(720f, 48f));

            // 子菜单（主菜单上方，动态重建：技能/道具/逃跑确认/点选提示）
            this.subMenuRoot = new GameObject("SubMenu");
            this.subMenuRoot.transform.SetParent(this.transform, false);
            RectTransform subRt = this.subMenuRoot.AddComponent<RectTransform>();
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 92f);
            subRt.sizeDelta = new Vector2(560f, 300f);
            Image subBg = this.subMenuRoot.AddComponent<Image>();
            subBg.color = SubMenuBgColor;
            VerticalLayoutGroup subLayout = this.subMenuRoot.AddComponent<VerticalLayoutGroup>();
            subLayout.spacing = 6f;
            subLayout.padding = new RectOffset(10, 10, 10, 10);
            subLayout.childAlignment = TextAnchor.UpperCenter;
            subLayout.childControlWidth = false;
            subLayout.childControlHeight = false;
            subLayout.childForceExpandWidth = false;
            subLayout.childForceExpandHeight = false;
            this.subMenuContainer = this.subMenuRoot.transform;
            this.subMenuRoot.SetActive(false);

            // 主菜单（底部中央：攻击/技能/道具/逃跑）
            GameObject menuGo = new GameObject("ActionBar");
            menuGo.transform.SetParent(this.transform, false);
            RectTransform menuRt = menuGo.AddComponent<RectTransform>();
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0.5f, 0f);
            menuRt.pivot = new Vector2(0.5f, 0f);
            menuRt.anchoredPosition = new Vector2(0f, 24f);
            menuRt.sizeDelta = new Vector2(640f, 56f);
            HorizontalLayoutGroup menuLayout = menuGo.AddComponent<HorizontalLayoutGroup>();
            menuLayout.spacing = 12f;
            menuLayout.childAlignment = TextAnchor.MiddleCenter;
            menuLayout.childControlWidth = false;
            menuLayout.childControlHeight = false;
            menuLayout.childForceExpandWidth = false;
            menuLayout.childForceExpandHeight = false;
            this.menuGroup = menuGo.AddComponent<CanvasGroup>();
            this.CreateMenuButton(menuGo.transform, "AttackBtn", "攻击", () => this.OnClickAttack());
            this.CreateMenuButton(menuGo.transform, "SkillBtn", "技能", () => this.OnClickSkill());
            this.CreateMenuButton(menuGo.transform, "ItemBtn", "道具", () => this.OnClickItem());
            this.CreateMenuButton(menuGo.transform, "FleeBtn", "逃跑", () => this.OnClickFlee());

            // 结果横幅（最上层）
            this.bannerRoot = new GameObject("Banner");
            this.bannerRoot.transform.SetParent(this.transform, false);
            RectTransform bannerRt = this.bannerRoot.AddComponent<RectTransform>();
            bannerRt.anchorMin = bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRt.anchoredPosition = Vector2.zero;
            bannerRt.sizeDelta = new Vector2(920f, 96f);
            Image bannerBg = this.bannerRoot.AddComponent<Image>();
            bannerBg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            this.bannerText = this.CreateText(
                this.bannerRoot.transform, "BannerLabel", string.Empty, 36, Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 90f));
            this.bannerRoot.SetActive(false);
        }

        private Transform CreateAnchorNode(string name, Vector2 anchor, Vector2 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(this.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = Vector2.zero;
            return go.transform;
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

            this.CreateText(
                root.transform, "Name", $"{unit.DisplayName} Lv.{unit.Level}", 24, Color.white,
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 26f));

            this.CreateText(
                root.transform, "Element", this.BuildElementText(unit), 12, new Color(0.75f, 0.72f, 0.55f),
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 20f));

            Image hpBg = this.CreateImage(root.transform, "HpBg", CardWidth - 24f, 15f, BarBgColor);
            Image hpFill = this.CreateStretchImage(hpBg.transform, "Fill", HpColor);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;

            Image mpBg = this.CreateImage(root.transform, "MpBg", CardWidth - 24f, 11f, BarBgColor);
            Image mpFill = this.CreateStretchImage(mpBg.transform, "Fill", MpColor);
            mpFill.type = Image.Type.Filled;
            mpFill.fillMethod = Image.FillMethod.Horizontal;

            Text hpText = this.CreateText(
                root.transform, "HpText", string.Empty, 12, Color.white,
                Vector2.zero, Vector2.zero, new Vector2(CardWidth - 12f, 20f));

            UnitCard card = new UnitCard
            {
                UnitId = unit.UnitId,
                Faction = unit.Faction,
                Root = root.transform,
                Bg = bg,
                HpFill = hpFill,
                MpFill = mpFill,
                HpText = hpText,
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
                    Destroy(card.Root);
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

            if (unit.IsDown && !card.Down)
            {
                this.MarkCardDown(card);
            }
        }

        private void MarkCardDown(UnitCard card)
        {
            card.Down = true;
            card.Group.alpha = 0.4f;
            card.Group.interactable = false;
            card.Root.localPosition = card.HomePos + new Vector3(0f, -14f, 0f);
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

                    string prefix = effect.ElementMultiplier > 1.05f ? "克制! "
                        : effect.ElementMultiplier < 0.95f ? "抵抗 " : string.Empty;
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
            // 数字键 1-4 主菜单快捷键（主菜单层可用时；子菜单开着时忽略防误触）
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

        private UnitCard GetCard(long unitId)
        {
            return this.cards.TryGetValue(unitId, out UnitCard card) ? card : null;
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

        private void StretchFull(GameObject go)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
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

        private void CreateMenuButton(Transform parent, string name, string label, System.Action onClick)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(148f, 52f);

            Image bg = go.AddComponent<Image>();
            bg.color = MenuBgColor;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());

            this.CreateText(
                go.transform, "Label", label, 24, Color.white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(144f, 48f));
        }

        #endregion
    }
}
