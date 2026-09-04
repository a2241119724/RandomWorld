namespace LAB2D
{
    using LAB2D.Constant;
    using LAB2D.Core;
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Time;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    /// <summary>
    /// 昼夜光照管理器 — 驱动场景全局光（Global 类型 Light2D）的强度与色温随游戏时间循环：
    /// 午夜暗蓝 0.35 → 正午纯白 1.0（与无光照时代的画面亮度一致）→ 黄昏橙红 → 入夜偏蓝。
    /// 场景缺失激活的全局光时运行时自建（场景存量未激活的 GlobalLight 对 FindWithTag 不可见，
    /// 自建物随场景销毁，跨场景重进自动重建）。光照公式收口在 Domain 层 DayNightRuleService，
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
                this.globalLight = this.FindOrCreateGlobalLight();
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
        /// 查找场景激活的全局光；缺失则运行时自建（Global 类型，应用 Default 层 —
        /// 世界物体实际全在 Default 层；Highest 层头顶 UI 不被全局光压暗，夜间保持可读）。
        /// </summary>
        private Light2D FindOrCreateGlobalLight()
        {
            GameObject go = GameObject.FindGameObjectWithTag(TagConstant.GLOBAL_LIGHT_TAG);
            if (go != null)
            {
                Light2D found = go.GetComponent<Light2D>();
                if (found != null)
                {
                    AWorkerTask.LogProvider("[LightDiag] 复用场景全局光，昼夜光照驱动生效", LogManager.LogLevelEnum.Debug);
                    return found;
                }
            }

            GameObject created = new GameObject("DayNightGlobalLight");
            created.tag = TagConstant.GLOBAL_LIGHT_TAG;
            Light2D light = created.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
            AWorkerTask.LogProvider("[LightDiag] 场景无激活全局光，已运行时自建 DayNightGlobalLight", LogManager.LogLevelEnum.Debug);
            return light;
        }
    }
}
