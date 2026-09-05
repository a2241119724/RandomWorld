namespace LAB2D
{
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Time;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// 昼夜光照管理器 — 驱动场景全局光（Global 类型 Light2D，场景物体 DayNightGlobalLight）的
    /// 强度与色温随游戏时间循环：午夜暗蓝 0.35 → 正午纯白 1.0（与无光照时代的画面亮度一致）→ 黄昏橙红 → 入夜偏蓝。
    /// 光由场景提供（带 GlobalLight tag），运行时只查找不自建；光照公式收口在 Domain 层 DayNightRuleService，
    /// 本类只负责查找/创建与节流写入（变化超阈值才写，Light2D setter 会置脏光照纹理）。
    /// 排在 GameTimeManager 之后 Tick，采样当帧新时间。
    /// </summary>
    public class DayNightLightManager : Singleton<DayNightLightManager>, ITickable
    {
        private const float WriteThreshold = 0.005f; // 强度/色温通道变化阈值，低于则跳过写入
        private const float FindRetryInterval = 2f; // 全局光缺失时的查找重试间隔（秒）

        private Light2D globalLight;
        private float nextFindRetryTime;
        private float lastIntensity = -1f;
        private Color lastColor = new Color(-1f, -1f, -1f, 1f);

        /// <summary>当前驱动的全局光（null=尚未找到/创建；场景切换销毁后自动重建）。</summary>
        public Light2D GlobalLight => this.globalLight;

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            if (this.globalLight == null)
            {
                if (Time.time < this.nextFindRetryTime)
                {
                    return;
                }

                this.nextFindRetryTime = Time.time + FindRetryInterval;
                this.globalLight = this.FindGlobalLight();
                if (this.globalLight == null)
                {
                    return;
                }
            }

            double curGameTime = GameTimeManager.Instance.CurGameTime;
            float intensity = DayNightRuleService.GetGlobalLightIntensity(curGameTime, GlobalData.GameDayTime);
            bool isBloodMoon = false;
            try
            {
                isBloodMoon = Core.ServiceLocator.Get<Manager.WeatherManager>().CurrentWeather
                    == Manager.WeatherManager.WeatherTypeEnum.BloodMoon;
            }
            catch (System.Exception)
            {
                // 天气服务不可用（初始化早期）按非血月
            }

            DayLightColor c = DayNightRuleService.GetGlobalLightColor(curGameTime, GlobalData.GameDayTime, isBloodMoon);

            if (Mathf.Abs(intensity - this.lastIntensity) >= WriteThreshold)
            {
                this.globalLight.intensity = intensity;
                this.lastIntensity = intensity;
            }

            if (Mathf.Abs(c.R - this.lastColor.r) >= WriteThreshold
                || Mathf.Abs(c.G - this.lastColor.g) >= WriteThreshold
                || Mathf.Abs(c.B - this.lastColor.b) >= WriteThreshold)
            {
                this.lastColor = new Color(c.R, c.G, c.B, 1f);
                this.globalLight.color = this.lastColor;
            }
        }

        /// <summary>
        /// 查找场景全局光（Global 类型 Light2D，带 GlobalLight tag；场景物体由编辑器维护）。
        /// 缺失时返回 null，Tick 按 FindRetryInterval 重试。
        /// </summary>
        private Light2D FindGlobalLight()
        {
            GameObject go = GameObject.FindGameObjectWithTag(TagConstant.GLOBAL_LIGHT_TAG);
            if (go == null)
            {
                return null;
            }

            Light2D found = go.GetComponent<Light2D>();
            if (found != null)
            {
                AWorkerTask.LogProvider("[LightDiag] 复用场景全局光，昼夜光照驱动生效", LogManager.LogLevelEnum.Debug);
            }

            return found;
        }
    }
}
