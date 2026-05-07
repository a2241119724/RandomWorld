namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    /// <summary>
    /// 沉浸式会话体验中枢。
    /// 负责把已有的战斗统计、连击增益、波次反馈和会话结算数据整合成玩家可见的 HUD、事件流和结算面板。
    /// 风险边界：本脚本只读现有玩法数据，不修改存档、Photon、波次生成、评分算法和已有 UI 层级。
    /// </summary>
    [DisallowMultipleComponent]
    public class AmbitiousExperienceHub : MonoBehaviour
    {
        private const string GameSceneName = "Game";
        private const string RuntimeRootName = "Ambitious_A001_ExperienceHub_Runtime";
        private const string SceneRootName = "Ambitious_A001_ExperienceHub_Root";
        private const string CanvasName = "Ambitious_A001_ExperienceHub_Canvas";
        private const string HudRootName = "Ambitious_A001_HUD_Root";
        private const string ResultPanelName = "Ambitious_A001_ResultPanel";
        private const string StatsTextName = "Ambitious_A001_StatsText";
        private const string WaveTextName = "Ambitious_A001_WaveText";
        private const string ScoreTextName = "Ambitious_A001_ScoreText";
        private const string EventFeedTextName = "Ambitious_A001_EventFeedText";
        private const string ComboBurstTextName = "Ambitious_A001_ComboBurstText";
        private const string ResultTitleTextName = "Ambitious_A001_ResultTitleText";
        private const string ResultStatsTextName = "Ambitious_A001_ResultStatsText";
        private const string HpFillName = "Ambitious_A001_HpFill";
        private const string MpFillName = "Ambitious_A001_MpFill";
        private const string EnergyFillName = "Ambitious_A001_EnergyFill";
        private const int MaxFeedCount = 6;

        private static bool sceneHooked;

        private readonly Queue<string> feedMessages = new Queue<string>();

        private Canvas canvas;
        private CanvasGroup hudGroup;
        private GameObject canvasObject;
        private GameObject hudRoot;
        private GameObject resultPanel;
        private Font defaultFont;
        private Image hpFill;
        private Image mpFill;
        private Image energyFill;
        private Text statsText;
        private Text waveText;
        private Text scoreText;
        private Text eventFeedText;
        private Text comboBurstText;
        private Text resultTitleText;
        private Text resultStatsText;
        private GameplaySessionStatsSnapshot latestSnapshot;
        private SessionResultData latestPreviewResult;
        private WaveFeedbackState latestWaveState;
        private float comboBurstHideTime;
        private float nextRefreshRealtime;
        private bool subscribed;

        /// <summary>
        /// 是否在 Awake 时自动构建 UI。
        /// Editor 生成 Prefab 时也保留为 true，实例进入 Play Mode 后会自动绑定或构建界面。
        /// </summary>
        public bool autoBuildOnAwake = true;

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
        public KeyCode toggleHudKey = KeyCode.F2;

        /// <summary>
        /// 最近结算/实时预览面板热键。
        /// </summary>
        public KeyCode toggleResultKey = KeyCode.F3;

        /// <summary>
        /// 运行时自动引导入口，只在 Game 场景创建独立根节点。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntime()
        {
            HookSceneLoaded();
            TryCreateForActiveGameScene();
        }

        /// <summary>
        /// 注册场景加载回调，保证从菜单切到 Game 场景时也能自动创建体验中枢。
        /// </summary>
        private static void HookSceneLoaded()
        {
            if (sceneHooked)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHooked = true;
        }

        /// <summary>
        /// 场景加载后尝试创建运行时根节点。
        /// </summary>
        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameSceneName)
            {
                TryCreateForActiveGameScene();
            }
        }

        /// <summary>
        /// 在 Game 场景中创建唯一的体验中枢实例。
        /// </summary>
        private static void TryCreateForActiveGameScene()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != GameSceneName)
            {
                return;
            }

            if (FindObjectOfType<AmbitiousExperienceHub>() != null)
            {
                return;
            }

            GameObject root = new GameObject(RuntimeRootName);
            root.AddComponent<AmbitiousExperienceHub>();
        }

        private void Awake()
        {
            if (Application.isPlaying && this.IsDuplicateRuntimeInstance())
            {
                Destroy(this.gameObject);
                return;
            }

            if (this.gameObject.name != SceneRootName && this.gameObject.name != RuntimeRootName)
            {
                this.gameObject.name = SceneRootName;
            }

            if (this.autoBuildOnAwake)
            {
                this.BuildInterface();
            }
        }

        private void OnEnable()
        {
            if (this.autoBuildOnAwake)
            {
                this.BuildInterface();
            }

            this.SubscribeGameplayEvents();
            this.RefreshAll(true);

            if (this.hudGroup != null)
            {
                this.SetHudVisible(this.showHudOnStart);
            }
        }

        private void Start()
        {
            this.AddFeed("体验中枢已启动");
            this.RefreshAll(true);
        }

        private void Update()
        {
            if (this.CanUseHotkey() && Input.GetKeyDown(this.toggleHudKey))
            {
                this.SetHudVisible(this.hudGroup == null || this.hudGroup.alpha < 0.5f);
            }

            if (this.CanUseHotkey() && Input.GetKeyDown(this.toggleResultKey))
            {
                this.ToggleResultPanel();
            }

            if (this.comboBurstText != null && this.comboBurstText.gameObject.activeSelf &&
                Time.unscaledTime >= this.comboBurstHideTime)
            {
                this.comboBurstText.gameObject.SetActive(false);
            }

            if (Time.unscaledTime >= this.nextRefreshRealtime)
            {
                this.nextRefreshRealtime = Time.unscaledTime + Mathf.Max(0.05f, this.refreshInterval);
                this.RefreshAll(false);
            }
        }

        private void OnDisable()
        {
            this.UnsubscribeGameplayEvents();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor 菜单生成场景节点或 Prefab 时调用，用于在编辑态创建完整 UI 层级。
        /// </summary>
        public void BuildPreviewForEditor()
        {
            this.ClearGeneratedInterface();
            this.BuildInterface();
            this.RefreshAll(true);
        }
#endif

        /// <summary>
        /// 构建或绑定体验中枢 UI。
        /// 如果 Prefab 中已有完整 UI 层级，则只重新绑定引用，避免重复创建。
        /// </summary>
        private void BuildInterface()
        {
            if (this.BindExistingInterface())
            {
                this.EnsureEventSystem();
                return;
            }

            this.defaultFont = this.LoadDefaultFont();
            this.EnsureEventSystem();

            this.canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            this.canvasObject.transform.SetParent(this.transform, false);
            this.canvas = this.canvasObject.GetComponent<Canvas>();
            this.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            this.canvas.sortingOrder = 900;

            CanvasScaler scaler = this.canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            this.BuildHud();
            this.BuildResultPanel();
            this.RefreshAll(true);
        }

        /// <summary>
        /// 绑定已存在的 UI 层级。
        /// </summary>
        /// <returns>是否成功绑定。</returns>
        private bool BindExistingInterface()
        {
            Transform canvasTransform = this.transform.Find(CanvasName);
            if (canvasTransform == null)
            {
                return false;
            }

            this.canvasObject = canvasTransform.gameObject;
            this.canvas = this.canvasObject.GetComponent<Canvas>();
            this.hudRoot = this.FindDeepChild(canvasTransform, HudRootName)?.gameObject;
            this.resultPanel = this.FindDeepChild(canvasTransform, ResultPanelName)?.gameObject;
            this.hudGroup = this.hudRoot == null ? null : this.hudRoot.GetComponent<CanvasGroup>();
            this.statsText = this.FindDeepComponent<Text>(canvasTransform, StatsTextName);
            this.waveText = this.FindDeepComponent<Text>(canvasTransform, WaveTextName);
            this.scoreText = this.FindDeepComponent<Text>(canvasTransform, ScoreTextName);
            this.eventFeedText = this.FindDeepComponent<Text>(canvasTransform, EventFeedTextName);
            this.comboBurstText = this.FindDeepComponent<Text>(canvasTransform, ComboBurstTextName);
            this.resultTitleText = this.FindDeepComponent<Text>(canvasTransform, ResultTitleTextName);
            this.resultStatsText = this.FindDeepComponent<Text>(canvasTransform, ResultStatsTextName);
            this.hpFill = this.FindDeepComponent<Image>(canvasTransform, HpFillName);
            this.mpFill = this.FindDeepComponent<Image>(canvasTransform, MpFillName);
            this.energyFill = this.FindDeepComponent<Image>(canvasTransform, EnergyFillName);
            this.defaultFont = this.LoadDefaultFont();

            return this.canvas != null &&
                   this.hudRoot != null &&
                   this.resultPanel != null &&
                   this.statsText != null &&
                   this.waveText != null &&
                   this.scoreText != null &&
                   this.eventFeedText != null &&
                   this.resultTitleText != null &&
                   this.resultStatsText != null;
        }

        /// <summary>
        /// 构建 HUD、事件流和连击提示。
        /// </summary>
        private void BuildHud()
        {
            this.hudRoot = this.CreatePanel(
                HudRootName,
                this.canvasObject.transform,
                new Color32(0, 0, 0, 0),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            this.hudGroup = this.hudRoot.AddComponent<CanvasGroup>();
            this.hudGroup.blocksRaycasts = false;
            this.hudGroup.interactable = false;

            GameObject leftPanel = this.CreatePanel(
                "Ambitious_A001_LeftStatsPanel",
                this.hudRoot.transform,
                new Color32(18, 22, 28, 210),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(560f, 300f));

            this.CreateText(
                "Ambitious_A001_HudTitle",
                leftPanel.transform,
                "会话态势",
                26,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color32(255, 226, 138, 255),
                new Vector2(24f, -18f),
                new Vector2(510f, 34f));

            this.statsText = this.CreateText(
                StatsTextName,
                leftPanel.transform,
                string.Empty,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white,
                new Vector2(24f, -58f),
                new Vector2(510f, 128f));

            this.hpFill = this.CreateBar(leftPanel.transform, "HP", HpFillName, new Vector2(24f, -198f), new Color32(231, 86, 86, 255));
            this.mpFill = this.CreateBar(leftPanel.transform, "MP", MpFillName, new Vector2(24f, -232f), new Color32(84, 152, 255, 255));
            this.energyFill = this.CreateBar(leftPanel.transform, "灵气", EnergyFillName, new Vector2(24f, -266f), new Color32(99, 216, 160, 255));

            GameObject rightPanel = this.CreatePanel(
                "Ambitious_A001_RightWavePanel",
                this.hudRoot.transform,
                new Color32(14, 18, 24, 205),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(520f, 220f));

            this.waveText = this.CreateText(
                WaveTextName,
                rightPanel.transform,
                string.Empty,
                20,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color32(174, 224, 255, 255),
                new Vector2(22f, -18f),
                new Vector2(476f, 78f));

            this.scoreText = this.CreateText(
                ScoreTextName,
                rightPanel.transform,
                string.Empty,
                18,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                Color.white,
                new Vector2(22f, -104f),
                new Vector2(476f, 92f));

            GameObject feedPanel = this.CreatePanel(
                "Ambitious_A001_EventFeedPanel",
                this.hudRoot.transform,
                new Color32(10, 12, 18, 190),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 24f),
                new Vector2(520f, 210f));

            this.CreateText(
                "Ambitious_A001_EventFeedTitle",
                feedPanel.transform,
                "近期反馈",
                22,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color32(255, 226, 138, 255),
                new Vector2(22f, -16f),
                new Vector2(476f, 30f));

            this.eventFeedText = this.CreateText(
                EventFeedTextName,
                feedPanel.transform,
                string.Empty,
                17,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color32(230, 237, 246, 255),
                new Vector2(22f, -52f),
                new Vector2(476f, 136f));

            this.comboBurstText = this.CreateText(
                ComboBurstTextName,
                this.hudRoot.transform,
                string.Empty,
                42,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color32(255, 226, 138, 255),
                new Vector2(0f, -118f),
                new Vector2(560f, 96f));
            this.SetRect(
                this.comboBurstText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -118f),
                new Vector2(560f, 96f));
            this.comboBurstText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 构建结算面板。
        /// </summary>
        private void BuildResultPanel()
        {
            this.resultPanel = this.CreatePanel(
                ResultPanelName,
                this.canvasObject.transform,
                new Color32(5, 8, 12, 180),
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            GameObject card = this.CreatePanel(
                "Ambitious_A001_ResultCard",
                this.resultPanel.transform,
                new Color32(20, 24, 31, 245),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(820f, 650f));

            this.resultTitleText = this.CreateText(
                ResultTitleTextName,
                card.transform,
                "关卡结算",
                34,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                new Color32(255, 226, 138, 255),
                new Vector2(0f, -34f),
                new Vector2(760f, 54f));
            this.SetRect(
                this.resultTitleText.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(760f, 54f));

            this.resultStatsText = this.CreateText(
                ResultStatsTextName,
                card.transform,
                string.Empty,
                22,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                new Color32(238, 244, 252, 255),
                new Vector2(40f, -118f),
                new Vector2(740f, 420f));

            Button closeButton = this.CreateButton(
                "Ambitious_A001_ResultCloseButton",
                card.transform,
                "继续",
                new Vector2(0f, 44f),
                new Vector2(260f, 62f));
            closeButton.onClick.AddListener(this.HideResultPanel);

            this.resultPanel.SetActive(false);
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
                GameplaySessionStats.Instance.StatsChanged += this.HandleStatsChanged;
                this.latestSnapshot = GameplaySessionStats.Instance.CreateSnapshot();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AmbitiousExperienceHub] 订阅会话统计失败: " + exception.Message);
            }

            try
            {
                ComboBonusManager combo = ComboBonusManager.Instance;
                int _ = combo.CurrentCombo;
                combo.OnComboMilestoneReached += this.HandleComboMilestoneReached;
                combo.OnComboBroken += this.HandleComboBroken;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AmbitiousExperienceHub] 订阅连击事件失败: " + exception.Message);
            }

            try
            {
                WaveEventFeedback feedback = WaveEventFeedback.Instance;
                feedback.Enable();
                this.latestWaveState = feedback.CurrentState;
                feedback.OnWaveFeedbackChanged += this.HandleWaveFeedbackChanged;
                feedback.OnWaveTipRequested += this.HandleWaveTipRequested;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AmbitiousExperienceHub] 订阅波次反馈失败: " + exception.Message);
            }

            try
            {
                SessionResultManager.Instance.OnResultCaptured += this.HandleResultCaptured;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AmbitiousExperienceHub] 订阅结算事件失败: " + exception.Message);
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
                GameplaySessionStats.Instance.StatsChanged -= this.HandleStatsChanged;
            }
            catch (Exception)
            {
            }

            try
            {
                ComboBonusManager combo = ComboBonusManager.Instance;
                combo.OnComboMilestoneReached -= this.HandleComboMilestoneReached;
                combo.OnComboBroken -= this.HandleComboBroken;
            }
            catch (Exception)
            {
            }

            try
            {
                WaveEventFeedback feedback = WaveEventFeedback.Instance;
                feedback.OnWaveFeedbackChanged -= this.HandleWaveFeedbackChanged;
                feedback.OnWaveTipRequested -= this.HandleWaveTipRequested;
            }
            catch (Exception)
            {
            }

            try
            {
                SessionResultManager.Instance.OnResultCaptured -= this.HandleResultCaptured;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 刷新全部显示内容。
        /// </summary>
        /// <param name="force">是否强制刷新。</param>
        private void RefreshAll(bool force)
        {
            if (!Application.isPlaying)
            {
                this.RefreshEditorPreview();
                return;
            }

            this.RefreshSnapshot();
            this.RefreshStatsText();
            this.RefreshPlayerBars();
            this.RefreshWaveText();
            this.RefreshScoreText();
            this.RefreshEventFeedText();
        }

        /// <summary>
        /// 编辑态生成 Prefab 时显示默认文案。
        /// </summary>
        private void RefreshEditorPreview()
        {
            if (this.statsText != null)
            {
                this.statsText.text = "击杀 0 | 当前连击 0 | 最高连击 0\n伤害 0 / 承伤 0 | 经验 0\n收集 0 | 工人任务 0 | 玩家死亡 0";
            }

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
                this.latestSnapshot = GameplaySessionStats.Instance.CreateSnapshot();
                this.latestPreviewResult = SessionResultData.FromSnapshot(this.latestSnapshot);
            }
            catch (Exception)
            {
                this.latestSnapshot = null;
                this.latestPreviewResult = null;
            }
        }

        /// <summary>
        /// 刷新左侧统计面板。
        /// </summary>
        private void RefreshStatsText()
        {
            if (this.statsText == null || this.latestSnapshot == null)
            {
                return;
            }

            GameplaySessionStatsSnapshot s = this.latestSnapshot;
            this.statsText.text =
                $"时长 {this.FormatDuration(s.SessionDuration)} | 击杀 <b>{s.TotalDefeatedEnemyCount}</b> | 连击 <b>{s.CurrentCombo}</b>\n" +
                $"最高连击 {s.MaxCombo} | 暴击 {s.CriticalHitCount} | 玩家死亡 {s.PlayerDeathCount}\n" +
                $"伤害 {s.TotalDamageDealt} / 承伤 {s.TotalDamageTaken} | 经验 {s.TotalExperienceGained}\n" +
                $"收集 {s.TotalCollectedItemCount} | 工人任务 {s.TotalWorkerTaskCompletedCount} | 工人死亡 {s.TotalWorkerDeathCount}";
        }

        /// <summary>
        /// 刷新玩家生存条和环境灵气条。
        /// </summary>
        private void RefreshPlayerBars()
        {
            float hpRatio = 0f;
            float mpRatio = 0f;
            float energyRatio = 0f;

            try
            {
                Player player = PlayerManager.Instance.Mine;
                if (player != null && player.CharacterDataLAB != null)
                {
                    hpRatio = this.SafeRatio(player.CharacterDataLAB.Hp, player.CharacterDataLAB.MaxHp);
                    mpRatio = this.SafeRatio(player.CharacterDataLAB.Mp, player.CharacterDataLAB.MaxMp);
                }
            }
            catch (Exception)
            {
            }

            try
            {
                energyRatio = this.SafeRatio(EnvironmentManager.Instance.CurEnergy, EnvironmentManager.Instance.MaxEnergy);
            }
            catch (Exception)
            {
            }

            this.SetFill(this.hpFill, hpRatio);
            this.SetFill(this.mpFill, mpRatio);
            this.SetFill(this.energyFill, energyRatio);
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
                    state = WaveEventFeedback.Instance.CurrentState;
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
                damageMultiplier = ComboBonusManager.Instance.DamageMultiplier;
                experienceMultiplier = ComboBonusManager.Instance.ExperienceMultiplier;
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
            this.latestSnapshot = snapshot;
            this.latestPreviewResult = SessionResultData.FromSnapshot(snapshot);
            this.RefreshStatsText();
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
            if (result == null || this.resultPanel == null)
            {
                return;
            }

            this.resultPanel.SetActive(true);
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
            if (this.resultPanel != null)
            {
                this.resultPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 切换结算面板，优先显示真实结算，没有结算时显示实时预览。
        /// </summary>
        private void ToggleResultPanel()
        {
            if (this.resultPanel != null && this.resultPanel.activeSelf)
            {
                this.HideResultPanel();
                return;
            }

            SessionResultData latest = null;
            try
            {
                latest = SessionResultManager.Instance.LatestResult;
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
            if (this.hudGroup == null)
            {
                return;
            }

            this.hudGroup.alpha = visible ? 1f : 0f;
            this.hudGroup.interactable = false;
            this.hudGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 创建 UI 面板。
        /// </summary>
        private GameObject CreatePanel(
            string objectName,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = objectName == ResultPanelName;
            this.SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, size);
            return go;
        }

        /// <summary>
        /// 创建文本。
        /// </summary>
        private Text CreateText(
            string objectName,
            Transform parent,
            string text,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.text = text;
            if (this.defaultFont == null)
            {
                this.defaultFont = this.LoadDefaultFont();
            }

            label.font = this.defaultFont;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            this.SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
            return label;
        }

        /// <summary>
        /// 创建条形进度。
        /// </summary>
        private Image CreateBar(Transform parent, string label, string fillName, Vector2 anchoredPosition, Color fillColor)
        {
            this.CreateText(
                "Ambitious_A001_" + label + "_Label",
                parent,
                label,
                16,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color32(224, 231, 242, 255),
                anchoredPosition,
                new Vector2(56f, 24f));

            GameObject back = this.CreatePanel(
                "Ambitious_A001_" + label + "_Back",
                parent,
                new Color32(255, 255, 255, 36),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                anchoredPosition + new Vector2(62f, 0f),
                new Vector2(440f, 22f));

            GameObject fill = this.CreatePanel(
                fillName,
                back.transform,
                fillColor,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(440f, 22f));

            return fill.GetComponent<Image>();
        }

        /// <summary>
        /// 创建按钮。
        /// </summary>
        private Button CreateButton(string objectName, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color32(65, 116, 179, 255);
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(84, 145, 210, 255);
            colors.pressedColor = new Color32(42, 82, 132, 255);
            button.colors = colors;
            this.SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, size);

            Text buttonText = this.CreateText(
                objectName + "_Text",
                go.transform,
                label,
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white,
                Vector2.zero,
                size);
            this.SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        /// <summary>
        /// 加载 Unity 内置字体。
        /// 新版 Unity 已废弃 Arial.ttf，因此优先使用 LegacyRuntime.ttf；旧版本再回退 Arial.ttf。
        /// 两者都不可用时返回 null，避免字体加载异常打断体验中枢初始化。
        /// </summary>
        private Font LoadDefaultFont()
        {
            Font font = this.TryLoadBuiltinFont("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return this.TryLoadBuiltinFont("Arial.ttf");
        }

        /// <summary>
        /// 尝试加载指定内置字体，吞掉版本差异导致的异常。
        /// </summary>
        /// <param name="fontName">内置字体名称。</param>
        /// <returns>字体资源，失败时返回 null。</returns>
        private Font TryLoadBuiltinFont(string fontName)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(fontName);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// 设置 RectTransform 布局。
        /// </summary>
        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// 设置进度条填充宽度。
        /// </summary>
        private void SetFill(Image fill, float ratio)
        {
            if (fill == null)
            {
                return;
            }

            RectTransform rect = fill.rectTransform;
            rect.sizeDelta = new Vector2(440f * Mathf.Clamp01(ratio), rect.sizeDelta.y);
        }

        /// <summary>
        /// 确保独立 UI 有 EventSystem，避免结算面板按钮无法点击。
        /// </summary>
        private void EnsureEventSystem()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("Ambitious_A001_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(this.transform, false);
        }

        /// <summary>
        /// 清理已生成的 UI 层级。
        /// </summary>
        private void ClearGeneratedInterface()
        {
            Transform canvasTransform = this.transform.Find(CanvasName);
            if (canvasTransform == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(canvasTransform.gameObject);
            }
            else
            {
                DestroyImmediate(canvasTransform.gameObject);
            }

            this.canvasObject = null;
            this.canvas = null;
            this.hudRoot = null;
            this.resultPanel = null;
            this.hudGroup = null;
            this.statsText = null;
            this.waveText = null;
            this.scoreText = null;
            this.eventFeedText = null;
            this.comboBurstText = null;
            this.resultTitleText = null;
            this.resultStatsText = null;
            this.hpFill = null;
            this.mpFill = null;
            this.energyFill = null;
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
            try
            {
                return !Tool.IsUIInputActive();
            }
            catch (Exception)
            {
                return true;
            }
        }

        /// <summary>
        /// 计算安全比例。
        /// </summary>
        private float SafeRatio(float current, float max)
        {
            if (max <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(current / max);
        }

        /// <summary>
        /// 构建星级文本。
        /// </summary>
        private string BuildStars(int starRating)
        {
            int stars = Mathf.Clamp(starRating, 0, 5);
            return new string('★', stars) + new string('☆', 5 - stars);
        }

        /// <summary>
        /// 格式化会话时长。
        /// </summary>
        private string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;
            return $"{minutes:D2}:{remainSeconds:D2}";
        }
    }
}
