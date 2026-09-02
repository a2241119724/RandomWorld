namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using LAB2D.Domain.Time;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏时间 HUD — 只读消费 GameTimeManager（时间推进/跨天天气/相位事件
    /// 已收口到 Manager 的 Tick，本类不再写 CurGameTime）。
    /// </summary>
    public class GameTimeUI : MonoBehaviour
    {
        private static readonly int DayTime = 86400;
        private static readonly int HourTime = 3600;
        private readonly double rate = DayTime * 1.0 / GlobalData.GameDayTime;
        private Text gameTime;
        private Light2D globalLight; // 白天黑天显示
        private Transform pointer; // 指针
        private string lastTimeText; // 上次写入的时间文本：分钟级才变化，避免每帧 string.Format 后重复赋值 UI（Text 赋值会触发重建顶点）

        private GameTimeManager gameTimeManager;

        public void Awake()
        {
            this.gameTime = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Text");
            this.globalLight = GameObject.FindGameObjectWithTag(TagConstant.GLOBAL_LIGHT_TAG).GetComponent<Light2D>();
            this.pointer = LAB2D.Tool.Tool.GetComponentInChildren<Image>(this.gameObject, "Pointer").transform;
            this.gameTimeManager = GameTimeManager.Instance;
        }

        public void Update()
        {
            double curGameTime = this.gameTimeManager.CurGameTime;
            double time = curGameTime * this.rate;

            // 光照强度：Domain 纯函数（与旧版 sin 曲线数值一致）
            this.globalLight.intensity = DayNightRuleService.GetLightIntensity(curGameTime, GlobalData.GameDayTime);
            string timeText = string.Format(
                "<color=" + PixelUITheme.RichPink + ">游戏时间: </color>{0:D2}天{1:D2}时{2:D2}分",
                (int)time / DayTime,
                ((int)time % DayTime) / HourTime,
                ((int)time % HourTime) / 60);
            if (timeText != this.lastTimeText) // 文本未变则跳过赋值（每帧热点：HUD 常驻，N 帧里只有分钟翻转的 1 帧真正需要更新）
            {
                this.lastTimeText = timeText;
                this.gameTime.text = timeText;
            }

            // 一度等于2分钟
            this.pointer.localRotation = Quaternion.Euler(0, 0, (float)(-180 - (time / (2 * 60))));
        }
    }
}
