namespace LAB2D.UI.Panel.PanelUI.ForegroundUI
{
    using LAB2D;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 游戏时间 HUD — 只读消费 GameTimeManager（时间推进/跨天天气/相位事件
    /// 已收口到 Manager 的 Tick，本类不再写 CurGameTime）。
    /// 昼夜全局光的强度/色温驱动已迁至 DayNightLightManager。
    /// </summary>
    public class GameTimeUI : MonoBehaviour
    {
        private static readonly int DayTime = 86400;
        private static readonly int HourTime = 3600;
        private readonly double rate = DayTime * 1.0 / GlobalData.GameDayTime;
        private Text gameTime;
        private Transform pointer; // 指针
        private int lastTotalMinutes = -1; // 上次写入的总分钟数：文本分钟级才变化，先比 int 再 Format，
                                           // 避免每帧 string.Format 分配（100 Worker 卡顿治理 2026-09-06）

        private GameTimeManager gameTimeManager;

        public void Awake()
        {
            this.gameTime = LAB2D.Tool.Tool.GetComponentInChildren<Text>(this.gameObject, "Text");
            this.pointer = LAB2D.Tool.Tool.GetComponentInChildren<Image>(this.gameObject, "Pointer")?.transform;
            this.gameTimeManager = GameTimeManager.Instance;
        }

        public void Update()
        {
            if (this.gameTimeManager == null || this.gameTime == null || this.pointer == null)
            {
                return; // Awake 被异常中断或子组件缺失：退化只读，避免每帧 NRE
            }

            double curGameTime = this.gameTimeManager.CurGameTime;
            double time = curGameTime * this.rate;

            int totalMinutes = (int)time / 60;
            if (totalMinutes != this.lastTotalMinutes) // 分钟翻转才 Format+赋值（每帧热点：常驻 HUD，先比 int 零分配）
            {
                this.lastTotalMinutes = totalMinutes;
                this.gameTime.text = string.Format(
                    "<color=" + PixelUITheme.RichPink + ">游戏时间: </color>{0:D2}天{1:D2}时{2:D2}分",
                    (int)time / DayTime,
                    ((int)time % DayTime) / HourTime,
                    ((int)time % HourTime) / 60);
            }

            // 一度等于2分钟
            this.pointer.localRotation = Quaternion.Euler(0, 0, (float)(-180 - (time / (2 * 60))));
        }
    }
}
