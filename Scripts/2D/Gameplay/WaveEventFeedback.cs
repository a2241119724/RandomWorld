namespace LAB2D.Gameplay
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Wave;
    using System;

    /// <summary>
    /// 波次事件反馈管理器。
    /// 订阅 WaveManager 的5个公开事件，在波次关键节点向玩家提供即时文字提示反馈。
    /// 同时维护波次状态数据，供 HUD 或其他系统查询和订阅。
    /// 运行时非 MonoBehaviour 单例，不保存数据，不涉及网络同步。
    ///
    /// 接入方式：
    ///   - 自动：首次访问 Instance 时自动订阅 WaveManager 事件（延迟初始化，避免构造顺序竞态）
    ///   - 手动启动：调用 WaveEventFeedback.Instance.Enable() 显式激活
    ///   - 手动停止：调用 WaveEventFeedback.Instance.Disable() 取消订阅
    ///   - HUD 数据源：WaveEventFeedback.Instance.CurrentState
    ///   - 外部订阅：WaveEventFeedback.Instance.OnWaveFeedbackChanged
    ///   - Editor 菜单：工具 > 波次事件反馈 > ...
    ///
    /// 风险边界：
    ///   - 不修改任何已有文件，仅新增独立脚本
    ///   - 不涉及 Scene/Prefab/SO/存档/Photon/AssetBundle
    ///   - Tip 反馈使用已有 GlobalInit.ShowTip 接口，缺失时自动降级为 Debug.Log
    /// </summary>
    public class WaveEventFeedback : Singleton<WaveEventFeedback>
    {
        /// <summary>
        /// 波次反馈状态数据，供 HUD 或外部系统使用
        /// </summary>
        public WaveFeedbackState CurrentState { get; private set; }

        /// <summary>
        /// 波次反馈状态变更事件，HUD 可订阅此事件刷新波次信息显示
        /// </summary>
        public event Action<WaveFeedbackState> OnWaveFeedbackChanged;

        /// <summary>
        /// 波次提示文本事件，外部系统可订阅以自定义提示展示方式
        /// </summary>
        public event Action<string> OnWaveTipRequested;

        private bool initialized;
        private bool enabled;
        private float restStartTime;
        private float restDuration;
        private readonly WaveRuleService ruleService = new WaveRuleService();
        private IGameTime gameTime;
        private IGameLogger gameLogger;

        private IGameTime GameTime
        {
            get
            {
                if (this.gameTime == null)
                {
                    this.gameTime = Core.ServiceLocator.Get<IGameTime>();
                }

                return this.gameTime;
            }
        }

        private IGameLogger GameLogger
        {
            get
            {
                if (this.gameLogger == null)
                {
                    this.gameLogger = Core.ServiceLocator.Get<IGameLogger>();
                }

                return this.gameLogger;
            }
        }

        /// <summary>
        /// 构造函数：初始化默认状态
        /// </summary>
        public WaveEventFeedback()
        {
            this.CurrentState = WaveFeedbackState.CreateDefault();
        }

        /// <summary>
        /// 延迟初始化：首次调用时订阅 WaveManager 的全部事件。
        /// 避免在构造函数中订阅，防止与 WaveManager 的构造顺序产生竞态。
        /// </summary>
        private void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;
            this.Enable();
        }

        /// <summary>
        /// 启用波次事件反馈：订阅 WaveManager 的全部5个事件。
        /// 如果 WaveManager 实例尚不存在，则静默跳过（等待下次 Enable 调用）。
        /// 如果已启用，则先取消旧订阅再重新订阅，避免重复订阅。
        /// </summary>
        public void Enable()
        {
            try
            {
                EventBus eventBus = Core.ServiceLocator.Get<EventBus>();

                // 先取消旧订阅，防止重复订阅
                this.DisableInternal(eventBus);

                eventBus.Subscribe<WaveStartedEvent>(this.HandleWaveStarted);
                eventBus.Subscribe<WaveEndedEvent>(this.HandleWaveEnded);
                eventBus.Subscribe<AllWavesClearedEvent>(this.HandleAllWavesCleared);
                eventBus.Subscribe<WaveRestStartedEvent>(this.HandleRestStartedEvent);

                // OnWaveStateChanged 暂无 EventBus 事件类型，通过 WaveManager C# 事件保持兼容
                WaveManager wm = Core.ServiceLocator.Get<WaveManager>();
                if (wm != null)
                {
                    wm.OnWaveStateChanged -= this.HandleWaveStateChanged;
                    wm.OnWaveStateChanged += this.HandleWaveStateChanged;
                }

                // 通过接口同步状态（不直接依赖 WaveManager 属性）
                if (Core.ServiceLocator.TryGet(out IWaveStateProvider wsp))
                {
                    this.SyncCurrentState(wsp);
                }

                this.enabled = true;
                this.initialized = true;
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    $"WaveEventFeedback.Enable failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        /// <summary>
        /// 禁用波次事件反馈：取消订阅所有波次事件。
        /// 不会销毁已记录的波次状态数据，外部仍可查询 CurrentState。
        /// </summary>
        public void Disable()
        {
            try
            {
                EventBus eventBus = Core.ServiceLocator.Get<EventBus>();
                this.DisableInternal(eventBus);

                WaveManager wm = Core.ServiceLocator.Get<WaveManager>();
                if (wm != null)
                {
                    wm.OnWaveStateChanged -= this.HandleWaveStateChanged;
                }
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    $"WaveEventFeedback.Disable failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }

            this.enabled = false;
            this.StopRestCountdown();
        }

        /// <summary>
        /// 取消订阅所有波次事件（内部方法，不检查 enabled 标志）
        /// </summary>
        private void DisableInternal(EventBus eventBus)
        {
            if (eventBus == null)
            {
                return;
            }

            eventBus.Unsubscribe<WaveStartedEvent>(this.HandleWaveStarted);
            eventBus.Unsubscribe<WaveEndedEvent>(this.HandleWaveEnded);
            eventBus.Unsubscribe<AllWavesClearedEvent>(this.HandleAllWavesCleared);
            eventBus.Unsubscribe<WaveRestStartedEvent>(this.HandleRestStartedEvent);
        }

        #region 事件处理器

        /// <summary>
        /// 波次开始回调：显示波次来袭提示
        /// </summary>
        /// <param name="e">波次开始事件。</param>
        private void HandleWaveStarted(WaveStartedEvent e)
        {
            if (e == null) return;
            string message = $"第 {e.WaveIndex} 波来袭! 准备迎战!";
            this.ShowTip(message);
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 波次结束回调（EventBus 订阅）：显示波次清除提示。
        /// </summary>
        /// <param name="e">波次结束事件。</param>
        private void HandleWaveEnded(WaveEndedEvent e)
        {
            if (e == null) return;
            string message = $"第 {e.WaveIndex} 波已清除! (共完成 {e.TotalWavesCompleted} 波)";
            this.ShowTip(message);
            this.StopRestCountdown();
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 全部波次完成回调（EventBus 订阅）：显示通关提示。
        /// </summary>
        /// <param name="e">全部波次完成事件。</param>
        private void HandleAllWavesCleared(AllWavesClearedEvent e)
        {
            if (e == null) return;
            string message = $"全部 {e.TotalWavesCompleted} 波已清除! 你已征服所有波次!";
            this.ShowTip(message);
            this.StopRestCountdown();
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 波间休息开始回调（EventBus 订阅）：显示休息倒计时提示。
        /// </summary>
        /// <param name="e">波间休息事件。</param>
        private void HandleRestStartedEvent(WaveRestStartedEvent e)
        {
            if (e == null) return;
            this.restStartTime = this.GameTime.Time;
            this.restDuration = e.RestDuration;

            string message = $"休息中... {e.RestDuration:F0} 秒后下一波开始";
            this.ShowTip(message);
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 波次状态变更回调（每帧可能触发）：更新内部状态快照
        /// </summary>
        private void HandleWaveStateChanged()
        {
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 从 WaveManager 同步当前状态到内部 WaveFeedbackState
        /// </summary>
        private void SyncCurrentState()
        {
            try
            {
                if (Core.ServiceLocator.TryGet(out IWaveStateProvider wsp))
                {
                    this.SyncCurrentState(wsp);
                }
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    $"WaveEventFeedback.SyncCurrentState failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        /// <summary>
        /// 从 IWaveStateProvider 同步波次状态（接口解耦，不直接依赖 WaveManager）。
        /// </summary>
        private void SyncCurrentState(IWaveStateProvider wsp)
        {
            if (wsp == null)
            {
                return;
            }

            float remainingRest = 0f;
            if (wsp.IsResting)
            {
                float elapsed = this.GameTime.Time - this.restStartTime;
                remainingRest = this.ruleService.GetRemainingRestTime(this.restDuration, elapsed);
            }

            this.CurrentState = new WaveFeedbackState
            {
                currentWaveIndex = wsp.CurrentWaveIndex,
                totalWavesCompleted = wsp.TotalWavesCompleted,
                enemiesAliveInWave = wsp.EnemiesAliveInWave,
                enemiesDefeatedInWave = wsp.EnemiesDefeatedInWave,
                isWaveActive = wsp.IsWaveActive,
                isResting = wsp.IsResting,
                difficultyScale = wsp.CurrentDifficultyScale,
                restDuration = this.restDuration,
                restRemaining = remainingRest,
                feedbackEnabled = this.enabled,
            };
        }

        /// <summary>
        /// 通知外部订阅者状态已更新
        /// </summary>
        private void NotifyStateChanged()
        {
            this.OnWaveFeedbackChanged?.Invoke(this.CurrentState);
        }

        /// <summary>
        /// 显示提示文本：优先通过 GlobalInit.ShowTip 显示游戏内 Tip UI，
        /// 不可用时降级为 Debug.Log 输出。
        /// 同时触发 OnWaveTipRequested 事件，允许外部系统自定义提示展示。
        /// </summary>
        /// <param name="message">提示文本</param>
        private void ShowTip(string message)
        {
            // 触发外部事件，允许 HUD 或其他系统先处理
            this.OnWaveTipRequested?.Invoke(message);

            try
            {
                AWorkerTask.ShowTipProvider(message);
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    $"WaveEventFeedback.ShowTip failed: {message}\n{e}",
                    LogManager.LogLevelEnum.Error);

                // 降级路径：GlobalInit 或 Tip Prefab 不可用时使用日志输出
            }

            this.GameLogger.Log($"[WaveEvent] {message}");
        }

        /// <summary>
        /// 停止休息倒计时
        /// </summary>
        private void StopRestCountdown()
        {
            this.restDuration = 0f;
            this.restStartTime = 0f;
        }

        #endregion
    }

    /// <summary>
    /// 波次反馈状态数据结构。
    /// 由 WaveEventFeedback 维护，供 HUD 或外部系统只读查询。
    /// 每次波次状态变化时更新，通过 OnWaveFeedbackChanged 事件推送。
    /// </summary>
    [Serializable]
    public class WaveFeedbackState
    {
        /// <summary>当前波次索引（从1开始，0表示未开始）</summary>
        public int currentWaveIndex;

        /// <summary>已完成的波次总数</summary>
        public int totalWavesCompleted;

        /// <summary>当前波次中存活的敌人数量</summary>
        public int enemiesAliveInWave;

        /// <summary>当前波次中已击杀的敌人数量</summary>
        public int enemiesDefeatedInWave;

        /// <summary>是否正在波次战斗中</summary>
        public bool isWaveActive;

        /// <summary>是否在波间休息中</summary>
        public bool isResting;

        /// <summary>当前难度缩放因子</summary>
        public float difficultyScale;

        /// <summary>波间休息总时长（秒）</summary>
        public float restDuration;

        /// <summary>波间休息剩余时间（秒）</summary>
        public float restRemaining;

        /// <summary>波次反馈是否已启用</summary>
        public bool feedbackEnabled;

        /// <summary>
        /// 创建默认状态（未开始波次）
        /// </summary>
        public static WaveFeedbackState CreateDefault()
        {
            return new WaveFeedbackState
            {
                currentWaveIndex = 0,
                totalWavesCompleted = 0,
                enemiesAliveInWave = 0,
                enemiesDefeatedInWave = 0,
                isWaveActive = false,
                isResting = false,
                difficultyScale = 1.0f,
                restDuration = 0f,
                restRemaining = 0f,
                feedbackEnabled = false,
            };
        }

        /// <summary>
        /// 生成可读的状态摘要文本，供调试和 Editor 菜单使用
        /// </summary>
        public string ToSummaryText()
        {
            string waveInfo;
            if (this.currentWaveIndex <= 0)
            {
                waveInfo = "波次尚未开始";
            }
            else if (this.isResting)
            {
                waveInfo = $"波间休息中... 剩余 {this.restRemaining:F0} 秒";
            }
            else if (this.isWaveActive)
            {
                waveInfo = $"第 {this.currentWaveIndex} 波进行中";
            }
            else
            {
                waveInfo = $"已完成 {this.totalWavesCompleted} 波";
            }

            return $"波次状态: {waveInfo}\n" +
                   $"存活敌人: {this.enemiesAliveInWave}\n" +
                   $"已击杀: {this.enemiesDefeatedInWave}\n" +
                   $"难度倍率: {this.difficultyScale:F2}x\n" +
                   $"反馈状态: {(this.feedbackEnabled ? "已启用" : "未启用")}";
        }
    }
}
