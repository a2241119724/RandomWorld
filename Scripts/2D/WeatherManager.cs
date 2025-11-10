namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 天气管理
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        private readonly Dictionary<WeatherTypeEnum, GameObject> weathers = new ();

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

        public void Awake()
        {
            Instance = this;
            foreach (WeatherTypeEnum weatherType in System.Enum.GetValues(typeof(WeatherTypeEnum)))
            {
                this.weathers.Add(weatherType, this.transform.Find(weatherType.ToString()).gameObject);
            }
        }

        /// <summary>
        /// 随机天气
        /// </summary>
        public void RandWeather()
        {
            foreach (WeatherTypeEnum weatherType in System.Enum.GetValues(typeof(WeatherTypeEnum)))
            {
                this.weathers[weatherType].SetActive(false);
            }

            this.weathers[(WeatherTypeEnum)Random.Range(0, System.Enum.GetValues(typeof(WeatherTypeEnum)).Length)].SetActive(true);
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
