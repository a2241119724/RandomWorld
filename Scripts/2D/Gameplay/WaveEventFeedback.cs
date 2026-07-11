namespace LAB2D
{
    using System;
    using UnityEngine;

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
            if (!this.initialized)
            {
                this.initialized = true;
            }

            try
            {
                WaveManager wm = WaveManager.Instance;
                if (wm == null)
                {
                    return;
                }

                // 先取消旧订阅，防止重复订阅
                this.DisableInternal(wm);

                wm.OnWaveStart += this.HandleWaveStart;
                wm.OnWaveEnd += this.HandleWaveEnd;
                wm.OnAllWavesCleared += this.HandleAllWavesCleared;
                wm.OnRestStart += this.HandleRestStart;
                wm.OnWaveStateChanged += this.HandleWaveStateChanged;

                this.enabled = true;

                // 立即同步当前波次状态，避免错过已开始的波次
                this.SyncCurrentState(wm);
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    $"WaveEventFeedback.Enable failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        /// <summary>
        /// 禁用波次事件反馈：取消订阅 WaveManager 的全部事件。
        /// 不会销毁已记录的波次状态数据，外部仍可查询 CurrentState。
        /// </summary>
        public void Disable()
        {
            try
            {
                WaveManager wm = WaveManager.Instance;
                if (wm != null)
                {
                    this.DisableInternal(wm);
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    $"WaveEventFeedback.Disable failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }

            this.enabled = false;
            this.StopRestCountdown();
        }

        /// <summary>
        /// 取消订阅 WaveManager 的所有事件（内部方法，不检查 enabled 标志）
        /// </summary>
        private void DisableInternal(WaveManager wm)
        {
            if (wm == null)
            {
                return;
            }

            wm.OnWaveStart -= this.HandleWaveStart;
            wm.OnWaveEnd -= this.HandleWaveEnd;
            wm.OnAllWavesCleared -= this.HandleAllWavesCleared;
            wm.OnRestStart -= this.HandleRestStart;
            wm.OnWaveStateChanged -= this.HandleWaveStateChanged;
        }

        #region 事件处理器

        /// <summary>
        /// 波次开始回调：显示波次来袭提示
        /// </summary>
        /// <param name="waveIndex">当前波次索引（从1开始）</param>
        private void HandleWaveStart(int waveIndex)
        {
            string message = $"第 {waveIndex} 波来袭! 准备迎战!";
            this.ShowTip(message);
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 波次结束回调：显示波次清除提示，包含已完成波次总数
        /// </summary>
        /// <param name="waveIndex">刚完成的波次索引</param>
        /// <param name="totalCompleted">已完成的波次总数</param>
        private void HandleWaveEnd(int waveIndex, int totalCompleted)
        {
            string message = $"第 {waveIndex} 波已清除! (共完成 {totalCompleted} 波)";
            this.ShowTip(message);

            // 波次结束时停止休息倒计时（如果有的话）
            this.StopRestCountdown();
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 全部波次完成回调：显示通关提示
        /// </summary>
        /// <param name="totalWaves">已完成的波次总数</param>
        private void HandleAllWavesCleared(int totalWaves)
        {
            string message = $"全部 {totalWaves} 波已清除! 你已征服所有波次!";
            this.ShowTip(message);

            this.StopRestCountdown();
            this.SyncCurrentState();
            this.NotifyStateChanged();
        }

        /// <summary>
        /// 波间休息开始回调：显示休息倒计时提示，启动休息倒计时协程
        /// </summary>
        /// <param name="duration">休息时长（秒）</param>
        private void HandleRestStart(float duration)
        {
            this.restStartTime = Time.time;
            this.restDuration = duration;

            string message = $"休息中... {duration:F0} 秒后下一波开始";
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
                WaveManager wm = WaveManager.Instance;
                if (wm != null)
                {
                    this.SyncCurrentState(wm);
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    $"WaveEventFeedback.SyncCurrentState failed.\n{e}",
                    LogManager.LogLevelEnum.Error);
            }
        }

        /// <summary>
        /// 从指定 WaveManager 实例同步状态
        /// </summary>
        private void SyncCurrentState(WaveManager wm)
        {
            if (wm == null)
            {
                return;
            }

            float remainingRest = 0f;
            if (wm.IsResting)
            {
                float elapsed = Time.time - this.restStartTime;
                remainingRest = this.ruleService.GetRemainingRestTime(this.restDuration, elapsed);
            }

            this.CurrentState = new WaveFeedbackState
            {
                currentWaveIndex = wm.CurrentWaveIndex,
                totalWavesCompleted = wm.TotalWavesCompleted,
                enemiesAliveInWave = wm.EnemiesAliveInWave,
                enemiesDefeatedInWave = wm.EnemiesDefeatedInWave,
                isWaveActive = wm.IsWaveActive,
                isResting = wm.IsResting,
                difficultyScale = wm.CurrentDifficultyScale,
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
                if (GlobalInit.Instance != null)
                {
                    GlobalInit.Instance.ShowTip(message);
                    return;
                }
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    $"WaveEventFeedback.ShowTip failed: {message}\n{e}",
                    LogManager.LogLevelEnum.Error);

                // 降级路径：GlobalInit 或 Tip Prefab 不可用时使用日志输出
            }

            Debug.Log($"[WaveEvent] {message}");
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
