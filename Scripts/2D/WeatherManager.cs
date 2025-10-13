namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 天气管理
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        private readonly Dictionary<WeatherType, GameObject> weathers = new ();

        /// <summary>
        /// 天气类型
        /// </summary>
        public enum WeatherType
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

        public void Awake()
        {
            Instance = this;
            foreach (WeatherType weatherType in System.Enum.GetValues(typeof(WeatherType)))
            {
                this.weathers.Add(weatherType, this.transform.Find(weatherType.ToString()).gameObject);
            }
        }

        /// <summary>
        /// 随机天气
        /// </summary>
        public void RandWeather()
        {
            foreach (WeatherType weatherType in System.Enum.GetValues(typeof(WeatherType)))
            {
                this.weathers[weatherType].SetActive(false);
            }

            this.weathers[(WeatherType)Random.Range(0, System.Enum.GetValues(typeof(WeatherType)).Length)].SetActive(true);
        }
    }
}
