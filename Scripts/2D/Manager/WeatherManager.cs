namespace LAB2D.Manager
{
    using LAB2D;
    using LAB2D.Character.Worker.Task;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 天气管理
    /// </summary>
    public class WeatherManager : MonoBehaviour
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
        }

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
        /// 随机天气
        /// </summary>
        public void RandWeather()
        {
            WeatherTypeEnum nextWeather = (WeatherTypeEnum)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(WeatherTypeEnum)).Length);
            this.SetWeather(nextWeather);
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
    }
}
