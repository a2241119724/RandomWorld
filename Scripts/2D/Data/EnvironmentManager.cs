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
        /// <summary>
        /// 温度
        /// </summary>
        public float Temperature = -10.0f;

        /// <summary>
        /// 湿度
        /// </summary>
        public float Humidity = -10.0f;

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
        /// 获取当前环境信息
        /// </summary>
        /// <returns>信息</returns>
        public override string ToString()
        {
            return $"温度:{this.Temperature}\n" +
                $"湿度:{this.Humidity}\n" +
                $"灵气值:{this.CurEnergy:F0}/{this.MaxEnergy:F0}\n";
        }

        /// <summary>
        /// 获取某位置的当前环境信息
        /// </summary>
        /// <param name="posMap">位置</param>
        /// <returns>信息</returns>
        public string ToString(Vector3Int posMap)
        {
            RoomInfo roomInfo = ServiceLocator.Get<RoomManager>().GetRoomByPos(posMap);
            if (roomInfo != null)
            {
                return roomInfo.ToString();
            }

            return this.ToString();
        }
    }
}
