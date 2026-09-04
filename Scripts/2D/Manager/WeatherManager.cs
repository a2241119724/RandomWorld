namespace LAB2D.Manager
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Data;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Gameplay;
    using LAB2D.Tool;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 天气管理（AMonoSaveData）。
    /// </summary>
    public class WeatherManager : AMonoSaveData
    {
        private readonly Dictionary<WeatherTypeEnum, GameObject> weathers = new ();
        private WeatherTypeEnum currentWeather = WeatherTypeEnum.Sunny;

        /// <summary>
        /// 天气类型
        /// </summary>
        public enum WeatherTypeEnum
        {
            /// <summary>
            /// 晴天
            /// </summary>
            Sunny,

            /// <summary>
            /// 雨天
            /// </summary>
            Rain,

            /// <summary>
            /// 雪天
            /// </summary>
            Snow,

            /// <summary>
            /// 灵雨（事件天气）：无场景视觉节点，数值效果走玩法乘数
            /// </summary>
            SpiritRain,

            /// <summary>
            /// 血月（事件天气）：无场景视觉节点，夜晚光色 tint + 波次强化
            /// </summary>
            BloodMoon,
        }

        /// <summary>
        /// 无场景视觉节点的天气（事件天气视觉后补，节点查找静默跳过不报缺失）。
        /// </summary>
        private static readonly HashSet<WeatherTypeEnum> NoVisualWeathers = new HashSet<WeatherTypeEnum>
        {
            WeatherTypeEnum.SpiritRain,
            WeatherTypeEnum.BloodMoon,
        };

        public static WeatherManager Instance { get; private set; }

        /// <summary>
        /// 当前天气。
        /// </summary>
        public WeatherTypeEnum CurrentWeather
        {
            get { return this.currentWeather; }
        }

        /// <summary>
        /// 天气变化事件。
        /// 参数为新的天气类型，供玩法、UI 和调试工具订阅。
        /// </summary>
        public event Action<WeatherTypeEnum> WeatherChanged;

        public void Awake()
        {
            Instance = this;
            foreach (WeatherTypeEnum weatherType in System.Enum.GetValues(typeof(WeatherTypeEnum)))
            {
                if (NoVisualWeathers.Contains(weatherType))
                {
                    continue;
                }

                Transform weatherTransform = this.transform.Find(weatherType.ToString());
                if (weatherTransform == null)
                {
                    AWorkerTask.LogProvider("天气节点缺失: " + weatherType, LogManager.LogLevelEnum.Warning);
                    continue;
                }

                this.weathers[weatherType] = weatherTransform.gameObject;
                if (weatherTransform.gameObject.activeSelf)
                {
                    this.currentWeather = weatherType;
                }
            }

            this.SetWeather(this.currentWeather, false);
        }

        /// <summary>
        /// 随机天气（加权：常规高权重，事件天气灵雨/血月稀有——规则见 WeatherGameplayRuleService.RollWeather）
        /// </summary>
        public void RandWeather()
        {
            WeatherType domainWeather = WeatherGameplayRuleService.RollWeather(UnityEngine.Random.Range(0f, 100f));
            this.SetWeather(WeatherGameplayTool.MapFromDomain(domainWeather));
        }

        /// <summary>
        /// 设置天气。
        /// </summary>
        /// <param name="weatherType">目标天气。</param>
        public void SetWeather(WeatherTypeEnum weatherType)
        {
            this.SetWeather(weatherType, true);
        }

        /// <summary>
        /// 设置天气。
        /// </summary>
        /// <param name="weatherType">目标天气。</param>
        /// <param name="notify">是否通知订阅者。</param>
        private void SetWeather(WeatherTypeEnum weatherType, bool notify)
        {
            foreach (KeyValuePair<WeatherTypeEnum, GameObject> pair in this.weathers)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(pair.Key == weatherType);
                }
            }

            this.currentWeather = weatherType;
            if (notify)
            {
                this.WeatherChanged?.Invoke(weatherType);
            }
        }

        /// <summary>
        /// 缩放天气
        /// </summary>
        /// <param name="rate">比例</param>
        public void Scale(float rate)
        {
            for (int i = 0; i < this.transform.childCount; i++)
            {
                this.transform.GetChild(i).localScale = new Vector3(rate, rate, 1);
            }
        }

        /// <inheritdoc/>
        public override void SaveData()
        {
            base.SaveData();
            WeatherManagerData data = new WeatherManagerData
            {
                CurrentWeather = (int)this.currentWeather,
            };
            DataTool.SaveDataByBinary(GlobalData.ConfigFile.GetPath(this.GetType().Name), data);
        }

        /// <inheritdoc/>
        public override void LoadData()
        {
            base.LoadData();
            WeatherManagerData data = DataTool.LoadDataByBinary<WeatherManagerData>(GlobalData.ConfigFile.GetPath(this.GetType().Name));
            if (data == null)
            {
                return;
            }

            WeatherTypeEnum loadedWeather = (WeatherTypeEnum)data.CurrentWeather;
            if (this.weathers.ContainsKey(loadedWeather))
            {
                this.SetWeather(loadedWeather, false);
            }
        }

        [Serializable]
        public class WeatherManagerData
        {
            public int CurrentWeather;
        }
    }
}
