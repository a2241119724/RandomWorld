namespace LAB2D.Gameplay
{
    using LAB2D.Enum;
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Manager;
    using Character = LAB2D.Character.Character;
    using System;
    using UnityEngine;

    /// <summary>
    /// 天气玩法影响管理器。
    /// 根据 WeatherManager 的当前天气，为移动速度、工人工作进度和环境灵气恢复提供运行时倍率。
    /// 本类不修改存档结构，不写入资源，不参与 Photon 同步。
    /// </summary>
    public class WeatherGameplayEffect : Singleton<WeatherGameplayEffect>, IWeatherGameplayService
    {
        private WeatherManager.WeatherTypeEnum currentWeather = WeatherManager.WeatherTypeEnum.Sunny;
        private WeatherGameplayState currentState;
        private bool initialized;
        private bool enabled = true;
        private bool tipEnabled = true;

        /// <summary>
        /// 天气效果状态变化事件。
        /// HUD 或其他展示层可订阅此事件刷新显示。
        /// </summary>
        public event Action<WeatherGameplayState> OnWeatherEffectChanged;

        /// <summary>
        /// 天气提示请求事件。
        /// 外部可订阅此事件接管提示展示方式。
        /// </summary>
        public event Action<string> OnWeatherTipRequested;

        public WeatherGameplayEffect()
        {
            this.currentState = WeatherGameplayState.FromWeather(this.currentWeather, true);
        }

        /// <summary>
        /// 当前天气玩法状态快照。
        /// </summary>
        public WeatherGameplayState CurrentState
        {
            get
            {
                this.EnsureInitialized();
                return this.currentState;
            }
        }

        /// <summary>
        /// 玩家移动速度倍率。
        /// </summary>
        public float PlayerMoveSpeedMultiplier
        {
            get
            {
                this.EnsureInitialized();
                return this.enabled ? this.currentState.PlayerMoveSpeedMultiplier : 1.0f;
            }
        }

        /// <summary>
        /// 工人移动速度倍率。
        /// </summary>
        public float WorkerMoveSpeedMultiplier
        {
            get
            {
                this.EnsureInitialized();
                return this.enabled ? this.currentState.WorkerMoveSpeedMultiplier : 1.0f;
            }
        }

        /// <summary>
        /// 工人任务进度倍率。
        /// </summary>
        public float WorkerTaskProgressMultiplier
        {
            get
            {
                this.EnsureInitialized();
                return this.enabled ? this.currentState.WorkerTaskProgressMultiplier : 1.0f;
            }
        }

        /// <summary>
        /// 环境灵气恢复倍率。
        /// </summary>
        public float EnergyRecoveryMultiplier
        {
            get
            {
                this.EnsureInitialized();
                return this.enabled ? this.currentState.EnergyRecoveryMultiplier : 1.0f;
            }
        }

        /// <summary>
        /// 启用天气玩法影响。
        /// </summary>
        public void Enable()
        {
            this.enabled = true;
            this.Refresh();
        }

        /// <summary>
        /// 禁用天气玩法影响，所有倍率回到 1。
        /// </summary>
        public void Disable()
        {
            this.enabled = false;
            this.UpdateState(this.currentWeather, false);
        }

        /// <summary>
        /// 设置是否显示天气切换提示。
        /// </summary>
        /// <param name="enabledTip">是否显示 Tip 提示。</param>
        public void SetTipEnabled(bool enabledTip)
        {
            this.tipEnabled = enabledTip;
        }

        /// <summary>
        /// 重新同步 WeatherManager 当前天气。
        /// </summary>
        public void Refresh()
        {
            this.EnsureInitialized();
            this.SubscribeWeatherManager();
            this.SyncFromWeatherManager();
            this.UpdateState(this.currentWeather, false);
        }

        /// <summary>
        /// 获取指定角色在当前天气下的移动速度倍率。
        /// 玩家和工人会受到天气影响，其他角色默认不受影响。
        /// </summary>
        /// <param name="character">目标角色。</param>
        /// <returns>移动速度倍率。</returns>
        public float GetCharacterMoveSpeedMultiplier(Character character)
        {
            this.EnsureInitialized();
            if (!this.enabled || character == null)
            {
                return 1.0f;
            }

            if (character is Player)
            {
                return this.PlayerMoveSpeedMultiplier;
            }

            if (character is AWorker)
            {
                return this.WorkerMoveSpeedMultiplier;
            }

            return 1.0f;
        }

        /// <summary>
        /// 获取指定角色在当前天气下的移动速度。
        /// </summary>
        /// <param name="character">目标角色。</param>
        /// <param name="baseSpeed">角色基础移动速度。</param>
        /// <returns>套用天气倍率后的移动速度。</returns>
        public float GetAdjustedCharacterMoveSpeed(Character character, float baseSpeed)
        {
            return WeatherGameplayTool.ApplyMultiplier(
                baseSpeed,
                this.GetCharacterMoveSpeedMultiplier(character),
                0.0f);
        }

        /// <summary>
        /// 获取指定 Worker 任务在当前天气下的进度倍率。
        /// 吃饭和睡觉属于恢复行为，不受恶劣天气减速影响。
        /// </summary>
        /// <param name="taskType">Worker 任务类型。</param>
        /// <returns>任务进度倍率。</returns>
        public float GetWorkerTaskProgressMultiplier(WorkerTaskType taskType)
        {
            this.EnsureInitialized();
            if (!this.enabled ||
                taskType == WorkerTaskType.Eat ||
                taskType == WorkerTaskType.Sleep)
            {
                return 1.0f;
            }

            return this.WorkerTaskProgressMultiplier;
        }

        /// <summary>
        /// 延迟初始化并订阅天气变化事件。
        /// </summary>
        private void EnsureInitialized()
        {
            if (this.initialized)
            {
                return;
            }

            this.initialized = true;
            this.SyncFromWeatherManager();
            this.SubscribeWeatherManager();
            this.UpdateState(this.currentWeather, false);
        }

        /// <summary>
        /// 订阅 WeatherManager 事件。
        /// </summary>
        private void SubscribeWeatherManager()
        {
            try
            {
                WeatherManager manager = WeatherManager.Instance;
                if (manager == null)
                {
                    return;
                }

                manager.WeatherChanged -= this.HandleWeatherChanged;
                manager.WeatherChanged += this.HandleWeatherChanged;
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    "订阅天气变化失败: " + exception.Message,
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 从 WeatherManager 同步当前天气。
        /// </summary>
        private void SyncFromWeatherManager()
        {
            try
            {
                WeatherManager manager = WeatherManager.Instance;
                if (manager != null)
                {
                    this.currentWeather = manager.CurrentWeather;
                }
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    "同步天气状态失败: " + exception.Message,
                    LogManager.LogLevelEnum.Warning);
            }
        }

        /// <summary>
        /// 天气切换回调。
        /// </summary>
        /// <param name="weather">新的天气类型。</param>
        private void HandleWeatherChanged(WeatherManager.WeatherTypeEnum weather)
        {
            this.UpdateState(weather, true);
        }

        /// <summary>
        /// 更新当前状态并通知外部。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <param name="showTip">是否显示提示。</param>
        private void UpdateState(WeatherManager.WeatherTypeEnum weather, bool showTip)
        {
            this.currentWeather = weather;
            this.currentState = WeatherGameplayState.FromWeather(weather, this.enabled);
            this.OnWeatherEffectChanged?.Invoke(this.currentState);

            if (showTip && this.tipEnabled)
            {
                this.ShowWeatherTip(this.currentState.ToTipText());
            }
        }

        /// <summary>
        /// 显示天气玩法提示。
        /// 优先使用现有 Tip UI，不可用时降级为日志。
        /// </summary>
        /// <param name="message">提示文本。</param>
        private void ShowWeatherTip(string message)
        {
            this.OnWeatherTipRequested?.Invoke(message);

            try
            {
                if (GlobalInit.Instance != null)
                {
                    GlobalInit.Instance.ShowTip(message);
                    return;
                }
            }
            catch (Exception exception)
            {
                LogManager.Instance.Log(
                    "显示天气提示失败: " + exception.Message,
                    LogManager.LogLevelEnum.Warning);
            }

            Debug.Log("[天气玩法] " + message);
        }
    }

    /// <summary>
    /// 天气玩法状态快照。
    /// 由 WeatherGameplayEffect 维护，供 HUD、Editor 菜单和其他业务只读查询。
    /// </summary>
    [Serializable]
    public class WeatherGameplayState
    {
        /// <summary>当前天气类型。</summary>
        public WeatherManager.WeatherTypeEnum Weather;

        /// <summary>当前天气中文名称。</summary>
        public string WeatherName;

        /// <summary>天气玩法影响是否启用。</summary>
        public bool EffectEnabled;

        /// <summary>玩家移动速度倍率。</summary>
        public float PlayerMoveSpeedMultiplier;

        /// <summary>工人移动速度倍率。</summary>
        public float WorkerMoveSpeedMultiplier;

        /// <summary>工人任务进度倍率。</summary>
        public float WorkerTaskProgressMultiplier;

        /// <summary>环境灵气恢复倍率。</summary>
        public float EnergyRecoveryMultiplier;

        /// <summary>
        /// 根据天气创建状态快照。
        /// </summary>
        /// <param name="weather">当前天气。</param>
        /// <param name="effectEnabled">天气玩法影响是否启用。</param>
        /// <returns>状态快照。</returns>
        public static WeatherGameplayState FromWeather(WeatherManager.WeatherTypeEnum weather, bool effectEnabled)
        {
            return new WeatherGameplayState
            {
                Weather = weather,
                WeatherName = WeatherGameplayTool.GetWeatherName(weather),
                EffectEnabled = effectEnabled,
                PlayerMoveSpeedMultiplier = effectEnabled ? WeatherGameplayTool.GetPlayerMoveSpeedMultiplier(weather) : 1.0f,
                WorkerMoveSpeedMultiplier = effectEnabled ? WeatherGameplayTool.GetWorkerMoveSpeedMultiplier(weather) : 1.0f,
                WorkerTaskProgressMultiplier = effectEnabled ? WeatherGameplayTool.GetWorkerTaskProgressMultiplier(weather) : 1.0f,
                EnergyRecoveryMultiplier = effectEnabled ? WeatherGameplayTool.GetEnergyRecoveryMultiplier(weather) : 1.0f,
            };
        }

        /// <summary>
        /// 生成 Tip 文案。
        /// </summary>
        /// <returns>适合游戏内提示条展示的文案。</returns>
        public string ToTipText()
        {
            return $"{this.WeatherName}: 玩家移动 {this.PlayerMoveSpeedMultiplier:0.00}x，" +
                $"工人工作 {this.WorkerTaskProgressMultiplier:0.00}x，" +
                $"灵气恢复 {this.EnergyRecoveryMultiplier:0.00}x";
        }

        /// <summary>
        /// 生成完整摘要文本。
        /// </summary>
        /// <returns>适合 HUD 和 Editor 菜单展示的多行文本。</returns>
        public string ToSummaryText()
        {
            return $"天气: {this.WeatherName}\n" +
                $"效果状态: {(this.EffectEnabled ? "已启用" : "已禁用")}\n" +
                $"玩家移动: {this.PlayerMoveSpeedMultiplier:0.00}x\n" +
                $"工人移动: {this.WorkerMoveSpeedMultiplier:0.00}x\n" +
                $"工人工作: {this.WorkerTaskProgressMultiplier:0.00}x\n" +
                $"灵气恢复: {this.EnergyRecoveryMultiplier:0.00}x";
        }
    }
}
