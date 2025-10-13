namespace LAB2D
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.UI;

    public class GameTimeUI : MonoBehaviour
    {
        private static readonly int DayTime = 86400;
        private static readonly int HourTime = 3600;
        private readonly double rate = DayTime * 1.0 / GlobalData.GameDayTime;
        private Text gameTime;
        private Light2D globalLight; // 白天黑天显示
        private Transform pointer; // 指针
        private double curGameTime;

        public void Awake()
        {
            this.gameTime = Tool.GetComponentInChildren<Text>(this.gameObject, "Text");
            this.globalLight = GameObject.FindGameObjectWithTag(TagConstant.GLOBAL_LIGHT_TAG).GetComponent<Light2D>();
            this.pointer = Tool.GetComponentInChildren<Image>(this.gameObject, "Pointer").transform;
        }

        public void Update()
        {
            // 根据真实游戏时间换算成30分钟一天对应的时间
            int last = (int)(this.curGameTime / GlobalData.GameDayTime);
            this.curGameTime += Time.deltaTime;
            if (last != (int)(this.curGameTime / GlobalData.GameDayTime))
            {
                // 每天开始随机天气
                WeatherManager.Instance.RandWeather();
            }

            double time = this.curGameTime * this.rate;

            // 将sin函数转为周期为1的函数
            this.globalLight.intensity = Mathf.Clamp(Mathf.Sin(((float)this.curGameTime / GlobalData.GameDayTime * 6.2624f) - 1.55f) + 0.7f, 0.2f, 0.9f);
            this.gameTime.text = string.Format(
                "<color=blue>游戏时间: </color>{0:D2}天{1:D2}时{2:D2}分",
                (int)time / DayTime,
                ((int)time % DayTime) / HourTime,
                ((int)time % HourTime) / 60);

            // 一度等于2分钟
            this.pointer.localRotation = Quaternion.Euler(0, 0, (float)(-180 - (time / (2 * 60))));
        }
    }
}
