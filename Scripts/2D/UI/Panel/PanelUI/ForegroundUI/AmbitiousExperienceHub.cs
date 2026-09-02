namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Gameplay;
    using LAB2D.UnityAdapter;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 沉浸式会话体验中枢。
    /// 负责把已有的连击增益、波次反馈和会话结算数据整合到 Game.unity 中预置的 HUD、事件流和结算面板。
    /// 风险边界：本脚本只绑定场景中已有 UI，只读玩法数据，不在运行时创建界面。
    /// </summary>
    [DisallowMultipleComponent]
    public class AmbitiousExperienceHub : MonoBehaviour
    {
        private const string RightWaveHUDName = "RightWaveHUD";
        private const string EventFeedHUDName = "EventFeedHUD";
        private const string ResultCardName = "ResultCard";
        private const string WaveTextName = "WaveText";
        private const string ScoreTextName = "ScoreText";
        private const string EventFeedTextName = "EventFeedText";
        private const string ComboBurstTextName = "ComboBurstText";
        private const string ResultTitleTextName = "ResultTitleText";
        private const string ResultStatsTextName = "ResultStatsText";
        private const string ResultCloseButtonName = "ResultCloseButton";
        private const int MaxFeedCount = 6;

        private readonly Queue<string> feedMessages = new Queue<string>();

        private GameObject rightWaveHUD;
        private GameObject eventFeedHUD;
        private GameObject resultCard;
        private Text waveText;
        private Text scoreText;
        private Text eventFeedText;
        private Text comboBurstText;
        private Text resultTitleText;
        private Text resultStatsText;
        private Button resultCloseButton;
        private SessionResultData latestPreviewResult;
        private WaveFeedbackState latestWaveState;
        private float comboBurstHideTime;
        private float nextRefreshRealtime;
        private bool subscribed;
        private IGameLogger gameLogger;

        private IGameLogger GameLogger => this.gameLogger ?? (this.gameLogger = GameLoggerFactory.Get());

        /// <summary>
        /// 进入 Game 场景后是否自动显示 HUD。
        /// </summary>
        public bool showHudOnStart = true;

        /// <summary>
        /// 结算数据采集后是否自动弹出结算面板。
        /// </summary>
        public bool showResultOnCapture = true;

        /// <summary>
        /// HUD 刷新间隔，避免每帧拼接大量文本。
        /// </summary>
        public float refreshInterval = 0.25f;

        /// <summary>
        /// HUD 显示/隐藏热键。
        /// </summary>
        public KeyCode toggleHudKey = InputKeyConstant.ToggleExperienceHubHud;

        /// <summary>
        /// 最近结算/实时预览面板热键。
        /// </summary>
        public KeyCode toggleResultKey = InputKeyConstant.ToggleExperienceResultPanel;

        private void Awake()
        {
            if (Application.isPlaying && this.IsDuplicateRuntimeInstance())
            {
                Destroy(this.gameObject);
                return;
            }

            this.BindExistingInterface();
        }

        private void OnEnable()
        {
            if (!this.BindExistingInterface())
            {
                this.GameLogger.LogWarning("[AmbitiousExperienceHub] 未找到 Game.unity 中预置的体验中枢 UI 层级。");
                return;
            }

            this.SubscribeGameplayEvents();
            this.RefreshAll();

            this.SetHudVisible(this.showHudOnStart);
        }

        private void Start()
        {
            this.AddFeed("体验中枢已启动");
            this.RefreshAll();
        }

        private void Update()
        {
            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleHudKey))
            {
                bool currentVisible = this.rightWaveHUD != null && this.rightWaveHUD.activeSelf;
                this.SetHudVisible(!currentVisible);
            }

            if (UnityGlobalInputAdapter.GetHudToggleDown(this.toggleResultKey))
            {
                this.ToggleResultPanel();
            }

            if (this.comboBurstText != null && this.comboBurstText.gameObject.activeSelf &&
                Time.unscaledTime >= this.comboBurstHideTime)
            {
                this.comboBurstText.gameObject.SetActive(false);
            }

            // HUD 隐藏且结果面板未打开时不刷新：RefreshAll 是快照采集+多段大字符串拼接，
            // 不可见时纯属浪费；切回可见时由 SetHudVisible 置 0 补一次刷新（结果面板打开时仍照常刷新）。
            bool hudVisible = this.rightWaveHUD != null && this.rightWaveHUD.activeSelf;
            bool resultOpen = this.resultCard != null && this.resultCard.activeSelf;
            if (!hudVisible && !resultOpen)
            {
                return;
            }

            if (Time.unscaledTime >= this.nextRefreshRealtime)
            {
                this.nextRefreshRealtime = Time.unscaledTime + MathHelper.ClampRefreshInterval(this.refreshInterval);
                this.RefreshAll();
            }
        }

        private void OnDisable()
        {
            if (this.resultCloseButton != null)
            {
                this.resultCloseButton.onClick.RemoveListener(this.HideResultPanel);
            }

            this.UnsubscribeGameplayEvents();
        }

        /// <summary>
        /// 绑定已存在的 UI 层级。
        /// </summary>
        /// <returns>是否成功绑定。</returns>
        private bool BindExistingInterface()
        {
            // 从 Foreground 查找所有独立面板，不再依赖 ExperienceHUD 父节点
            Transform root = this.transform.parent;
            if (root == null)
            {
                return false;
            }

            this.rightWaveHUD = this.FindDeepChild(root, RightWaveHUDName)?.gameObject;
            this.eventFeedHUD = this.FindDeepChild(root, EventFeedHUDName)?.gameObject;
            this.resultCard = this.FindDeepChild(root, ResultCardName)?.gameObject;
            this.waveText = this.FindDeepComponent<Text>(root, WaveTextName);
            this.scoreText = this.FindDeepComponent<Text>(root, ScoreTextName);
            this.eventFeedText = this.FindDeepComponent<Text>(root, EventFeedTextName);
            this.comboBurstText = this.FindDeepComponent<Text>(root, ComboBurstTextName);
            this.resultTitleText = this.FindDeepComponent<Text>(root, ResultTitleTextName);
            this.resultStatsText = this.FindDeepComponent<Text>(root, ResultStatsTextName);
            this.resultCloseButton = this.FindDeepComponent<Button>(root, ResultCloseButtonName);
            if (this.resultCloseButton != null)
            {
                this.resultCloseButton.onClick.RemoveListener(this.HideResultPanel);
                this.resultCloseButton.onClick.AddListener(this.HideResultPanel);
            }

            return this.rightWaveHUD != null &&
                   this.eventFeedHUD != null &&
                   this.waveText != null &&
                   this.scoreText != null &&
                   this.eventFeedText != null &&
                   this.resultCard != null &&
                   this.resultTitleText != null &&
                   this.resultStatsText != null &&
                   this.resultCloseButton != null;
        }

        /// <summary>
        /// 订阅已有玩法系统事件。
        /// </summary>
        private void SubscribeGameplayEvents()
        {
            if (this.subscribed || !Application.isPlaying)
            {
                return;
            }

            this.subscribed = true;

            try
            {
                ServiceLocator.Get<GameplaySessionStats>().StatsChanged += this.HandleStatsChanged;
                GameplaySessionStatsSnapshot snapshot = ServiceLocator.Get<GameplaySessionStats>().CreateSnapshot();
                this.latestPreviewResult = SessionResultData.FromSnapshot(snapshot);
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[AmbitiousExperienceHub] 订阅会话统计失败: " + exception.Message);
            }

            try
            {
                ComboBonusManager combo = ServiceLocator.Get<ComboBonusManager>();
                int _ = combo.CurrentCombo;
                combo.OnComboMilestoneReached += this.HandleComboMilestoneReached;
                combo.OnComboBroken += this.HandleComboBroken;
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[AmbitiousExperienceHub] 订阅连击事件失败: " + exception.Message);
            }

            try
            {
                WaveEventFeedback feedback = ServiceLocator.Get<WaveEventFeedback>();
                feedback.Enable();
                this.latestWaveState = feedback.CurrentState;
                feedback.OnWaveFeedbackChanged += this.HandleWaveFeedbackChanged;
                feedback.OnWaveTipRequested += this.HandleWaveTipRequested;
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[AmbitiousExperienceHub] 订阅波次反馈失败: " + exception.Message);
            }

            try
            {
                ServiceLocator.Get<SessionResultManager>().OnResultCaptured += this.HandleResultCaptured;
            }
            catch (Exception exception)
            {
                this.GameLogger.LogWarning("[AmbitiousExperienceHub] 订阅结算事件失败: " + exception.Message);
            }
        }

        /// <summary>
        /// 取消订阅玩法系统事件，避免场景切换后残留委托。
        /// </summary>
        private void UnsubscribeGameplayEvents()
        {
            if (!this.subscribed)
            {
                return;
            }

            this.subscribed = false;

            try
            {
                ServiceLocator.Get<GameplaySessionStats>().StatsChanged -= this.HandleStatsChanged;
            }
            catch (Exception)
            {
            }

            try
            {
                ComboBonusManager combo = ServiceLocator.Get<ComboBonusManager>();
                combo.OnComboMilestoneReached -= this.HandleComboMilestoneReached;
                combo.OnComboBroken -= this.HandleComboBroken;
            }
            catch (Exception)
            {
            }

            try
            {
                WaveEventFeedback feedback = ServiceLocator.Get<WaveEventFeedback>();
                feedback.OnWaveFeedbackChanged -= this.HandleWaveFeedbackChanged;
                feedback.OnWaveTipRequested -= this.HandleWaveTipRequested;
            }
            catch (Exception)
            {
            }

            try
            {
                ServiceLocator.Get<SessionResultManager>().OnResultCaptured -= this.HandleResultCaptured;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 刷新全部显示内容。
        /// </summary>
        private void RefreshAll()
        {
            if (!Application.isPlaying)
            {
                this.RefreshEditorPreview();
                return;
            }

            this.RefreshSnapshot();
            this.RefreshWaveText();
            this.RefreshScoreText();
            this.RefreshEventFeedText();
        }

        /// <summary>
        /// 编辑态显示默认文案。
        /// </summary>
        private void RefreshEditorPreview()
        {
            if (this.waveText != null)
            {
                this.waveText.text = "波次：等待启动\n状态：未接入运行时数据";
            }

            if (this.scoreText != null)
            {
                this.scoreText.text = "实时评分 0 / 10000\n星级 ☆☆☆☆☆ | 评级 D";
            }

            if (this.eventFeedText != null)
            {
                this.eventFeedText.text = "等待战斗、波次和结算事件";
            }
        }

        /// <summary>
        /// 刷新会话统计快照。
        /// </summary>
        private void RefreshSnapshot()
        {
            try
            {
                GameplaySessionStatsSnapshot snapshot = ServiceLocator.Get<GameplaySessionStats>().CreateSnapshot();
                this.latestPreviewResult = SessionResultData.FromSnapshot(snapshot);
            }
            catch (Exception)
            {
                this.latestPreviewResult = null;
            }
        }

        /// <summary>
        /// 刷新波次状态面板。
        /// </summary>
        private void RefreshWaveText()
        {
            if (this.waveText == null)
            {
                return;
            }

            WaveFeedbackState state = this.latestWaveState;
            try
            {
                if (state == null)
                {
                    state = ServiceLocator.Get<WaveEventFeedback>().CurrentState;
                }
            }
            catch (Exception)
            {
                state = null;
            }

            if (state == null || state.currentWaveIndex <= 0)
            {
                this.waveText.text = "波次：未启动\n状态：常规探索";
                return;
            }

            string status = state.isResting
                ? $"休息中 {state.restRemaining:0}s"
                : state.isWaveActive
                    ? "战斗中"
                    : "等待下一波";

            this.waveText.text =
                $"第 <b>{state.currentWaveIndex}</b> 波 | {status}\n" +
                $"完成 {state.totalWavesCompleted} 波 | 存活敌人 {state.enemiesAliveInWave} | 难度 {state.difficultyScale:0.00}x";
        }

        /// <summary>
        /// 刷新实时评分预览。
        /// </summary>
        private void RefreshScoreText()
        {
            if (this.scoreText == null)
            {
                return;
            }

            if (this.latestPreviewResult == null)
            {
                this.scoreText.text = "实时评分 0 / 10000\n星级 ☆☆☆☆☆ | 评级 D\n连击增益 1.00x 伤害 / 1.00x 经验";
                return;
            }

            float damageMultiplier = 1f;
            float experienceMultiplier = 1f;
            try
            {
                damageMultiplier = ServiceLocator.Get<ComboBonusManager>().DamageMultiplier;
                experienceMultiplier = ServiceLocator.Get<ComboBonusManager>().ExperienceMultiplier;
            }
            catch (Exception)
            {
            }

            this.scoreText.text =
                $"实时评分 <b>{this.latestPreviewResult.CombatScore}</b> / 10000\n" +
                $"星级 {this.BuildStars(this.latestPreviewResult.StarRating)} | 评级 {this.latestPreviewResult.GradeText}\n" +
                $"连击增益 {damageMultiplier:0.00}x 伤害 / {experienceMultiplier:0.00}x 经验";
        }

        /// <summary>
        /// 刷新事件流文本。
        /// </summary>
        private void RefreshEventFeedText()
        {
            if (this.eventFeedText == null)
            {
                return;
            }

            if (this.feedMessages.Count == 0)
            {
                this.eventFeedText.text = "等待战斗、波次和结算事件";
                return;
            }

            this.eventFeedText.text = string.Join("\n", this.feedMessages.ToArray());
        }

        /// <summary>
        /// 会话统计变更回调。
        /// </summary>
        /// <param name="snapshot">最新快照。</param>
        private void HandleStatsChanged(GameplaySessionStatsSnapshot snapshot)
        {
            this.latestPreviewResult = SessionResultData.FromSnapshot(snapshot);
            this.RefreshScoreText();
        }

        /// <summary>
        /// 连击里程碑回调。
        /// </summary>
        private void HandleComboMilestoneReached(int combo, float damageMultiplier, float experienceMultiplier)
        {
            string text = $"连击 x{combo}  伤害 {damageMultiplier:0.00}x  经验 {experienceMultiplier:0.00}x";
            this.AddFeed(text);
            this.ShowComboBurst(text);
        }

        /// <summary>
        /// 连击中断回调。
        /// </summary>
        private void HandleComboBroken(int maxCombo)
        {
            this.AddFeed($"连击中断，最高 {maxCombo}");
        }

        /// <summary>
        /// 波次状态变更回调。
        /// </summary>
        private void HandleWaveFeedbackChanged(WaveFeedbackState state)
        {
            this.latestWaveState = state;
            this.RefreshWaveText();
        }

        /// <summary>
        /// 波次提示回调。
        /// </summary>
        private void HandleWaveTipRequested(string message)
        {
            this.AddFeed(message);
        }

        /// <summary>
        /// 结算采集回调。
        /// </summary>
        private void HandleResultCaptured(SessionResultData result)
        {
            if (result == null)
            {
                return;
            }

            this.AddFeed($"结算完成：{result.CombatScore} 分 / {result.GradeText} / {result.StarRating}星");
            if (this.showResultOnCapture)
            {
                this.ShowResultPanel(result, false);
            }
        }

        /// <summary>
        /// 显示结算面板。
        /// </summary>
        private void ShowResultPanel(SessionResultData result, bool isPreview)
        {
            if (result == null || this.resultCard == null)
            {
                return;
            }

            this.resultCard.SetActive(true);
            if (this.resultTitleText != null)
            {
                this.resultTitleText.text = isPreview ? "实时表现预览" : "关卡结算";
            }

            if (this.resultStatsText != null)
            {
                this.resultStatsText.text =
                    $"<size=34><b>{result.CombatScore}</b></size> / 10000    {this.BuildStars(result.StarRating)}    评级 <b>{result.GradeText}</b>\n\n" +
                    $"会话时长：{this.FormatDuration(result.SessionDuration)}\n" +
                    $"击杀敌人：{result.TotalDefeatedEnemyCount}    最高连击：{result.MaxCombo}    暴击：{result.CriticalHitCount}\n" +
                    $"造成伤害：{result.TotalDamageDealt}    承受伤害：{result.TotalDamageTaken}    效率：{result.DamageEfficiency:0.0}x\n" +
                    $"经验获取：{result.TotalExperienceGained}    收集物品：{result.TotalCollectedItemCount}    工人任务：{result.TotalWorkerTaskCompletedCount}\n" +
                    $"玩家死亡：{result.PlayerDeathCount}    工人死亡：{result.TotalWorkerDeathCount}    存活：{(result.HasSurvived ? "是" : "否")}";
            }
        }

        /// <summary>
        /// 隐藏结算面板。
        /// </summary>
        private void HideResultPanel()
        {
            if (this.resultCard != null)
            {
                this.resultCard.SetActive(false);
            }
        }

        /// <summary>
        /// 切换结算面板，优先显示真实结算，没有结算时显示实时预览。
        /// </summary>
        private void ToggleResultPanel()
        {
            if (this.resultCard != null && this.resultCard.activeSelf)
            {
                this.HideResultPanel();
                return;
            }

            SessionResultData latest = null;
            try
            {
                latest = ServiceLocator.Get<SessionResultManager>().LatestResult;
            }
            catch (Exception)
            {
            }

            if (latest != null)
            {
                this.ShowResultPanel(latest, false);
                return;
            }

            this.ShowResultPanel(this.latestPreviewResult, true);
        }

        /// <summary>
        /// 显示中央连击强调文字。
        /// </summary>
        private void ShowComboBurst(string message)
        {
            if (this.comboBurstText == null)
            {
                return;
            }

            this.comboBurstText.text = message;
            this.comboBurstText.gameObject.SetActive(true);
            this.comboBurstHideTime = Time.unscaledTime + 1.4f;
        }

        /// <summary>
        /// 添加事件流消息。
        /// </summary>
        private void AddFeed(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            string line = $"{DateTime.Now:HH:mm:ss}  {message}";
            this.feedMessages.Enqueue(line);
            while (this.feedMessages.Count > MaxFeedCount)
            {
                this.feedMessages.Dequeue();
            }

            this.RefreshEventFeedText();
        }

        /// <summary>
        /// 设置 HUD 显隐，不影响结算面板按钮交互。
        /// </summary>
        private void SetHudVisible(bool visible)
        {
            if (this.rightWaveHUD != null)
            {
                this.rightWaveHUD.SetActive(visible);
            }

            if (this.eventFeedHUD != null)
            {
                this.eventFeedHUD.SetActive(visible);
            }

            // 重新显示时立即刷新（隐藏期间不刷新会残留旧数据），置 0 让下帧 Update 放行
            if (visible)
            {
                this.nextRefreshRealtime = 0f;
            }
        }

        /// <summary>
        /// 查找深层子物体。
        /// </summary>
        private Transform FindDeepChild(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 查找深层子组件。
        /// </summary>
        private T FindDeepComponent<T>(Transform root, string objectName)
            where T : Component
        {
            Transform child = this.FindDeepChild(root, objectName);
            return child == null ? null : child.GetComponent<T>();
        }

        /// <summary>
        /// 判断当前实例是否为重复运行时实例。
        /// </summary>
        private bool IsDuplicateRuntimeInstance()
        {
            AmbitiousExperienceHub[] hubs = FindObjectsOfType<AmbitiousExperienceHub>();
            foreach (AmbitiousExperienceHub hub in hubs)
            {
                if (hub != null && hub != this)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断是否可处理热键。
        /// </summary>
        private bool CanUseHotkey()
        {
            return UnityGlobalInputAdapter.CanUseHudHotkey();
        }

        /// <summary>
        /// 构建星级文本。
        /// </summary>
        private string BuildStars(int starRating)
        {
            int stars = MathHelper.Clamp(starRating, 0, 5);
            return new string('★', stars) + new string('☆', 5 - stars);
        }

        /// <summary>
        /// 格式化会话时长。
        /// </summary>
        private string FormatDuration(float seconds)
        {
            int totalSeconds = System.Math.Max(0, MathHelper.RoundToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;
            return $"{minutes:D2}:{remainSeconds:D2}";
        }
    }
}
