using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D
{
    public class EnvironmentManager : Singleton<EnvironmentManager>
    {
        public float Temperature = -10.0f;
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
        public void updateEnergy()
        {
            if(CurEnergy <= MaxEnergy)
            {
                CurEnergy += Time.deltaTime;
            }
        }

        public override string ToString()
        {
            return $"温度:{Temperature}\n" +
                $"湿度:{Humidity}\n";
        }

        public string ToString(Vector3Int posMap) {
            RoomInfo roomInfo = RoomManager.Instance.getRoomByPos(posMap);
            if (roomInfo != null)
            {
                return roomInfo.ToString();
            }
            return ToString();
        }
    }
}
