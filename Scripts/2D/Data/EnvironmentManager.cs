namespace LAB2D.Data
{
    using LAB2D;
    using LAB2D.Domain.Common;
    using UnityEngine;

    /// <summary>
    /// 环境管理
    /// </summary>
    public class EnvironmentManager : Singleton<EnvironmentManager>, ITickable
    {
        // 温度由 TemperatureEffect 计算（季节+天气+昼夜），不在此维护死字段。

        /// <summary>
        /// 湿度（占位默认值；房间内湿度由 RoomManager 维护，野外湿度暂不做天气联动）
        /// </summary>
        public float Humidity = 25.0f;

        /// <summary>
        /// 最大灵气值
        /// </summary>
        public float MaxEnergy = 100.0f;

        /// <summary>
        /// 当前灵气值
        /// </summary>
        public float CurEnergy = 100.0f;

        /// <summary>
        /// 缓慢恢复灵气
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (this.CurEnergy <= this.MaxEnergy)
            {
                IWeatherGameplayService weather = ServiceLocator.Get<IWeatherGameplayService>();
                float recovery = deltaTime * weather.EnergyRecoveryMultiplier;
                this.CurEnergy = System.Math.Min(this.MaxEnergy, this.CurEnergy + recovery);
            }
        }

        /// <summary>
        /// 获取当前环境信息（温度 = 实时室外温度）。
        /// </summary>
        /// <returns>信息</returns>
        public override string ToString()
        {
            float temp = ServiceLocator.TryGet<ITemperatureEffectService>(out var tempEffect)
                ? tempEffect.GetOutdoorTemperature()
                : 0f;
            return $"温度:{temp:F1}\n" +
                $"湿度:{this.Humidity}\n" +
                $"灵气值:{this.CurEnergy:F0}/{this.MaxEnergy:F0}\n";
        }

        /// <summary>
        /// 获取某位置的当前环境信息。
        /// 房间内显示房间实时温度（室外+保温+供暖），野外显示室外温度。
        /// 用 GetRoomInterior 精确判断，避免 GetRoomByPos 的射线误判（曾致非房间位置显示房间温度）。
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int posMap)
        {
            RoomInfo roomInfo = ServiceLocator.Get<RoomManager>().GetRoomInterior(posMap);
            if (roomInfo != null)
            {
                return roomInfo.ToString();
            }

            return this.ToString();
        }
    }
}
