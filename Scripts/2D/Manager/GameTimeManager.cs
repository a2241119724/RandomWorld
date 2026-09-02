namespace LAB2D
{
    using LAB2D.Core;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Time;
    using System;

    /// <summary>
    /// 游戏时间管理器 — 自推进的持久化游戏内时间（天/时/分）与昼夜相位。
    /// 时间推进/跨天天气/相位事件由 Tick 统一驱动（IInitializable + ITickable，
    /// GlobalInit 有序列表首位的时钟源）；GameTimeUI 等表现层只读消费。
    /// 相位判定与光照公式收口在 Domain 层 DayNightRuleService。
    /// </summary>
    public class GameTimeManager : ASingletonSaveData<GameTimeManager>, IInitializable, ITickable
    {
        /// <summary>
        /// 累计真实游戏时间（秒）。按 GlobalData.GameDayTime 换算为游戏内天数。
        /// </summary>
        public double CurGameTime { get; set; }

        /// <summary>当前昼夜相位（Tick 中检测变化并发布 <see cref="GamePhaseChangedEvent"/>）。</summary>
        public GamePhase CurrentPhase { get; private set; }

        /// <summary>当前天索引（从 0 开始，跨天时发布 <see cref="GameDayChangedEvent"/>）。</summary>
        public int CurrentDayIndex { get; private set; }

        /// <summary>
        /// 跨天回调 — 默认滚动每日随机天气；抽 provider 便于测试替换。
        /// try-catch 静默降级（初始化早期/测试环境 WeatherManager 未注册），仿 TipProvider 先例。
        /// </summary>
        internal static Action DayRolloverAction { get; set; }
            = () =>
            {
                try
                {
                    ServiceLocator.Get<WeatherManager>().RandWeather();
                }
                catch (Exception)
                {
                    // 天气服务不可用时静默跳过
                }
            };

        /// <summary>
        /// 时间推进提供者 — 默认 += deltaTime；测试可注入固定步进。
        /// </summary>
        internal static Func<double, float, double> AdvanceAction { get; set; }
            = (cur, delta) => cur + delta;

        private bool isInitialized;

        /// <inheritdoc/>
        public void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            this.isInitialized = true;
            this.CurrentDayIndex = DayNightRuleService.DayIndex(this.CurGameTime, GlobalData.GameDayTime);
            this.CurrentPhase = DayNightRuleService.GetPhase(this.CurGameTime, GlobalData.GameDayTime);
            AWorkerTask.LogProvider(
                $"[TimeDiag] 时间服务初始化：第{this.CurrentDayIndex}天 相位={this.CurrentPhase}",
                LogManager.LogLevelEnum.Debug);
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            this.CurGameTime = AdvanceAction(this.CurGameTime, deltaTime);

            int dayIndex = DayNightRuleService.DayIndex(this.CurGameTime, GlobalData.GameDayTime);
            if (dayIndex != this.CurrentDayIndex)
            {
                this.CurrentDayIndex = dayIndex;
                DayRolloverAction();
                EventBus.Instance.Publish(new GameDayChangedEvent { DayIndex = dayIndex });
                AWorkerTask.LogProvider($"[TimeDiag] 跨天 -> 第{dayIndex}天", LogManager.LogLevelEnum.Debug);
            }

            GamePhase phase = DayNightRuleService.GetPhase(this.CurGameTime, GlobalData.GameDayTime);
            if (phase != this.CurrentPhase)
            {
                GamePhase old = this.CurrentPhase;
                this.CurrentPhase = phase;
                EventBus.Instance.Publish(new GamePhaseChangedEvent
                {
                    OldPhase = old,
                    NewPhase = phase,
                    DayIndex = dayIndex,
                });
                AWorkerTask.LogProvider(
                    $"[TimeDiag] 相位 {old} -> {phase}（第{dayIndex}天）",
                    LogManager.LogLevelEnum.Debug);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            GameTimeData data = new GameTimeData { CurGameTime = this.CurGameTime };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            GameTimeData data = DataTool.LoadDataByBinary<GameTimeData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            this.CurGameTime = data?.CurGameTime ?? 0.0;
        }

        [Serializable]
        private class GameTimeData
        {
            public double CurGameTime;
        }
    }
}
